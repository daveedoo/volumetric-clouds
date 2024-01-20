using Clouds.GLWrappers;
using ImGuiNET;
using OpenTK.Graphics.OpenGL;
using System.Numerics;

namespace Clouds
{
    public class Application : Window
    {
        private GLWrappers.Program program;
        private int vaoId;

        public Application() : base()
        {
            Title = "Clouds";

            using Shader vertexShader = new(ShaderType.VertexShader, "../../../shaders/vertex.vert");
            using Shader fragmentShader = new(ShaderType.FragmentShader, "../../../shaders/fragment.frag");
            program = new(vertexShader, fragmentShader);

            int vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            float[] vertices =
            [
                -1.0f, -1.0f,   1.0f, -1.0f,
                0.0f, 1.0f
            ];
            GL.BufferData(BufferTarget.ArrayBuffer, 6 * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            vaoId = GL.GenVertexArray();
            GL.BindVertexArray(vaoId);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
            GL.EnableVertexArrayAttrib(vaoId, 0);
        }

        protected override void RenderScene()
        {
            program.Use();

            GL.BindVertexArray(vaoId);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        }

        private Vector3 test = new(0.0f);
        protected override void RenderGUI()
        {
            //// Enable Docking
            //ImGui.DockSpaceOverViewport();

            ImGui.ShowDemoWindow();

            ImGui.DragFloat3("test", ref test);
        }
    }
}
