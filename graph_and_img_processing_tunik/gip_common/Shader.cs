using System;
using System.IO;
using OpenTK.Graphics.OpenGL4;

namespace gip_common
{
    // Minimal Shader helper: loads vertex and fragment shader from files, compiles and links them.
    public class Shader : IDisposable
    {
        public int Handle { get; private set; }

        public Shader(string vertexPath, string fragmentPath)
        {
            string vsSource = File.ReadAllText(vertexPath);
            string fsSource = File.ReadAllText(fragmentPath);

            int vs = CompileShader(ShaderType.VertexShader, vsSource);
            int fs = CompileShader(ShaderType.FragmentShader, fsSource);

            Handle = GL.CreateProgram();
            GL.AttachShader(Handle, vs);
            GL.AttachShader(Handle, fs);
            GL.LinkProgram(Handle);

            GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int status);
            if (status == 0)
            {
                string info = GL.GetProgramInfoLog(Handle);
                throw new Exception($"Program link error: {info}");
            }

            GL.DetachShader(Handle, vs);
            GL.DetachShader(Handle, fs);
            GL.DeleteShader(vs);
            GL.DeleteShader(fs);
        }

        private int CompileShader(ShaderType type, string source)
        {
            int shader = GL.CreateShader(type);
            GL.ShaderSource(shader, source);
            GL.CompileShader(shader);
            GL.GetShader(shader, ShaderParameter.CompileStatus, out int status);
            if (status == 0)
            {
                string info = GL.GetShaderInfoLog(shader);
                GL.DeleteShader(shader);
                throw new Exception($"Shader compile error ({type}): {info}");
            }
            return shader;
        }

        public void Use() => GL.UseProgram(Handle);

        public int GetUniformLocation(string name) => GL.GetUniformLocation(Handle, name);

        public void Dispose()
        {
            if (Handle != 0)
            {
                GL.DeleteProgram(Handle);
                Handle = 0;
            }
        }
    }
}
