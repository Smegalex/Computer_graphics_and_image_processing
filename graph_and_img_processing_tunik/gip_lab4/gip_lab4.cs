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
            Title = "gip_lab4",
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
    private int _cubeTexture = 0;
    private int _cubeTexture2 = 0; // second texture for scene 2
    private int _cubeTexture3 = 0; // procedural texture for scene 3
    private float _texture2scale = 2f;

    // which scene to render (1 = original, 2 = scaled/new texture, 3 = procedural)
    private int _scene = 1;
    // input helper
    private InputController? _input;

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
        _cubeMesh = CubeMesh.Create(h);

        // Load textures only from embedded resources (Resources.resx)
        var rm = new ResourceManager("gip_lab4.Resources", typeof(Game).Assembly);
        _cubeTexture = LoadBmpTexture(rm, "TextureTask1");
        _cubeTexture2 = LoadBmpTexture(rm, "TextureTask2");

        // Create procedural texture for scene 3
        _cubeTexture3 = CalculateTexture(512, 512);

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
        if (_cubeTexture3 != 0 && _cubeTexture3 != _cubeTexture && _cubeTexture3 != _cubeTexture2) GL.DeleteTexture(_cubeTexture3);
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

        if (_shader == null || _cubeMesh == null)
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
            case 3: texToUse = _cubeTexture3; break;
        }

        // If textures are not available, texToUse may be 0. Shader should handle absence gracefully.
        float texScale = (_scene == 1) ? 1.0f : (_scene == 2 ? _texture2scale : 1.0f);
        GL.Uniform1(_shader.GetUniformLocation("uTexScale"), texScale);

        // bind texture to unit 0
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, texToUse);
        GL.Uniform1(_shader.GetUniformLocation("uTexture"), 0);

        // draw cube
        _cubeMesh.Draw();

        Context.SwapBuffers();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
    }

    // Procedural texture generator: fills RGBA array, uploads texture, sets filters and generates mipmaps
    private int CalculateTexture(int tw, int th)
    {
        if (tw <= 0 || th <= 0) return 0;

        var data = new byte[tw * th * 4];

        // more intricate spiral parameters
        float turns = 8.0f;            // base number of spiral turns
        float tightness = 4.0f;        // radial contribution to winding
        float wavesA = 12.0f;          // higher-frequency modulation A
        float wavesB = 6.0f;           // higher-frequency modulation B
        float amplitudeA = 0.6f;
        float amplitudeB = 0.35f;
        float ringFreq = 25.0f;        // concentric rings frequency
        float ringSharpness = 2.0f;   // makes rings sharper
        float saturation = 0.95f;

        // center and normalization
        float cx = tw * 0.5f;
        float cy = th * 0.5f;
        float invMaxRadius = 1.0f / MathF.Min(cx, cy);

        for (int y = 0; y < th; y++)
        {
            for (int x = 0; x < tw; x++)
            {
                int i = (y * tw + x) * 4;
                // centered coordinates in -1..1 space
                float dx = (x - cx) * invMaxRadius;
                float dy = (y - cy) * invMaxRadius;
                float r = MathF.Sqrt(dx * dx + dy * dy); // radius
                float theta = MathF.Atan2(dy, dx); // angle
                                                   // base spiral phase
                float basePhase = theta * turns + r * tightness;
                // harmonic modulations (no random noise)
                float modA = MathF.Sin(basePhase * wavesA + MathF.Sin(r * 6.0f) * 1.2f) * amplitudeA;
                float modB = MathF.Cos(basePhase * wavesB - r * 3.0f) * amplitudeB;
                // combine into a complex wave; add small angular modulation to create curls
                float compound = MathF.Sin(basePhase + modA + modB + MathF.Sin(theta * 3.0f) * 0.25f);
                // rings overlay (sharpened sine) to create concentric detail
                float rings = MathF.Sin(r * ringFreq + compound * 2.0f);
                // sharpen rings using smoothstep-like shaping
                float ringsShaped = SmoothStep(0.5f - 0.5f / ringSharpness, 0.5f + 0.5f / ringSharpness, rings * 0.5f + 0.5f);
                // create folds by combining absolute and pow for contrast
                float folds = MathF.Pow(MathF.Abs(compound) * 1.1f, 0.9f);
                // final value multiplicative combination, fade toward edges
                float fade = Clamp01(1.0f - r);
                float value = Clamp01((0.6f * ringsShaped + 0.6f * folds) * fade);

                // Color phase depends ONLY on theta for seamless wrapping
                // Normalize theta to 0..2π
                float colorPhase = theta < 0 ? theta + 2.0f * MathF.PI : theta;

                // Use r and compound to modulate saturation/brightness instead
                float sat = saturation * (0.8f + 0.2f * MathF.Sin(r * 2.0f + compound));

                // color channels as cosine waves shifted by 120 degrees
                float baseR = 0.5f + 0.5f * MathF.Cos(colorPhase);
                float baseG = 0.5f + 0.5f * MathF.Cos(colorPhase + 2.0f * MathF.PI / 3.0f);
                float baseB = 0.5f + 0.5f * MathF.Cos(colorPhase + 4.0f * MathF.PI / 3.0f);

                // apply saturation and brightness
                float rf = value * ((1f - sat) + sat * baseR);
                float gf = value * ((1f - sat) + sat * baseG);
                float bf = value * ((1f - sat) + sat * baseB);

                data[i + 0] = (byte)(Clamp01(rf) * 255f);
                data[i + 1] = (byte)(Clamp01(gf) * 255f);
                data[i + 2] = (byte)(Clamp01(bf) * 255f);
                data[i + 3] = 255;
            }
        }

        int tex = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, tex);

        // set wrapping and filtering
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);

        GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, tw, th, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Rgba, PixelType.UnsignedByte, data);

        // generate mipmaps (gluBuild2DMipmaps equivalent)
        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

        GL.BindTexture(TextureTarget.Texture2D, 0);
        return tex;
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

    // Overload: accept a resource object (Bitmap, byte[], or FileRef string)
    private int LoadBmpTexture(object resource)
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

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
            // Bitmap stores in memory as BGRA (Windows GDI), so upload with Bgra
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, formatted.Width, formatted.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
            formatted.UnlockBits(data);

            GL.BindTexture(TextureTarget.Texture2D, 0);
            return tex;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to load bmp texture from bitmap: " + ex.Message);
            return 0;
        }
    }

    // Helper clamps for environments without MathF.Clamp / Clamp01
    private static float Clamp(float v, float a, float b)
    {
        if (v < a) return a;
        if (v > b) return b;
        return v;
    }

    private static float Clamp01(float v) => Clamp(v, 0f, 1f);

    private static float SmoothStep(float a, float b, float x)
    {
        float t = Clamp01((x - a) / (b - a));
        return t * t * (3f - 2f * t);
    }
}