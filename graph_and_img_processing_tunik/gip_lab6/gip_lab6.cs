using System;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System.Drawing;

class gip_lab2
{
    static void Main()
    {
        var gameWindowSettings = GameWindowSettings.Default;
        var nativeWindowSettings = new NativeWindowSettings();
        nativeWindowSettings.Title = "gip_lab6";
        nativeWindowSettings.ClientSize = new OpenTK.Mathematics.Vector2i(800, 600);

        // Для сумісності з legacy OpenGL:
        nativeWindowSettings.Profile = ContextProfile.Compatability;

            using (var window = new MyWindow(gameWindowSettings, nativeWindowSettings))
            {
                window.Run();
            }
    }
}

class MyWindow : GameWindow
{
    // глобальні змінні
    private float Angle = 0.0f;
    // rotation speed in degrees per second
    private float RotationSpeed = 30.0f;
    private TeapotRenderer teapotRenderer = new TeapotRenderer();

    public MyWindow(GameWindowSettings gws, NativeWindowSettings nws)
        : base(gws, nws) { }

    // FormCreate
    protected override void OnLoad()
    {
        // Load teapot from embedded Resources.resx instead of file path
        teapotRenderer.LoadTeapotFromResources();
        base.OnLoad();

        GL.Enable(EnableCap.Lighting);   // вмикаємо освітлення
        GL.Enable(EnableCap.Light0);     // джерело світла 0

        GL.Enable(EnableCap.DepthTest);  // Z-буфер
        GL.Enable(EnableCap.ColorMaterial); // режим відтворення кольорів
        // Enable scissor test using the OpenTK enum
        GL.Enable(EnableCap.ScissorTest);

        // Normalize normals after scaling/transforms so lighting remains correct
        GL.Enable(EnableCap.Normalize);
        GL.ShadeModel(ShadingModel.Smooth);
        GL.Enable(EnableCap.ColorMaterial);

        // Add a small global ambient component so the entire scene is softly lit
        float[] sceneAmbient = { 0.15f, 0.15f, 0.15f, 1.0f };
        GL.LightModel(LightModelParameter.LightModelAmbient, sceneAmbient);

        GL.ClearColor(0.1f, 0.0f, 0.2f, 1.0f); // фон
    }

    // FormResize
    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        float w = 1.5f; // масштабуючий множник

        GL.Viewport(0, 0, Size.X, Size.Y); // розмір вікна на формі
        GL.MatrixMode(MatrixMode.Projection); // встановлюємо видову матрицю (проектування)
        GL.LoadIdentity(); // завантажумо одиничну матрицю

        // задаємо об’єм видимості паралельної проекції
        float aspect = (float)Size.X / Size.Y;
        GL.Ortho(-w * aspect, w * aspect, -w, w, 1.0, 10.0);

