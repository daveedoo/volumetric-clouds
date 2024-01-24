using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Clouds.GLWrappers
{
    public class Program : IDisposable
    {
        public readonly int ID;

        public Program(Shader vertexShader, Shader fragmentShader)
        {
            if (vertexShader.ShaderType != ShaderType.VertexShader ||
                fragmentShader.ShaderType != ShaderType.FragmentShader)
            {
                throw new ArgumentException("wrong shader type");
            }

            ID = GL.CreateProgram();
            GL.AttachShader(ID, vertexShader.ID);
            GL.AttachShader(ID, fragmentShader.ID);
            GL.LinkProgram(ID);

            GL.GetProgram(ID, GetProgramParameterName.LinkStatus, out int linkStatus);
            if (linkStatus == 0)
            {
                GL.GetProgramInfoLog(ID, out string info);
                throw new Exception(info);
            }

            GL.DetachShader(ID, vertexShader.ID);
            GL.DetachShader(ID, fragmentShader.ID);
        }

        public void Dispose()
        {
            GL.DeleteProgram(ID);
        }

        public void Use()
        {
            GL.UseProgram(ID);
        }

        void ExecuteUniformVariableOperation(string uniformName, Action<int> operation)
        {
            Use();
            int location = GL.GetUniformLocation(ID, uniformName);
            if (location == -1)
            {
                throw new Exception($"Unable to locate uniform variable {uniformName}.");
            }
            operation(location);
        }

        public void SetFloat(string uniformName, float value)
        {
            ExecuteUniformVariableOperation(uniformName, (int location) =>
            {
                GL.Uniform1(location, value);
            });
        }

        public void SetVec2(string uniformName, Vector2 value)
        {
            ExecuteUniformVariableOperation(uniformName, (int location) =>
            {
                GL.Uniform2(location, value);
            });
        }

        public void SetVec3(string uniformName, Vector3 value)
        {
            ExecuteUniformVariableOperation(uniformName, (int location) =>
            {
                GL.Uniform3(location, value);
            });
        }
        public void SetVec4(string uniformName, Vector4 value)
        {
            ExecuteUniformVariableOperation(uniformName, (int location) =>
            {
                GL.Uniform4(location, value);
            });
        }

        public void SetMat4(string uniformName, Matrix4 value)
        {
            ExecuteUniformVariableOperation(uniformName, (int location) =>
            {
                GL.UniformMatrix4(location, false, ref value);
            });
        }
    }
}
