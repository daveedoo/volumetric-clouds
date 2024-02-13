using OpenTK.Graphics.OpenGL;

namespace Clouds.GLWrappers
{
    public class FBO : IDisposable
    {
        private readonly int ID;

        public FBO()
        {
            ID = GL.GenFramebuffer();
        }

        public void Bind()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, ID);
        }

        public static void Unbind()
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void SetColorAttachment(int textureID)
        {
            Bind();
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D, textureID, 0);
        }

        public void Dispose()
        {
            GL.DeleteFramebuffer(ID);
            GC.SuppressFinalize(this);
        }
    }
}
