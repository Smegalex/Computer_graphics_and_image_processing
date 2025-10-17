using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;


class gip_lab3
{
    static void Main()
    {
        var gameWindowSettings = GameWindowSettings.Default;
        var nativeWindowSettings = new NativeWindowSettings();
        nativeWindowSettings.Title = "OpenGL lab 3";
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
        Draw3D();
        // Draw3D("wireframe");


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

        DrawTransforming3DCylindricalSurface();

        // Повернути в нормальний режим, щоб не зламати інший рендеринг
        GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
    }



    void DrawColorful3DCube()
    {
        float h = 1.0f; // половина довжини ребра куба

        GL.Begin(PrimitiveType.Quads); // режим виведення 4-кутників

        // Передня грань (бузкова)
        GL.Color3(0.78f, 0.635f, 0.78f);
        GL.Normal3(0.0f, 0.0f, 1.0f);
        GL.Vertex3(h, h, h);
        GL.Vertex3(-h, h, h);
        GL.Vertex3(-h, -h, h);
        GL.Vertex3(h, -h, h);

        // Права грань (лимонна)
        GL.Color3(0.996f, 0.9686f, 0.65f);
        GL.Normal3(1.0f, 0.0f, 0.0f);
        GL.Vertex3(h, h, -h);
        GL.Vertex3(h, h, h);
        GL.Vertex3(h, -h, h);
        GL.Vertex3(h, -h, -h);

        // Задня грань (коричнева)
        GL.Color3(0.424f, 0.235f, 0.047f);
        GL.Normal3(0.0f, 0.0f, -1.0f);
        GL.Vertex3(-h, h, -h);
        GL.Vertex3(h, h, -h);
        GL.Vertex3(h, -h, -h);
        GL.Vertex3(-h, -h, -h);

        // Ліва грань (морської хвилі)
        GL.Color3(0.149f, 1.0f, 0.773f);
        GL.Normal3(-1.0f, 0.0f, 0.0f);
        GL.Vertex3(-h, h, h);
        GL.Vertex3(-h, -h, h);
        GL.Vertex3(-h, -h, -h);
        GL.Vertex3(-h, h, -h);

        // Верхня грань (вишнева)
        GL.Color3(0.392f, 0.0f, 0.137f);
        GL.Normal3(0.0f, 1.0f, 0.0f);
        GL.Vertex3(h, h, h);
        GL.Vertex3(h, h, -h);
        GL.Vertex3(-h, h, -h);
        GL.Vertex3(-h, h, h);

        // Нижня грань (салатова)
        GL.Color3(0.624f, 0.926f, 0.326f);
        GL.Normal3(0.0f, -1.0f, 0.0f);
        GL.Vertex3(h, -h, h);
        GL.Vertex3(-h, -h, h);
        GL.Vertex3(-h, -h, -h);
        GL.Vertex3(h, -h, -h);

        GL.End();
    }
    void DrawGradient3DCube()
    {
        float h = 1.0f; // половина довжини ребра куба

        GL.Begin(PrimitiveType.Quads); // режим виведення 4-кутників

        // Передня грань 
        GL.Normal3(0.0f, 0.0f, 1.0f);
        GL.Color3(1f, 1f, 0f); // yellow
        GL.Vertex3(h, h, h);
        GL.Vertex3(-h, h, h);
        GL.Color3(1f, 0f, 0f); // red
        GL.Vertex3(-h, -h, h);
        GL.Vertex3(h, -h, h);

        // Права грань 
        GL.Normal3(1.0f, 0.0f, 0.0f);
        GL.Color3(1f, 1f, 0f); // yellow
        GL.Vertex3(h, h, -h);
        GL.Vertex3(h, h, h);
        GL.Color3(1f, 0f, 0f); // red
        GL.Vertex3(h, -h, h);
        GL.Vertex3(h, -h, -h);

        // Задня грань
        GL.Normal3(0.0f, 0.0f, -1.0f);
        GL.Color3(1f, 1f, 0f); // yellow
        GL.Vertex3(-h, h, -h);
        GL.Vertex3(h, h, -h);
        GL.Color3(1f, 0f, 0f); // red
        GL.Vertex3(h, -h, -h);
        GL.Vertex3(-h, -h, -h);

        // Ліва грань
        GL.Normal3(-1.0f, 0.0f, 0.0f);
        GL.Color3(1f, 1f, 0f); // yellow
        GL.Vertex3(-h, h, h);
        GL.Color3(1f, 0f, 0f); // red
        GL.Vertex3(-h, -h, h);
        GL.Vertex3(-h, -h, -h);
        GL.Color3(1f, 1f, 0f); // yellow
        GL.Vertex3(-h, h, -h);

        // Верхня грань 
        GL.Normal3(0.0f, 1.0f, 0.0f);
        GL.Color3(1f, 1f, 0f); // yellow
        GL.Vertex3(h, h, h);
        GL.Vertex3(h, h, -h);
        GL.Vertex3(-h, h, -h);
        GL.Vertex3(-h, h, h);

        // Нижня грань 
        GL.Normal3(0.0f, -1.0f, 0.0f);
        GL.Color3(1f, 0f, 0f); // red
        GL.Vertex3(h, -h, h);
        GL.Vertex3(-h, -h, h);
        GL.Vertex3(-h, -h, -h);
        GL.Vertex3(h, -h, -h);

        GL.End();
    }

