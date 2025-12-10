using System;
using OpenTK.Mathematics;

namespace gip_common
{
    // Simple FPS-style camera supporting WASD movement, mouse rotation and scroll zoom
    public class Camera
    {
        public Vector3 Position { get; set; }
        public float Pitch { get; private set; } = 0f;
        public float Yaw { get; private set; } = -90f; // default to look along -Z
        public float Fov { get; private set; } = 45f;

        public Camera(Vector3 position)
        {
            Position = position;
        }

        public Matrix4 GetViewMatrix()
        {
            var front = GetFront();
            return Matrix4.LookAt(Position, Position + front, Vector3.UnitY);
        }

        public Vector3 GetFront()
        {
            Vector3 front;
            front.X = MathF.Cos(MathHelper.DegreesToRadians(Yaw)) * MathF.Cos(MathHelper.DegreesToRadians(Pitch));
            front.Y = MathF.Sin(MathHelper.DegreesToRadians(Pitch));
            front.Z = MathF.Sin(MathHelper.DegreesToRadians(Yaw)) * MathF.Cos(MathHelper.DegreesToRadians(Pitch));
            return front.Normalized();
        }

        public void ProcessMouseMovement(float deltaX, float deltaY, float sensitivity = 0.1f)
        {
            Yaw += deltaX * sensitivity;
            Pitch -= deltaY * sensitivity; // invert Y for typical camera

            // constrain
            Pitch = Math.Clamp(Pitch, -89f, 89f);
        }

        public void ProcessMouseScroll(float deltaZ, float sensitivity = 0.1f)
        {
            Position += GetFront() * deltaZ * sensitivity;
        }
    }
}
