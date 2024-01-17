using ImGuiNET;
using OpenTK.Windowing.Common;

namespace Clouds
{
    public class Application
    {
        private readonly Window window;

        public Application()
        {
            window = new Window();
            window.Title = "Clouds";
            window.UpdateFrame += OnFrameUpdate;
            window.RenderFrame += OnFrameRender;
        }

        void OnFrameUpdate(FrameEventArgs args)
        {

        }

        float val = 0.0f;
        void OnFrameRender(FrameEventArgs args)
        {

        }

        public void Run()
        {
            window.Run();
        }
    }
}
