using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace gip_common
{
    // Encapsulates a cylindrical prism mesh with panoramic UVs
    public class PrismMesh : IDisposable
    {
        private int _vao;
        private int _vbo;
        private int _vertexCount;

        public PrismMesh(int nSides, float height, float radius)
        {
            Create(nSides, height, radius);
        }

        private void Create(int nSides, float height, float radius)
        {
            if (_vao != 0) Dispose();

            var vertices = new List<float>();
            float delta = MathF.PI * 2.0f / nSides;
            float fi = 0.0f;

            // build strip of top/bottom vertex pairs with u running 0..1
            for (int i = 0; i <= nSides; i++)
            {
                float u = (float)i / nSides;
                float x = radius * MathF.Cos(fi);
                float y = radius * MathF.Sin(fi);
                // top
                vertices.Add(x); vertices.Add(y); vertices.Add(height / 2f);
                vertices.Add(1f); vertices.Add(1f); vertices.Add(1f);
                vertices.Add(u); vertices.Add(1f);
                vertices.Add(1f);
                // bottom
                vertices.Add(x); vertices.Add(y); vertices.Add(-height / 2f);
                vertices.Add(1f); vertices.Add(1f); vertices.Add(1f);
                vertices.Add(u); vertices.Add(0f);
                vertices.Add(1f);

                fi += delta;
            }

            // expand quad strip to triangles
            var tri = new List<float>();
            int pairs = nSides + 1;
            for (int i = 0; i < pairs - 1; i++)
            {
                int idx0 = i * 2;
                int idx1 = idx0 + 1;
                int idx2 = idx0 + 2;
                int idx3 = idx0 + 3;

                for (int k = 0; k < 9; k++) tri.Add(vertices[idx0 * 9 + k]);
                for (int k = 0; k < 9; k++) tri.Add(vertices[idx1 * 9 + k]);
                for (int k = 0; k < 9; k++) tri.Add(vertices[idx3 * 9 + k]);

                for (int k = 0; k < 9; k++) tri.Add(vertices[idx0 * 9 + k]);
                for (int k = 0; k < 9; k++) tri.Add(vertices[idx3 * 9 + k]);
                for (int k = 0; k < 9; k++) tri.Add(vertices[idx2 * 9 + k]);
            }

            var arr = tri.ToArray();
            _vertexCount = arr.Length / 9;

            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();
            GL.BindVertexArray(_vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, arr.Length * sizeof(float), arr, BufferUsageHint.StaticDraw);

            int stride = 9 * sizeof(float);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, 6 * sizeof(float));
            GL.EnableVertexAttribArray(3);
            GL.VertexAttribPointer(3, 1, VertexAttribPointerType.Float, false, stride, 8 * sizeof(float));

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }

        public void Draw()
        {
            if (_vao == 0) return;
            GL.BindVertexArray(_vao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, _vertexCount);
            GL.BindVertexArray(0);
        }

        public void Dispose()
        {
            if (_vbo != 0) GL.DeleteBuffer(_vbo);
            if (_vao != 0) GL.DeleteVertexArray(_vao);
            _vbo = 0; _vao = 0; _vertexCount = 0;
        }
    }
}
