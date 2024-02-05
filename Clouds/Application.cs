using Clouds.GLWrappers;
using ImGuiNET;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Clouds
{
    public class Application : Window
    {
        private GLWrappers.Program program;
        private Camera _camera;
        private int vaoId;
        
        private Vector2i windowSize = defaultWindowSize;
        private Vector3 cameraPosition = new(5.0f, 3.0f, 0.0f);

        private System.Numerics.Vector3 cloudsBoxCenter = new(0.0f);
        private float cloudsBoxSideLength = 2.0f;
        private float cloudsBoxHeight = 2.0f;


        private static readonly Vector2i defaultWindowSize = new(1600, 900);
        public Application() : base(defaultWindowSize)
        {
            Title = "Clouds";
            _camera = new Camera(cameraPosition, (float)windowSize.X / windowSize.Y);
            _camera.Yaw = 180;
            _camera.Pitch = -10;
            SetupShaders();
            SetupVAO();
            SetupTexture();
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
            
            program.SetVec3("cameraPos", _camera.Position);
            SetCloudBoxUniforms();
        }

        private void SetupTexture()
        {
            const int cloudsTextureUnit = 0;

            int texId = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture0 + cloudsTextureUnit);
            GL.BindTexture(TextureTarget.Texture2D, texId);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);

            (byte[] data, int texSize)= GetCloudTextureData();
            
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, texSize, texSize, 0, PixelFormat.Rgba, PixelType.UnsignedByte/* or different? */, data);

            program.SetInt("cloudsTexture", cloudsTextureUnit);
        }

        // TODO: decide if byte type is the best
        private (byte[] data, int textureSize) GetCloudTextureData()
        {
            int texSize = 512;
            // mock
            return (Enumerable.Repeat<byte>(50, 4 * texSize * texSize).ToArray(), texSize);
        }

        private void SetCloudBoxUniforms()
        {
            program.SetVec3("cloudsBoxCenter", new Vector3(cloudsBoxCenter.X, cloudsBoxCenter.Y, cloudsBoxCenter.Z));
            program.SetFloat("cloudsBoxSideLength", cloudsBoxSideLength);
            program.SetFloat("cloudsBoxHeight", cloudsBoxHeight);
        }

        protected override void RenderScene()
        {
            program.Use();

            program.SetMat4("viewMtx", _camera.GetViewMatrix());
            program.SetMat4("projMtx", _camera.GetProjectionMatrix());

            GL.BindVertexArray(vaoId);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        }


        private bool _firstMove = true;

        private Vector2 _lastPos;

        private double _time;

        protected override void HandleMouseAndKeyboardInput(FrameEventArgs e)
        {
            if (!IsFocused) // Check to see if the window is focused
            {
                return;
            }

            var input = KeyboardState;

            if (input.IsKeyDown(Keys.Escape))
            {
                Close();
            }

            const float cameraSpeed = 30.5f;
            const float sensitivity = 0.2f;

            if (input.IsKeyDown(Keys.W))
            {
                _camera.Position += _camera.Front * cameraSpeed * (float)e.Time; // Forward
            }

            if (input.IsKeyDown(Keys.S))
            {
                _camera.Position -= _camera.Front * cameraSpeed * (float)e.Time; // Backwards
            }
            if (input.IsKeyDown(Keys.A))
            {
                _camera.Position -= _camera.Right * cameraSpeed * (float)e.Time; // Left
            }
            if (input.IsKeyDown(Keys.D))
            {
                _camera.Position += _camera.Right * cameraSpeed * (float)e.Time; // Right
            }
            if (input.IsKeyDown(Keys.Space))
            {
                _camera.Position += _camera.Up * cameraSpeed * (float)e.Time; // Up
            }
            if (input.IsKeyDown(Keys.LeftShift))
            {
                _camera.Position -= _camera.Up * cameraSpeed * (float)e.Time; // Down
            }


            if (!input.IsKeyDown(Keys.LeftControl))
            {
                _firstMove = true;
                return;
            }

            // Get the mouse state
            var mouse = MouseState;

            if (_firstMove) // This bool variable is initially set to true.
            {
                _lastPos = new Vector2(mouse.X, mouse.Y);
                _firstMove = false;
            }
            else
            {
                // Calculate the offset of the mouse position
                var deltaX = mouse.X - _lastPos.X;
                var deltaY = mouse.Y - _lastPos.Y;
                _lastPos = new Vector2(mouse.X, mouse.Y);

                // Apply the camera pitch and yaw (we clamp the pitch in the camera class)
                _camera.Yaw += deltaX * sensitivity;
                _camera.Pitch -= deltaY * sensitivity; // Reversed since y-coordinates range from bottom to top
            }
        }
    

        protected override void RenderGUI()
        {
            //// Enable Docking
            //ImGui.DockSpaceOverViewport();

            ImGui.ShowDemoWindow();

            ImGui.Begin("-");
            //if (ImGui.TreeNodeEx("Camera", ImGuiTreeNodeFlags.DefaultOpen))
            //{
            //    if (ImGui.DragFloat3("Camera Position", ref camera.Position, 0.01f))
            //    {
            //        SetViewMatrix();
            //    }
            //    if (ImGui.DragFloat3("Camera Target", ref cameraTarget, 0.01f))
            //    {
            //        SetViewMatrix();
            //    }
            //    if (ImGui.DragFloat("Camera FOV", ref camera.Fov, 0.1f, 10.0f, 179.0f, "%.1f", ImGuiSliderFlags.AlwaysClamp))
            //    {
            //        SetProjectionMatrix();
            //    }
            //    ImGui.TreePop();
            //}
            if (ImGui.TreeNodeEx("Clouds box", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.DragFloat3("Center", ref cloudsBoxCenter, 0.01f))
                {
                    SetCloudBoxUniforms();
                }
                if (ImGui.DragFloat("SideLength", ref cloudsBoxSideLength, 0.01f, 0.1f, float.MaxValue))
                {
                    SetCloudBoxUniforms();
                }
                if (ImGui.DragFloat("Height", ref cloudsBoxHeight, 0.01f, 0.1f, float.MaxValue))
                {
                    SetCloudBoxUniforms();
                }
                ImGui.TreePop();
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