    void DrawDynamic3DCube()
    {
        float h = 1.0f; // половина довжини ребра куба
        float rad = MathF.PI / 180f; // degrees → radians

        // Обчислюємо базові кольори (в залежності від часу)
        float redBase = MathF.Abs(MathF.Sin(2 * Angle * rad));
        float greenBase = MathF.Abs(MathF.Sin(3 * Angle * rad));
        float blueBase = MathF.Abs(MathF.Sin(5 * Angle * rad));


        GL.Begin(PrimitiveType.Quads); // режим виведення 4-кутників

        // Передня грань 
        GL.Normal3(0.0f, 0.0f, 1.0f);
        GL.Color3(redBase, 0.0f, blueBase);
        GL.Vertex3(h, h, h);
        GL.Vertex3(-h, h, h);
        GL.Color3(redBase * 0.5f, greenBase, blueBase * 0.5f);
        GL.Vertex3(-h, -h, h);
        GL.Vertex3(h, -h, h);

        // Права грань 
        GL.Normal3(1.0f, 0.0f, 0.0f);
        GL.Color3(redBase, 0.0f, blueBase);
        GL.Vertex3(h, h, -h);
        GL.Vertex3(h, h, h);
        GL.Color3(redBase * 0.5f, greenBase, blueBase * 0.5f);
        GL.Vertex3(h, -h, h);
        GL.Vertex3(h, -h, -h);

        // Задня грань
        GL.Normal3(0.0f, 0.0f, -1.0f);
        GL.Color3(redBase, 0.0f, blueBase);
        GL.Vertex3(-h, h, -h);
        GL.Vertex3(h, h, -h);
        GL.Color3(redBase * 0.5f, greenBase, blueBase * 0.5f);
        GL.Vertex3(h, -h, -h);
        GL.Vertex3(-h, -h, -h);

        // Ліва грань
        GL.Normal3(-1.0f, 0.0f, 0.0f);
        GL.Color3(redBase, 0.0f, blueBase);
        GL.Vertex3(-h, h, h);
        GL.Color3(redBase * 0.5f, greenBase, blueBase * 0.5f);
        GL.Vertex3(-h, -h, h);
        GL.Vertex3(-h, -h, -h);
        GL.Color3(redBase, 0.0f, blueBase);
        GL.Vertex3(-h, h, -h);

        // Верхня грань 
        GL.Normal3(0.0f, 1.0f, 0.0f);
        GL.Color3(redBase, 0.0f, blueBase);
        GL.Vertex3(h, h, h);
        GL.Vertex3(h, h, -h);
        GL.Vertex3(-h, h, -h);
        GL.Vertex3(-h, h, h);

        // Нижня грань 
        GL.Normal3(0.0f, -1.0f, 0.0f);
        GL.Color3(redBase * 0.5f, greenBase, blueBase * 0.5f);
        GL.Vertex3(h, -h, h);
        GL.Vertex3(-h, -h, h);
        GL.Vertex3(-h, -h, -h);
        GL.Vertex3(h, -h, -h);

        GL.End();
    }

    void Draw3DCylinder(int n = 40, float h = 1.0f, float r = 0.5f)
    {

        float delta_fi = 2.0f * MathF.PI / n;
        float fi = 0.0f;

        GL.Color3(0.0f, 1.0f, 0.0f); // колір смуг

        GL.Begin(PrimitiveType.QuadStrip); // draw using 4-vertex strips
        for (int i = 0; i <= n; i++)
        {
            // Вектор нормалі для освітлення
            GL.Normal3(MathF.Cos(fi), MathF.Sin(fi), 0.0f);

            // Верхня вершина
            GL.Vertex3(r * MathF.Cos(fi), r * MathF.Sin(fi), h / 2.0f);

            // Нижня вершина
            GL.Vertex3(r * MathF.Cos(fi), r * MathF.Sin(fi), -h / 2.0f);

            // Пересуваємо кут
            fi += delta_fi;
        }
        GL.End();
    }

    void DrawDynamic3DCylinder(int n = 80, float h = 2.0f, float r = 1.0f)
    {
        float delta_fi = 2.0f * MathF.PI / n;
        float fi = 0.0f;

        GL.Disable(EnableCap.Lighting); // вимикаємо освітлення
        GL.Begin(PrimitiveType.QuadStrip); // пояс із 4-кутників

        for (int i = 0; i <= n; i++)
        {
            // ---- колір нижніх вершин ----
            float red = MathF.Abs(MathF.Sin(2 * Angle * MathF.PI / 180f - fi));
            float blue = MathF.Abs(MathF.Sin(3 * Angle * MathF.PI / 180f - fi));
            GL.Color3(red, 0.0f, blue);
            GL.Vertex3(r * MathF.Cos(fi), r * MathF.Sin(fi), -0.5f*h); // нижня вершина

            // ---- колір верхніх вершин ----
            red = MathF.Abs(MathF.Sin(5 * Angle * MathF.PI / 180f + fi));
            blue = MathF.Abs(MathF.Sin(7 * Angle * MathF.PI / 180f + fi));
            GL.Color3(red, 1.0f, blue);
            GL.Vertex3(r * MathF.Cos(fi), r * MathF.Sin(fi), 0.5f*h); // верхня вершина

            fi += delta_fi;
        }

        GL.End();
    }