        GL.MatrixMode(MatrixMode.Modelview); // встановлюємо модельну матрицю
        GL.LoadIdentity(); // завантажуємо одиничну матрицю
    }

    // FormPaint (replaced with multi-viewport rendering)
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        int W = Size.X;
        int H = Size.Y;
        if (W <= 0 || H <= 0)
        {
            SwapBuffers();
            return;
        }

        // Top-left viewport
        GL.Viewport(0, H / 2, W / 2, H / 2);
        GL.Scissor(0, H / 2, W / 2, H / 2);
        GL.ClearColor(0.9f, 0.7f, 0.8f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        GL.MatrixMode(MatrixMode.Modelview);
        GL.PushMatrix();
        {
            // For orthographic LEFT view: place camera on negative X axis looking toward origin
            Matrix4 lookat = Matrix4.LookAt(new Vector3(-8, 0, 0),
                                            new Vector3(0, 0, 0),
                                            new Vector3(0, 1, 0));
            GL.LoadMatrix(ref lookat);
            // rotation moved into Draw3D so light position follows object
            Draw3D();
        }
        GL.PopMatrix();

        // Bottom-left viewport
        GL.Viewport(0, 0, W / 2, H / 2);
        GL.Scissor(0, 0, W / 2, H / 2);
        GL.ClearColor(0.8f, 0.9f, 0.7f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        GL.PushMatrix();
        {
            // Frontal orthographic camera for axonometric projection
            Matrix4 lookat = Matrix4.LookAt(new Vector3(0, 0, 8),
                                            new Vector3(0, 0, 0),
                                            new Vector3(0, 1, 0));
            GL.LoadMatrix(ref lookat);

            // Apply axonometric rotations: psi = 75deg around Y, fi = 45deg around X
            GL.Rotate(75.0f, 0.0f, 1.0f, 0.0f); // psi
            GL.Rotate(45.0f, 1.0f, 0.0f, 0.0f); // fi

            // Draw without the automatic scene rotation to keep axonometric fixed
            Draw3D("noRotate");
        }
        GL.PopMatrix();

        // Top-right viewport
        GL.Viewport(W / 2, H / 2, W - W / 2, H / 2);
        GL.Scissor(W / 2, H / 2, W - W / 2, H / 2);
        GL.ClearColor(0.8f, 0.8f, 0.9f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        GL.PushMatrix();
        {
            // For orthographic BOTTOM view: place camera on negative Y axis looking toward origin
            Matrix4 lookat = Matrix4.LookAt(new Vector3(0, -8, 0),
                                            new Vector3(0, 0, 0),
                                            new Vector3(0, 0, 1));
            GL.LoadMatrix(ref lookat);
            Draw3D();
        }
        GL.PopMatrix();

        // Bottom-right viewport: use perspective projection with fov = 60 degrees
        GL.Viewport(W / 2, 0, W - W / 2, H / 2);
        GL.Scissor(W / 2, 0, W - W / 2, H / 2);
        GL.ClearColor(0.9f, 0.8f, 0.8f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        GL.PushMatrix();
        {
            // Save current projection, set perspective, draw scene, then restore projection
            GL.MatrixMode(MatrixMode.Projection);
            GL.PushMatrix();
            GL.LoadIdentity();

            // Field of view: 60 degrees
            float fovRadians = MathHelper.DegreesToRadians(60.0f);
            // Aspect ratio for this viewport
            int vpW = W - W / 2;
            int vpH = H / 2;
            float aspect = vpH > 0 ? (float)vpW / vpH : 1.0f;
            Matrix4 perspective = Matrix4.CreatePerspectiveFieldOfView(fovRadians, aspect, 1.0f, 10.0f);
            GL.LoadMatrix(ref perspective);

            // Back to modelview to position camera and draw
            GL.MatrixMode(MatrixMode.Modelview);

            // Place camera and scale scene so size roughly matches other viewports
            Matrix4 lookat = Matrix4.LookAt(new Vector3(2f, 2f, 2f),
                                            new Vector3(0, 0, 0),
                                            new Vector3(0, 1, 0));
            GL.LoadMatrix(ref lookat);

            // Empirically chosen scale to approximate visible object size in orthographic views
            float scale = 1f; // adjust if needed
            GL.Scale(scale, scale, scale);

            Draw3D();

            // Restore projection matrix
            GL.MatrixMode(MatrixMode.Projection);
            GL.PopMatrix();
            GL.MatrixMode(MatrixMode.Modelview);
        }
        GL.PopMatrix();

        // Update angle (speed-based)
        Angle += RotationSpeed * (float)args.Time; // degrees per second
        if (Angle >= 360.0f) Angle = 0.0f;

        SwapBuffers();
    }

    // FormDestroy
    protected override void OnUnload()
    {
        base.OnUnload();
        // у Delphi треба було wglDeleteContext — тут OpenTK сам все робить
    }

    private void Draw3D(string mode = "")
    {
        // Draw coordinate axes first
        // вектор з координатами джерела світла
        // Make the light positional (w=1) and place it above and to the side for stronger shading
        float[] pos = { 2.0f, 3.0f, 2.0f, 1.0f };

        // поворот сцени навколо осі Y за кутом Angle
        if (mode != "noRotate")
        {
        GL.Rotate(Angle, 0.0f, 1.0f, 0.0f);
        }

        // Move entire object (axes + teapot) down so it's centered in view
        GL.Translate(0.0f, -1.25f, 0.0f);

        // Configure light components for higher contrast: low ambient, stronger diffuse, bright specular
        float[] ambient = { 0.06f, 0.06f, 0.06f, 1.0f };
        float[] diffuse = { 0.9f, 0.9f, 0.9f, 1.0f };
        float[] specular = { 1.0f, 1.0f, 1.0f, 1.0f };

        GL.Light(LightName.Light0, LightParameter.Ambient, ambient);
        GL.Light(LightName.Light0, LightParameter.Diffuse, diffuse);
        GL.Light(LightName.Light0, LightParameter.Specular, specular);

        // Attenuation makes light fall off with distance so lighting isn't uniform everywhere
        GL.Light(LightName.Light0, LightParameter.ConstantAttenuation, 1.0f);
        GL.Light(LightName.Light0, LightParameter.LinearAttenuation, 0.08f);
        GL.Light(LightName.Light0, LightParameter.QuadraticAttenuation, 0.02f);

        // задання положення джерела світла (позиція задається в поточних координатах моделі)
        GL.Light(LightName.Light0, LightParameter.Position, pos);
        // DrawAxis; // зображення координатних напівосей
        DrawAxis();


        if (mode == "points")
        {
            GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Point);
            GL.PointSize(15.0f);
            GL.Enable(EnableCap.PointSmooth);
        }
        else if (mode == "wireframe")
        {
            GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
            GL.LineWidth(8.0f);
            GL.Enable(EnableCap.LineSmooth);
        }
        else // solid
        {
            GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
        }

        //Draw3DCube();
        CColors.GlColorFromArray(CColors.C_Cherry); // задаємо колір
        // Call native GLUT teapot via DGLUT wrapper (ensure freeglut.dll is in output folder)
        teapotRenderer.DrawTeapot(0.75);


        // Повернути в нормальний режим, щоб не зламати інший рендеринг
        GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
    }

    // Draw coordinate axes (X - red, Y - blue, Z - green)
    private void DrawAxis()
    {
        GL.Disable(EnableCap.Lighting);
        GL.LineWidth(2.0f);
        GL.Begin(PrimitiveType.Lines);

        // X axis (red)
        GL.Color3(1.0f, 0.0f, 0.0f);
        GL.Vertex3(0.0f, 0.0f, 0.0f);
        GL.Vertex3(1.0f, 0.0f, 0.0f);

        // Y axis (blue)
        GL.Color3(0.0f, 0.0f, 1.0f);
        GL.Vertex3(0.0f, 0.0f, 0.0f);
        GL.Vertex3(0.0f, 1.0f, 0.0f);

        // Z axis (green)
        GL.Color3(0.0f, 1.0f, 0.0f);
        GL.Vertex3(0.0f, 0.0f, 0.0f);
        GL.Vertex3(0.0f, 0.0f, 1.0f);

        GL.End();
        GL.Enable(EnableCap.Lighting);
    }


    void Draw3DCube()
    {
        GL.Translate(0.0f, 1f, 0.0f);
        float h = 1.0f; // половина довжини ребра куба

        GL.Begin(PrimitiveType.Quads); // режим виведення 4-кутників

        // Передня грань (червона)
        GL.Color3(1.0f, 0.0f, 0.0f);
        GL.Normal3(0.0f, 0.0f, 1.0f);
        GL.Vertex3(h, h, h);
        GL.Vertex3(-h, h, h);
        GL.Vertex3(-h, -h, h);
        GL.Vertex3(h, -h, h);

        // Права грань (синя)
        GL.Color3(0.0f, 0.0f, 1.0f);
        GL.Normal3(1.0f, 0.0f, 0.0f);
        GL.Vertex3(h, h, -h);
        GL.Vertex3(h, h, h);
        GL.Vertex3(h, -h, h);
        GL.Vertex3(h, -h, -h);

        // Задня грань (зелена)
        GL.Color3(0.0f, 1.0f, 0.0f);
        GL.Normal3(0.0f, 0.0f, -1.0f);
        GL.Vertex3(-h, h, -h);
        GL.Vertex3(h, h, -h);
        GL.Vertex3(h, -h, -h);
        GL.Vertex3(-h, -h, -h);

        // Ліва грань (жовта)
        GL.Color3(1.0f, 1.0f, 0.0f);
        GL.Normal3(-1.0f, 0.0f, 0.0f);
        GL.Vertex3(-h, h, h);
        GL.Vertex3(-h, -h, h);
        GL.Vertex3(-h, -h, -h);
        GL.Vertex3(-h, h, -h);

        // Верхня грань (рожева)
        GL.Color3(1.0f, 0.0f, 1.0f);
        GL.Normal3(0.0f, 1.0f, 0.0f);
        GL.Vertex3(h, h, h);
        GL.Vertex3(h, h, -h);
        GL.Vertex3(-h, h, -h);
        GL.Vertex3(-h, h, h);

        // Нижня грань (блакитна)
        GL.Color3(0.0f, 1.0f, 1.0f);
        GL.Normal3(0.0f, -1.0f, 0.0f);
        GL.Vertex3(h, -h, h);
        GL.Vertex3(-h, -h, h);
        GL.Vertex3(-h, -h, -h);
        GL.Vertex3(h, -h, -h);

        GL.End();
    }


}
