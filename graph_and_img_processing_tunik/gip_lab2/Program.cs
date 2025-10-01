using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

class Program
{
    static void Main()
    {
        var gameWindowSettings = GameWindowSettings.Default;
        var nativeWindowSettings = new NativeWindowSettings();
        nativeWindowSettings.Title = "My First OpenGL Window";
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

    public MyWindow(GameWindowSettings gws, NativeWindowSettings nws)
        : base(gws, nws) { }

    // FormCreate
    protected override void OnLoad()
    {
        base.OnLoad();

        GL.Enable(EnableCap.Lighting);   // вмикаємо освітлення
        GL.Enable(EnableCap.Light0);     // джерело світла 0

        GL.Enable(EnableCap.DepthTest);  // Z-буфер
        GL.Enable(EnableCap.ColorMaterial); // режим відтворення кольорів

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

    // FormPaint
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit); // очищення буферів зображення та глибини

        GL.MatrixMode(MatrixMode.Modelview);
        GL.LoadIdentity();

        // задання положення камери в світових координатах
        // gluLookAt(0,0,8, 0,0,0, 0,1,0)
        Matrix4 lookat = Matrix4.LookAt(new Vector3(0, 0, 8),
                                        new Vector3(0, 0, 0),
                                        new Vector3(0, 1, 0));
        GL.LoadMatrix(ref lookat);

        // Поворот на кут Angle навколо вектора (1,1,1)
        GL.Rotate(Angle, 1.0f, 1.0f, 1.0f);

        // виклик процедури побудови об’єктів
        Draw3D("points");
        Draw3D("wireframe");


        // OnTimer (обмін зображення переднього і заднього буферів)
        SwapBuffers();

        // зміна кута для обертання сцени
        Angle += 30f * (float)args.Time; // 30 градусів/секунду
        if (Angle >= 360.0f) Angle = 0.0f;
    }

    // FormDestroy
    protected override void OnUnload()
    {
        base.OnUnload();
        // у Delphi треба було wglDeleteContext — тут OpenTK сам все робить
    }

    private void Draw3D(string mode = "")
    {
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

        Draw3DPrism();

        // Повернути в нормальний режим, щоб не зламати інший рендеринг
        GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
    }



    void Draw3DCube()
    {
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

    public static void Draw3DPrism(int n = 10, float h = 1.0f, float r = 0.5f) // n - кількість граней, h - висота, r - радіус описаного навколо основи кола
    {
        float deltaFi = 2.0f * MathF.PI / n;
        float fi = 0.0f;

        // Бічні грані
        GL.Color3(0.0f, 0.0f, 1.0f); // зелений
        GL.Begin(PrimitiveType.Quads);
        for (int i = 0; i < n; i++)
        {
            // нормаль у напрямку середини грані
            GL.Normal3(MathF.Cos(fi + deltaFi / 2), MathF.Sin(fi + deltaFi / 2), 0.0f);

            // вершини грані (по колу)
            GL.Vertex3(r * MathF.Cos(fi), r * MathF.Sin(fi), h / 2);   // 1
            GL.Vertex3(r * MathF.Cos(fi), r * MathF.Sin(fi), -h / 2);  // 2
            GL.Vertex3(r * MathF.Cos(fi + deltaFi), r * MathF.Sin(fi + deltaFi), -h / 2); // 3
            GL.Vertex3(r * MathF.Cos(fi + deltaFi), r * MathF.Sin(fi + deltaFi), h / 2);  // 4

            fi += deltaFi;
        }
        GL.End();

        // Верхня грань (n-кутник)
        fi = 0.0f;
        GL.Begin(PrimitiveType.Polygon);
        GL.Color3(1.0f, 1.0f, 0.0f); // колір верхньої грані
        GL.Normal3(0.0f, 0.0f, 1.0f); // вектор нормалі
        for (int i = 0; i < n; i++) // вершини правильного n-кутника
        {
            GL.Vertex3(r * MathF.Cos(fi), r * MathF.Sin(fi), h / 2);
            fi += deltaFi;
        }
        GL.End();

        // Нижня грань (n-кутник)
        fi = 0.0f;
        GL.Begin(PrimitiveType.Polygon);
        GL.Color3(1.0f, 0.0f, 1.0f); // колір нижньої грані
        GL.Normal3(0.0f, 0.0f, -1.0f); // вектор нормалі
        for (int i = 0; i < n; i++)
        {
            // обхід у зворотному напрямку, щоб нормалі були правильні
            GL.Vertex3(r * MathF.Cos(-fi), r * MathF.Sin(-fi), -h / 2);
            fi += deltaFi;
        }
        GL.End();
    }

    private void Draw3DPyramid(int n = 16, float h = 1.0f, float R = 0.5f) // n - кількість граней, h - висота, R - радіус описаного навколо основи кола
    {
        float delta_fi = 2.0f * MathF.PI / n;
        float teta = MathF.Atan(h / R);
        float fi = 0.0f;

        // Бічні грані
        GL.Begin(PrimitiveType.Triangles);
        for (int i = 0; i < n; i++)
        {
            // різні кольори для кожної грані
            GL.Color3((i % 2) / 1.0f, (i % 3) / 2.0f, (i % 5) / 4.0f);

            // нормаль
            GL.Normal3(
                MathF.Cos(fi + delta_fi / 2) * MathF.Sin(teta),
                MathF.Sin(fi + delta_fi / 2) * MathF.Sin(teta),
                MathF.Cos(teta)
            );

            // вершини грані
            GL.Vertex3(0.0f, 0.0f, h);                    // v1 (вершина)
            GL.Vertex3(R * MathF.Cos(fi), R * MathF.Sin(fi), 0.0f);                  // v2
            GL.Vertex3(R * MathF.Cos(fi + delta_fi), R * MathF.Sin(fi + delta_fi), 0.0f); // v3

            fi += delta_fi;
        }
        GL.End();

        // Основа (n-кутник)
        fi = 0.0f;
        GL.Begin(PrimitiveType.Polygon);
        GL.Color3(1.0f, 1.0f, 0.0f); // колір основи
        GL.Normal3(0.0f, 0.0f, -1.0f); // нормаль

        for (int i = 0; i < n; i++)
        {
            GL.Vertex3(R * MathF.Cos(fi), R * MathF.Sin(fi), 0.0f);
            fi += delta_fi;
        }

        GL.End();
    }

}
