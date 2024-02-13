using Clouds.GLWrappers;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace Clouds
{
    public abstract class Window : GameWindow
    {
        ImGuiController _controller;
        protected Color4 clearColor = new(0, 32, 48, 255);

        public Window(Vector2i clientSize) : base(GameWindowSettings.Default, new NativeWindowSettings() { ClientSize = clientSize, APIVersion = new Version(4, 6) })
        { }

        protected override void OnLoad()
        {
            base.OnLoad();

            Title += ": OpenGL Version: " + GL.GetString(StringName.Version);

            _controller = new ImGuiController(ClientSize.X, ClientSize.Y);
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            // Update the opengl viewport
            GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);

            // Tell ImGui of the new size
            _controller.WindowResized(ClientSize.X, ClientSize.Y);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            _controller.Update(this, (float)e.Time);

            HandleMouseAndKeyboardInput(e);
            RenderScene();

            RenderGUI();
            _controller.Render();
            ImGuiController.CheckGLError("End of frame");

            SwapBuffers();
        }

        protected abstract void RenderScene();
        protected abstract void HandleMouseAndKeyboardInput(FrameEventArgs e);
        protected abstract void RenderGUI();

        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);


            _controller.PressChar((char)e.Unicode);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            _controller.MouseScroll(e.Offset);
        }
    }
}