    void Draw3DDoublePyramid(int n = 40, float h = 1.5f, float r = 1.0f)
    {
        float delta_fi = 2.0f * MathF.PI / n;

        GL.Disable(EnableCap.Lighting); // вимикаємо освітлення для яскравих кольорів

        for (int k = 0; k <= 1; k++) // дві піраміди
        {
            float fi = 0.0f;

            GL.Begin(PrimitiveType.TriangleFan); // виведення трикутників

            // Колір вершин
            float redApex = 0.5f;  
            float blueApex = 0.5f;
            GL.Color3(1.0f - redApex, 0.0f, 1.0f - blueApex);
            GL.Vertex3(0.0f, 0.0f, h - k * 2.0f * h); // верхня або нижня вершина (v0)

            // Вершини основи
            for (int i = 0; i <= n; i++)
            {
                float red = MathF.Abs(MathF.Sin(5 * Angle * MathF.PI / 180f - fi));
                float blue = MathF.Abs(MathF.Sin(3 * Angle * MathF.PI / 180f - fi));
                GL.Color3(red, 1.0f - red, blue);
                GL.Vertex3(r * MathF.Cos(fi), r * MathF.Sin(fi), 0.0f); // V1..Vn

                fi += delta_fi;
            }

            GL.End();
        }
    }

    void Draw3DTruncatedCone(int n = 90, float h = 1.0f, float r1 = 1.0f, float r2 = 0.05f) {
        float delta_fi = 2.0f * MathF.PI / n;

        GL.Disable(EnableCap.Lighting); 

        for (int k = 0; k <= 1; k++)
        {
            float fi = 0.0f;

            GL.Begin(PrimitiveType.QuadStrip);

            for (int i = 0; i <= n; i++)
            {
                // ---- колір нижніх вершин ----
                float red = MathF.Abs(MathF.Sin(2 * Angle * MathF.PI / 180f - fi));
                float blue = MathF.Abs(MathF.Sin(3 * Angle * MathF.PI / 180f - fi));
                float green = MathF.Abs(MathF.Sin(Angle * MathF.PI / 180f + fi));
                GL.Color3(red, green, blue);

                // вершина нижнього кільця
                GL.Vertex3(r1 * MathF.Cos(fi), r1 * MathF.Sin(fi), -h + 2.0f * h * k);

                // ---- колір верхніх вершин ----
                red = MathF.Abs(MathF.Sin(5 * Angle * MathF.PI / 180f + fi));
                blue = MathF.Abs(MathF.Sin(7 * Angle * MathF.PI / 180f + fi));
                green = MathF.Abs(MathF.Sin(3 * Angle * MathF.PI / 180f + fi));
                GL.Color3(red, green, blue);

                // вершина верхнього кільця
                GL.Vertex3(r2 * MathF.Cos(fi), r2 * MathF.Sin(fi), 0.0f);

                fi += delta_fi;
            }

            GL.End();
        }
    }

    void DrawTransforming3DCylindricalSurface(int n = 80, float r = 1.0f, float h = 2.0f)
    {
        float delta_fi = 2.0f * MathF.PI / n;
        float fi = 0.0f;

        GL.Begin(PrimitiveType.QuadStrip);

        for (int i = 0; i <= n; i++)
        {
            // ---- колір нижніх вершин ----
            float red = MathF.Abs(MathF.Sin(2 * Angle * MathF.PI / 180f - fi));
            float blue = MathF.Abs(MathF.Sin(3 * Angle * MathF.PI / 180f - fi));
            GL.Color3(red, 0.0f, blue);

            // Нижн вершина (повернена на sin/cos кута перегляду)
            GL.Vertex3(
                r * MathF.Cos(fi) * MathF.Sin(Angle * MathF.PI / 180f),
                r * MathF.Sin(fi) * MathF.Cos(Angle * MathF.PI / 180f),
                -h / 2.0f
            );

            // ---- колір верхніх вершин ----
            red = MathF.Abs(MathF.Sin(5 * Angle * MathF.PI / 180f + fi));
            blue = MathF.Abs(MathF.Sin(7 * Angle * MathF.PI / 180f + fi));
            GL.Color3(red, 1.0f, blue);

            // Верхня вершина (інша логіка обертання)
            GL.Vertex3(
                r * MathF.Cos(fi) * MathF.Cos(Angle * MathF.PI / 180f),
                r * MathF.Sin(fi) * MathF.Sin(Angle * MathF.PI / 180f),
                h / 2.0f
            );

            fi += delta_fi;
        }

        GL.End();
    }

}
