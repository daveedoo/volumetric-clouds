using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Clouds
{
    public class ComputeShader
    {
        int Handle;
        private readonly Dictionary<string, int> UniformLoc;
        public ComputeShader(string computePath)
        {
            //uchwyt na shadery
            int ComputeShader = 0;
            //zapisanie shadera do stringa
            string ComputeShaderSource = File.ReadAllText(computePath);
            //stworzenie shaderów i przypisanie do uchwytów + uzupełnienie zawartością
            ComputeShader = GL.CreateShader(ShaderType.ComputeShader);
            GL.ShaderSource(ComputeShader, ComputeShaderSource);


            //kompilacja shaderów i test czy działa
            GL.CompileShader(ComputeShader);
            int success = 0;
            GL.GetShader(ComputeShader, ShaderParameter.CompileStatus, out success);
            if (success == 0)
            {
                string infoLog = GL.GetShaderInfoLog(ComputeShader);
                Console.WriteLine(infoLog);
            }

            //stworzenie programu
            Handle = GL.CreateProgram();

            GL.AttachShader(Handle, ComputeShader);

            GL.LinkProgram(Handle);

            GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out success);
            if (success == 0)
            {
                string infoLog = GL.GetProgramInfoLog(Handle);
                Console.WriteLine(infoLog);
            }

            GL.DetachShader(Handle, ComputeShader);
            GL.DeleteShader(ComputeShader);

            GL.GetProgram(Handle, GetProgramParameterName.ActiveUniforms, out var numberOfUniforms);

            UniformLoc = new Dictionary<string, int>();

            for (var i = 0; i < numberOfUniforms; i++)
            {
                var key = GL.GetActiveUniform(Handle, i, out _, out _);
                var location = GL.GetUniformLocation(Handle, key);
                UniformLoc.Add(key, location);
            }
        }

        public void Use()
        {
            GL.UseProgram(Handle);
        }

        public void SetMatrix4(string name, Matrix4 matrix)
        {
            //GL.UseProgram(Handle);
            GL.UniformMatrix4(UniformLoc[name], true, ref matrix);
        }

        public void SetVal(string name, float data)
        {
            //GL.UseProgram(Handle);
            GL.Uniform1(UniformLoc[name], data);
        }
        public void SetVec3(string name, Vector3 vec)
        {
            GL.Uniform3(UniformLoc[name], vec);
        }
        public void SetVec4(string name, Vector4 vec)
        {
            GL.Uniform4(UniformLoc[name], vec);
        }

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                GL.DeleteProgram(Handle);

                disposedValue = true;
            }
        }

        ~ComputeShader()
        {
            GL.DeleteProgram(Handle);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this); //Garbage Collector
        }
    }
}
