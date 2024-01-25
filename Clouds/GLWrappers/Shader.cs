using OpenTK.Graphics.OpenGL;

namespace Clouds.GLWrappers
{
    public class Shader : IDisposable
    {
        public readonly int ID;
        public readonly ShaderType ShaderType;

        public Shader(ShaderType shaderType, string shaderSourcePath)
        {
            string srcCode = ReadFileContents(shaderSourcePath);
            ID = CompileShader(shaderType, srcCode);
            ShaderType = shaderType;
        }

        private static string ReadFileContents(string shaderSourcePath)
        {
            using StreamReader reader = File.OpenText(shaderSourcePath);
            return reader.ReadToEnd();
        }

        private static int CompileShader(ShaderType shaderType, string shaderSrcCode)
        {
            int ID = GL.CreateShader(shaderType);
            GL.ShaderSource(ID, shaderSrcCode);
            GL.CompileShader(ID);

            GL.GetShader(ID, ShaderParameter.CompileStatus, out int compilationStatus);
            if (compilationStatus == 0)
            {
                GL.GetShaderInfoLog(ID, out string info);
                throw new Exception(info);
            }

            return ID;
        }

        public void Dispose()
        {
            GL.DeleteShader(ID);
        }
    }
}
