using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace gip_common
{
    public class CubeMesh : IDisposable
    {
        private int _vao;
        private int _vbo;
        private int _vertexCount;

        private CubeMesh(int vao, int vbo, int vertexCount)
        {
            _vao = vao; _vbo = vbo; _vertexCount = vertexCount;
        }

        // Backwards-compatible Create
        public static CubeMesh Create(float h) => Create(h, false);

        // New overload: panoramic=true will generate UVs that map the 4 side faces continuously from 0..1
        public static CubeMesh Create(float h, bool panoramic)
        {
            var vertices = CreateCubeVertices(h, panoramic);
            int vao = GL.GenVertexArray();
            int vbo = GL.GenBuffer();

            GL.BindVertexArray(vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            int stride = 9 * sizeof(float);
            GL.EnableVertexAttribArray(0); // pos
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
            GL.EnableVertexAttribArray(1); // color
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
            GL.EnableVertexAttribArray(2); // tex
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, 6 * sizeof(float));
            GL.EnableVertexAttribArray(3); // use
            GL.VertexAttribPointer(3, 1, VertexAttribPointerType.Float, false, stride, 8 * sizeof(float));

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);

            return new CubeMesh(vao, vbo, vertices.Length / 9);
        }

        public void Draw()
        {
            GL.BindVertexArray(_vao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, _vertexCount);
            GL.BindVertexArray(0);
        }

        public void Dispose()
        {
            if (_vbo != 0) GL.DeleteBuffer(_vbo);
            if (_vao != 0) GL.DeleteVertexArray(_vao);
            _vbo = 0; _vao = 0;
        }

        private static float[] CreateCubeVertices(float h, bool panoramic)
        {
            // define cube corners
            var p000 = new Vector3(-h, -h, -h);
            var p001 = new Vector3(-h, -h, h);
            var p010 = new Vector3(-h, h, -h);
            var p011 = new Vector3(-h, h, h);
            var p100 = new Vector3(h, -h, -h);
            var p101 = new Vector3(h, -h, h);
            var p110 = new Vector3(h, h, -h);
            var p111 = new Vector3(h, h, h);

            var v = new System.Collections.Generic.List<float>();
            void AddFace(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 color)
            {
                Vector3[] vs = new Vector3[] { a, b, c, d };
                Vector2[] uvs = new Vector2[] { new Vector2(1,1), new Vector2(0,1), new Vector2(0,0), new Vector2(1,0) };
                int[] idx = new int[] { 0, 1, 2, 0, 2, 3 };
                for (int i = 0; i < 6; i++)
                {
                    var p = vs[idx[i]];
                    var t = uvs[idx[i]];
                    v.Add(p.X); v.Add(p.Y); v.Add(p.Z);
                    v.Add(color.X); v.Add(color.Y); v.Add(color.Z);
                    v.Add(t.X); v.Add(t.Y);
                    v.Add(1f);
                }
            }

            // default uv corners used for non-panoramic faces (matches previous mapping)
            var uv0 = new Vector2(1, 1);
            var uv1 = new Vector2(0, 1);
            var uv2 = new Vector2(0, 0);
            var uv3 = new Vector2(1, 0);

            if (!panoramic)
            {
                // original mapping
                AddFace(p111, p011, p001, p101, new Vector3(1, 0, 0)); // front
                AddFace(p110, p111, p101, p100, new Vector3(0, 0, 1)); // right
                AddFace(p010, p110, p100, p000, new Vector3(0, 1, 0)); // back
                AddFace(p011, p010, p000, p001, new Vector3(1, 1, 0)); // left
                AddFace(p111, p110, p010, p011, new Vector3(1, 0, 1)); // top
                AddFace(p101, p001, p000, p100, new Vector3(0, 1, 1)); // bottom
            }
            else
            {
                // Create panoramic mapping around the four side faces so that u runs 0..1 continuously
                // face order: front, right, back, left
                float[] uStarts = new float[] { 0.0f, 0.25f, 0.5f, 0.75f };
                float uWidth = 0.25f;

                // helper that adds a face with explicit uvs
                void AddFaceWithUVs(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 color, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
                {
                    Vector3[] vs = new Vector3[] { a, b, c, d };
                    Vector2[] uvs = new Vector2[] { ua, ub, uc, ud };
                    int[] idx = new int[] { 0, 1, 2, 0, 2, 3 };
                    for (int i = 0; i < 6; i++)
                    {
                        var p = vs[idx[i]];
                        var t = uvs[idx[i]];
                        v.Add(p.X); v.Add(p.Y); v.Add(p.Z);
                        v.Add(color.X); v.Add(color.Y); v.Add(color.Z);
                        v.Add(t.X); v.Add(t.Y);
                        v.Add(1f);
                    }
                }

                for (int face = 0; face < 4; face++)
                {
                    float u0 = uStarts[face];
                    float u1 = u0 + uWidth;
                    var ua = new Vector2(u1, 1);
                    var ub = new Vector2(u0, 1);
                    var uc = new Vector2(u0, 0);
                    var ud = new Vector2(u1, 0);

                    if (face == 0) // front
                        AddFaceWithUVs(p111, p011, p001, p101, new Vector3(1, 0, 0), ua, ub, uc, ud);
                    else if (face == 1) // right
                        AddFaceWithUVs(p110, p111, p101, p100, new Vector3(0, 0, 1), ua, ub, uc, ud);
                    else if (face == 2) // back
                        AddFaceWithUVs(p010, p110, p100, p000, new Vector3(0, 1, 0), ua, ub, uc, ud);
                    else // left
                        AddFaceWithUVs(p011, p010, p000, p001, new Vector3(1, 1, 0), ua, ub, uc, ud);
                }

                // Note: intentionally omit top and bottom when panoramic mapping is used
            }

            return v.ToArray();
        }
    }
}
