using OpenTK.Graphics.OpenGL;

namespace Clouds.GLWrappers
{
    public class VAO
    {
        private readonly int ID;
        
        public VAO()
        {
            ID = GL.GenVertexArray();
        }

        public void Bind()
        {
            GL.BindVertexArray(ID);
        }

        public static void Unbind()
        {
            GL.BindVertexArray(0);
        }
    }
}
