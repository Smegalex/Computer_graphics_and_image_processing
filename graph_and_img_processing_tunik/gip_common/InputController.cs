using System;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;

namespace gip_common
{
    public class InputController
    {
        private bool _middleDown;
        private Camera _camera;
        private NativeWindow _window;
        private int _scene = 1;

        public int Scene => _scene;

        public InputController(NativeWindow window, Camera camera)
        {
            _window = window;
            _camera = camera;
            _window.MouseDown += OnMouseDown;
            _window.MouseUp += OnMouseUp;
            _window.MouseMove += OnMouseMove;
            _window.MouseWheel += OnMouseWheel;
        }

        public void Update(KeyboardState kb, float deltaTime)
        {
            var front = _camera.GetFront();
            var right = Vector3.Cross(front, Vector3.UnitY).Normalized();
            var up = Vector3.Cross(right, front).Normalized();
            float speed = 2.0f * deltaTime;

            if (kb.IsKeyDown(Keys.W)) _camera.Position += up * speed;
            if (kb.IsKeyDown(Keys.S)) _camera.Position -= up * speed;
            if (kb.IsKeyDown(Keys.A)) _camera.Position -= right * speed;
            if (kb.IsKeyDown(Keys.D)) _camera.Position += right * speed;

            // Scene switching: press 1..5 to switch scenes
            if (kb.IsKeyPressed(Keys.D1)) _scene = 1;
            if (kb.IsKeyPressed(Keys.D2)) _scene = 2;
            if (kb.IsKeyPressed(Keys.D3)) _scene = 3;
            if (kb.IsKeyPressed(Keys.D4)) _scene = 4;
            if (kb.IsKeyPressed(Keys.D5)) _scene = 5;
        }

        private void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.Button == MouseButton.Middle)
            {
                _middleDown = true;
                _window.CursorState = CursorState.Grabbed;
            }
        }

        private void OnMouseUp(MouseButtonEventArgs e)
        {
            if (e.Button == MouseButton.Middle)
            {
                _middleDown = false;
                _window.CursorState = CursorState.Normal;
            }
        }

        private void OnMouseMove(MouseMoveEventArgs e)
        {
            if (_middleDown)
            {
                _camera.ProcessMouseMovement(e.DeltaX, e.DeltaY, 0.2f);
            }
        }

        private void OnMouseWheel(MouseWheelEventArgs e)
        {
            _camera.ProcessMouseScroll(e.OffsetY);
        }
    }
}
