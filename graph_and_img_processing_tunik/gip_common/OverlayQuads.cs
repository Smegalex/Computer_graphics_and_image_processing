using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace gip_common
{
    // Encapsulates two overlay quads (top and bottom) that can be drawn with blending
    public class OverlayQuads : IDisposable
    {
        private int _vao;
        private int _vbo;
        private int _vertexCount;

        public OverlayQuads(float h)
        {
            Create(h);
        }

        private void Create(float h)
        {
            if (_vao != 0) Dispose();

            var verts = new List<float>();

            void AddQuad(float y, bool flipV)
            {
                float t0 = flipV ? 1f : 0f;
                float t1 = flipV ? 0f : 1f;

                verts.Add(-h); verts.Add(y); verts.Add(-h);
                verts.Add(1f); verts.Add(1f); verts.Add(1f);
                verts.Add(0f); verts.Add(t1);
                verts.Add(1f);

                verts.Add(h); verts.Add(y); verts.Add(-h);
                verts.Add(1f); verts.Add(1f); verts.Add(1f);
                verts.Add(1f); verts.Add(t1);
                verts.Add(1f);

                verts.Add(h); verts.Add(y); verts.Add(h);
                verts.Add(1f); verts.Add(1f); verts.Add(1f);
                verts.Add(1f); verts.Add(t0);
                verts.Add(1f);

                verts.Add(-h); verts.Add(y); verts.Add(-h);
                verts.Add(1f); verts.Add(1f); verts.Add(1f);
                verts.Add(0f); verts.Add(t1);
                verts.Add(1f);

                verts.Add(h); verts.Add(y); verts.Add(h);
                verts.Add(1f); verts.Add(1f); verts.Add(1f);
                verts.Add(1f); verts.Add(t0);
                verts.Add(1f);

                verts.Add(-h); verts.Add(y); verts.Add(h);
                verts.Add(1f); verts.Add(1f); verts.Add(1f);
                verts.Add(0f); verts.Add(t0);
                verts.Add(1f);
            }

            AddQuad(h, false);
            AddQuad(-h, true);

            var arr = verts.ToArray();
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
