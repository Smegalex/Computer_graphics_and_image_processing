using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Resources;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using gip_common;
using StbImageSharp;

class Program
{
    static void Main(string[] args)
    {
        var nativeSettings = new NativeWindowSettings()
        {
            ClientSize = new Vector2i(800, 600),
            Title = "gip_lab5",
            Flags = ContextFlags.Default
        };

        using (var window = new Game(GameWindowSettings.Default, nativeSettings))
        {
            window.Run();
        }
    }
}

class Game : GameWindow
{
    private Shader? _shader;
    private Camera _camera;

    // cube mesh
    private CubeMesh? _cubeMesh;
    private bool _cubePanoramic = false; // track current cube UV layout
    private int _cubeTexture = 0;
    private int _cubeTexture2 = 0; // second texture for scene 2 / panoramic texture
    private int _atlasTexture = 0; // atlas texture for scene 4
    private int _transparentTexture = 0; // Task4 transparent RGBA texture
    private float _texture2scale = 1f;

    // which scene to render (1 = original, 2 = scaled/new texture, 3 = procedural)
    private int _scene = 1;
    // input helper
    private InputController? _input;

    // encapsulated prism and overlay meshes (moved to gip_common)
    private PrismMesh? _prism;
    private OverlayQuads? _overlayQuads;

    private float _prismPanoramaSpeed = 0.5f;

    public Game(GameWindowSettings gwSettings, NativeWindowSettings nwSettings)
        : base(gwSettings, nwSettings)
    {
        _camera = new Camera(new Vector3(0f, 0f, 3f));

        // Normal cursor by default; will grab when middle button pressed
        CursorState = CursorState.Normal;

        // InputController will subscribe to mouse events
        _input = new InputController(this, _camera);
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);

        // Load shaders
        var exeDir = AppContext.BaseDirectory;
        var vertPath = Path.Combine(exeDir, "Shaders", "simple.vert.glsl");
        var fragPath = Path.Combine(exeDir, "Shaders", "simple.frag.glsl");

        _shader = new gip_common.Shader(vertPath, fragPath);

        // Create cube mesh (encapsulates VAO/VBO)
        float h = 1.0f; // half-edge
        _cubeMesh = CubeMesh.Create(h, false);
        _cubePanoramic = false;

        // Load textures only from embedded resources (Resources.resx)
        var rm = new ResourceManager("gip_lab5.Resources", typeof(Game).Assembly);
        _cubeTexture = LoadBmpTexture(rm, "Task1Texture");
        _cubeTexture2 = LoadBmpTexture(rm, "Task2PanoramicTexture");

        // load atlas generated from 4 JPEG resource images (Task3Texture1..4)
        _atlasTexture = LoadJpegTexturesFromResources(rm, "Task3Texture", 4, 512, 512);

        // load transparent RGBA texture prepared as Task4Texture (white background -> alpha)
        _transparentTexture = LoadBmpTexture(rm, "Task4Texture", generateRgbaWithAlpha: true);

        // create prism mesh for scene 3 (panoramic-mapped sides) using gip_common.PrismMesh
        _prism = new PrismMesh(12, 2.0f, 1f);

        // create overlay quads for scene 5 using gip_common.OverlayQuads
        _overlayQuads = new OverlayQuads(1.0f);

