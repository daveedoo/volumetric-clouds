using Clouds.GLWrappers;
using ImGuiNET;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Clouds
{
    public class Application : Window
    {
        private GLWrappers.Program program;
        private int vaoId;
        
        private Vector2i windowSize = defaultWindowSize;
        private System.Numerics.Vector3 cameraPosition = new(5.0f, 2.0f, 5.0f);
        private System.Numerics.Vector3 cameraTarget = new(0.0f);
        private float cameraFOV = 90; // degrees

        private System.Numerics.Vector3 cloudBoxMin = new(-1.0f, -1.0f, 1.0f);
        private System.Numerics.Vector3 cloudBoxMax = new(1.0f, 1.0f, -1.0f);


        private static readonly Vector2i defaultWindowSize = new(1600, 900);
        public Application() : base(defaultWindowSize)
        {
            Title = "Clouds";

            SetupShaders();
            SetupVAO();
        }

        private void SetupVAO()
        {
            int vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            float[] vertices =
            [
                -1.0f, -1.0f,   1.0f, 1.0f,     -1.0f, 1.0f,
                -1.0f, -1.0f,   1.0f, -1.0f,    1.0f, 1.0f
            ];
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            vaoId = GL.GenVertexArray();
            GL.BindVertexArray(vaoId);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
            GL.EnableVertexArrayAttrib(vaoId, 0);
        }

        private void SetupShaders()
        {
            using Shader vertexShader = new(ShaderType.VertexShader, "../../../shaders/vertex.vert");
            using Shader fragmentShader = new(ShaderType.FragmentShader, "../../../shaders/fragment.frag");
            program = new(vertexShader, fragmentShader);
            program.SetVec3("cameraPos", new Vector3(cameraPosition.X, cameraPosition.Y, cameraPosition.Z));
            program.SetVec3("cloudsBoxMin", new Vector3(cloudBoxMin.X, cloudBoxMin.Y, cloudBoxMin.Z));
            program.SetVec3("cloudsBoxMax", new Vector3(cloudBoxMax.X, cloudBoxMax.Y, cloudBoxMax.Z));

            SetViewMatrix();
            SetProjectionMatrix();
        }

        private void SetViewMatrix()
        {
            Matrix4 viewMtx = Matrix4.LookAt(
                new Vector3(cameraPosition.X, cameraPosition.Y, cameraPosition.Z),
                new Vector3(cameraTarget.X, cameraTarget.Y, cameraTarget.Z),
                Vector3.UnitY);
            program.SetMat4("viewMtx", viewMtx);
        }
        private void SetProjectionMatrix()
        {
            Matrix4 projMtx = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(cameraFOV), (float)windowSize.X / windowSize.Y, 1.0f, 100.0f);
            program.SetMat4("projMtx", projMtx);
        }

        protected override void RenderScene()
        {
            program.Use();

            GL.BindVertexArray(vaoId);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        }

        protected override void RenderGUI()
        {
            //// Enable Docking
            //ImGui.DockSpaceOverViewport();

            ImGui.ShowDemoWindow();

            ImGui.Begin("-");
            if (ImGui.TreeNode("Camera"))
            {
                if (ImGui.DragFloat3("Camera Position", ref cameraPosition, 0.01f))
                {
                    SetViewMatrix();
                }
                if (ImGui.DragFloat3("Camera Target", ref cameraTarget, 0.01f))
                {
                    SetViewMatrix();
                }
                if (ImGui.DragFloat("Camera FOV", ref cameraFOV, 0.1f, 10.0f, 179.0f, "%.1f", ImGuiSliderFlags.AlwaysClamp))
                {
                    SetProjectionMatrix();
                }
                ImGui.TreePop();
            }
            if (ImGui.TreeNode("Clouds box"))
            {
                if (ImGui.DragFloat3("CloudBoxMin", ref cloudBoxMin))
                {
                    program.SetVec3("cloudsBoxMin", new Vector3(cloudBoxMin.X, cloudBoxMin.Y, cloudBoxMin.Z));
                }
                if (ImGui.DragFloat3("CloudBoxMax", ref cloudBoxMax))
                {
                    program.SetVec3("cloudsBoxMax", new Vector3(cloudBoxMax.X, cloudBoxMax.Y, cloudBoxMax.Z));
                }

            }
            ImGui.End();
        }

        public override void Dispose()
        {
            base.Dispose();

            program.Dispose();
            GL.DeleteVertexArray(vaoId);
        }
    }
}