        // Enable depth testing
        GL.Enable(EnableCap.DepthTest);
    }

    protected override void OnUnload()
    {
        base.OnUnload();

        _shader?.Dispose();
        _cubeMesh?.Dispose();
        if (_cubeTexture != 0) GL.DeleteTexture(_cubeTexture);
        if (_cubeTexture2 != 0 && _cubeTexture2 != _cubeTexture) GL.DeleteTexture(_cubeTexture2);
        if (_atlasTexture != 0 && _atlasTexture != _cubeTexture && _atlasTexture != _cubeTexture2) GL.DeleteTexture(_atlasTexture);
        if (_transparentTexture != 0 && _transparentTexture != _cubeTexture && _transparentTexture != _cubeTexture2  && _transparentTexture != _atlasTexture) GL.DeleteTexture(_transparentTexture);

        _prism?.Dispose();
        _overlayQuads?.Dispose();
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        // delegate movement handling to InputController (keyboard+mouse)
        _input?.Update(KeyboardState, (float)args.Time);

        // Scene switching moved into InputController; read current scene from it
        _scene = _input?.Scene ?? 1;

        if (IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Escape))
            Close();
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        if (_shader == null)
            return;

        _shader.Use();

        var model = Matrix4.Identity;
        var view = _camera.GetViewMatrix();
        var projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(_camera.Fov), (float)Size.X / Size.Y, 0.1f, 100f);

        GL.UniformMatrix4(_shader.GetUniformLocation("uModel"), false, ref model);
        GL.UniformMatrix4(_shader.GetUniformLocation("uView"), false, ref view);
        GL.UniformMatrix4(_shader.GetUniformLocation("uProjection"), false, ref projection);

        // Setup texture scale / choose texture based on scene
        int texToUse = _cubeTexture;
        switch (_scene)
        {
            case 1: texToUse = _cubeTexture; break;
            case 2: texToUse = _cubeTexture2; break;
            case 3: texToUse = _cubeTexture2; break; // use panoramic texture for prism as requested
            case 4: texToUse = _atlasTexture; break; // scene 4 uses atlas of 4 JPEGs
            case 5: texToUse = _atlasTexture; break; // scene 5 uses atlas for sides and overlays transparent texture on top/bottom
        }

        // If textures are not available, texToUse may be 0. Shader should handle absence gracefully.
        // compute animated offset for panoramic texture (scene 2)
        float offset = 0f;
        if (_scene == 2)
        {
            // scroll based on time/rotation; use frame time via system clock
            double t = DateTime.Now.TimeOfDay.TotalSeconds;
            offset = (float)((t * 0.05) % 1.0); // slow scroll to the right
        }
        else if (_scene == 3)
        {
            // For scene 3 we want the panoramic texture to be offset differently (reverse direction)
            double t = DateTime.Now.TimeOfDay.TotalSeconds;
            offset = (float)((-t * _prismPanoramaSpeed) % 1.0);
        }

        float texScale = (_scene == 1) ? 1.0f : (_scene == 2 ? _texture2scale : 1.0f);
        // for scene 4 we want to keep scale 1.0 (tiles are baked into atlas)
        if (_scene == 4 || _scene == 5) texScale = 1.0f;

        GL.Uniform1(_shader.GetUniformLocation("uTexScale"), texScale);
        GL.Uniform1(_shader.GetUniformLocation("uTexOffset"), offset);

        // draw depending on scene
        if (_scene == 3)
        {
            // bind panoramic texture
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, texToUse);
            GL.Uniform1(_shader.GetUniformLocation("uTexture"), 0);

            DrawPrism();
        }
        else if (_scene == 2)
        {
            // recreate cube mesh as panoramic so side faces use continuous u mapping
            // only recreate if not already panoramic
            if (!_cubePanoramic)
            {
                _cubeMesh?.Dispose();
                _cubeMesh = CubeMesh.Create(1.0f, true);
                _cubePanoramic = true;
            }

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, texToUse);
            GL.Uniform1(_shader.GetUniformLocation("uTexture"), 0);

            _cubeMesh?.Draw();
        }
        else if (_scene == 4)
        {
            // Scene 4: use panoramic cube so the 4 side faces read the atlas left-to-right
            if (!_cubePanoramic)
            {
                _cubeMesh?.Dispose();
                _cubeMesh = CubeMesh.Create(1.0f, true);
                _cubePanoramic = true;
            }

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, texToUse);
            GL.Uniform1(_shader.GetUniformLocation("uTexture"), 0);

            _cubeMesh?.Draw();
        }
        else if (_scene == 5)
        {
            // Scene 5: draw panoramic cube (atlas on sides) then overlay transparent texture on top and bottom
            if (!_cubePanoramic)
            {
                _cubeMesh?.Dispose();
                _cubeMesh = CubeMesh.Create(1.0f, true);
                _cubePanoramic = true;
            }

            // draw panoramic sides with atlas
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _atlasTexture != 0 ? _atlasTexture : 0);
            GL.Uniform1(_shader.GetUniformLocation("uTexture"), 0);
            _cubeMesh?.Draw();

            // draw overlay quads with transparent texture
            if (_transparentTexture != 0 && _overlayQuads != null)
            {
                GL.Enable(EnableCap.Blend);
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

                // Prevent transparent fragments from writing to the depth buffer
                GL.DepthMask(false);

                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, _transparentTexture);
                GL.Uniform1(_shader.GetUniformLocation("uTexture"), 0);

                _overlayQuads.Draw();

                // restore depth writes and blending state
                GL.DepthMask(true);
                GL.Disable(EnableCap.Blend);
            }
        }
        else
        {
            // ensure non-panoramic cube when in scene 1
            if (_cubePanoramic)
            {
                _cubeMesh?.Dispose();
                _cubeMesh = CubeMesh.Create(1.0f, false);
                _cubePanoramic = false;
            }
            else if (_cubeMesh == null)
            {
                _cubeMesh = CubeMesh.Create(1.0f, false);
            }

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, texToUse);
            GL.Uniform1(_shader.GetUniformLocation("uTexture"), 0);

            _cubeMesh.Draw();
        }

        Context.SwapBuffers();
    }

    // Draw the prism (uses panoramic texture bound by caller)
    private void DrawPrism()
    {
        if (_prism == null) return;

        // rotate prism so its vertical axis (height) is perpendicular to camera forward (-Z)
        // the prism currently has its axis along Z; rotate 90 degrees around Y to align axis with X
        var prismModel = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(-90f));
        GL.UniformMatrix4(_shader!.GetUniformLocation("uModel"), false, ref prismModel);

        _prism.Draw();

        // restore model to identity for subsequent draws
        var identity = Matrix4.Identity;
        GL.UniformMatrix4(_shader!.GetUniformLocation("uModel"), false, ref identity);
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
    }

    // Load BMP from resource (resx) by key, returns texture handle or 0
    private int LoadBmpTexture(ResourceManager rm, string resourceKey)
    {
        if (rm == null || string.IsNullOrEmpty(resourceKey)) return 0;
        try
        {
            var obj = rm.GetObject(resourceKey);
            return LoadBmpTexture(obj);
        }
        catch (Exception ex)
        {
            Console.WriteLine("LoadBmpTexture(ResourceManager) failed: " + ex.Message);
            return 0;
        }
    }

    // Overload that can optionally generate RGBA with alpha from white background
    private int LoadBmpTexture(ResourceManager rm, string resourceKey, bool generateRgbaWithAlpha)
    {
        if (rm == null || string.IsNullOrEmpty(resourceKey)) return 0;
        try
        {
            var obj = rm.GetObject(resourceKey);
            if (obj is Bitmap bmp)
            {
                if (generateRgbaWithAlpha)
                    return LoadBmpTextureAsRgbaWithAlpha(bmp);
                return LoadBmpTexture(bmp);
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("LoadBmpTexture(ResourceManager) failed: " + ex.Message);
            return 0;
        }
    }

    // Overload: accept a resource object (Bitmap, byte[], or FileRef string)
    private int LoadBmpTexture(object? resource)
    {
        if (resource == null) return 0;

        try
        {
            if (resource is Bitmap bmp)
            {
                return LoadBmpTexture(bmp);
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("LoadBmpTexture(resource) failed: " + ex.Message);
            return 0;
        }
    }

    private int LoadBmpTexture(Bitmap bmp)
    {
        if (bmp == null) return 0;
        try
        {
            // convert to 32bpp ARGB and flip vertically to match OpenGL
            using var formatted = new Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(formatted))
            {
                g.DrawImage(bmp, 0, 0, bmp.Width, bmp.Height);
            }
            formatted.RotateFlip(RotateFlipType.RotateNoneFlipY);

            var data = formatted.LockBits(new Rectangle(0, 0, formatted.Width, formatted.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            int tex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, tex);

            // set wrapping and filtering
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            // Use mipmapped min filter and linear mag filter. This mirrors the behavior of gluBuild2DMipmaps
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
            // Bitmap stores in memory as BGRA (Windows GDI), so upload with Bgra
            // Upload level 0 then generate mipmaps automatically (modern equivalent of gluBuild2DMipmaps)
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, formatted.Width, formatted.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
            formatted.UnlockBits(data);

            // Generate mipmaps for automatic mipmap levels (replaces gluBuild2DMipmaps)
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            GL.BindTexture(TextureTarget.Texture2D, 0);
            return tex;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to load bmp texture from bitmap: " + ex.Message);
            return 0;
        }
    }

    // Create an RGBA texture from a bitmap by setting alpha=0 for near-white pixels (useful for subject on white background)
    private int LoadBmpTextureAsRgbaWithAlpha(Bitmap bmp)
    {
        if (bmp == null) return 0;
        try
        {
            int w = bmp.Width;
            int h = bmp.Height;
            var data = new byte[w * h * 4];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // note: bmp origin is top-left; we want to flip vertically for GL, so write row h-1-y
                    Color c = bmp.GetPixel(x, y);
                    int iy = ( (h - 1 - y) * w + x ) * 4;
                    // invert color as in original Pascal example (255 - channel)
                    byte r = (byte)(255 - c.R);
                    byte g = (byte)(255 - c.G);
                    byte b = (byte)(255 - c.B);
                    byte a = 255;
                    // if the pixel is very close to white (original foreground on white) then make transparent
                    if (c.R > 240 && c.G > 240 && c.B > 240)
                        a = 0;

                    data[iy + 0] = r;
                    data[iy + 1] = g;
                    data[iy + 2] = b;
                    data[iy + 3] = a;
                }
            }

            int tex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, tex);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);

            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, w, h, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Rgba, PixelType.UnsignedByte, data);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            return tex;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to create RGBA texture with alpha: " + ex.Message);
            return 0;
        }
    }

    // Load multiple JPEG images from resources, place them side-by-side into an atlas and upload as a single GL texture.
    private int LoadJpegTexturesFromResources(ResourceManager rm, string baseKey, int count, int tileW, int tileH)
    {
        if (rm == null || count <= 0 || tileW <= 0 || tileH <= 0) return 0;

        var bitmaps = new System.Collections.Generic.List<Bitmap>();
        try
        {
            for (int k = 1; k <= count; k++)
            {
                string key = baseKey + k.ToString();
                object? obj = null;
                try { obj = rm.GetObject(key); } catch { obj = null; }

                Bitmap? bmp = null;
                if (obj is Bitmap b) bmp = new Bitmap(b);
                else if (obj is byte[] bytes)
                {
                    using var ms = new MemoryStream(bytes);
                    var img = Image.FromStream(ms);
                    bmp = new Bitmap(img);
                }
                else if (obj is string path && File.Exists(path))
                {
                    bmp = new Bitmap(path);
                }

                if (bmp == null)
                {
                    // create placeholder (solid magenta) if resource missing
                    bmp = new Bitmap(tileW, tileH, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                    using (var g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.Magenta);
                    }
                }

                // ensure bmp has expected size by drawing into a new bitmap sized tileW x tileH
                var resized = new Bitmap(tileW, tileH, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(resized))
                {
                    g.DrawImage(bmp, 0, 0, tileW, tileH);
                }
                bmp.Dispose();
                // rotate flip vertically to match OpenGL coordinate expectations later
                resized.RotateFlip(RotateFlipType.RotateNoneFlipY);
                bitmaps.Add(resized);
            }

            // build atlas
            int atlasW = count * tileW;
            int atlasH = tileH;
            using var atlas = new Bitmap(atlasW, atlasH, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(atlas))
            {
                g.Clear(Color.Transparent);
                for (int i = 0; i < bitmaps.Count; i++)
                {
                    g.DrawImage(bitmaps[i], i * tileW, 0, tileW, tileH);
                }
            }

            // upload atlas to GL (bitmap is already flipped vertically to match GL when we created individual tiles)
            var atlasData = atlas.LockBits(new Rectangle(0, 0, atlas.Width, atlas.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            int tex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, tex);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);

            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, atlas.Width, atlas.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, atlasData.Scan0);
            atlas.UnlockBits(atlasData);

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            return tex;
        }
        catch (Exception ex)
        {
            Console.WriteLine("LoadJpegTexturesFromResources failed: " + ex.Message);
            foreach (var b in bitmaps) b.Dispose();
            return 0;
        }
        finally
        {
            // dispose temporary resized bitmaps (they were copied into atlas or uploaded)
            foreach (var b in bitmaps) b.Dispose();
        }
    }
}
