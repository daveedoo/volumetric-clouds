using Clouds.GLWrappers;
using ImGuiNET;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Clouds
{
    public class Application : Window
    {
        private GLWrappers.Program program;
        private GLWrappers.ComputeShader computeShader;
        private int vaoId;
        private int Shape3DTexHandle;
        private int Detail3DTexHandle;
        private const int Shape3DTexSize = 128;
        private const int Detail3DTexSize = 128;
        
        private Vector2i windowSize = defaultWindowSize;
        private System.Numerics.Vector3 cameraPosition = new(5.0f, 2.0f, 5.0f);
        private System.Numerics.Vector3 cameraTarget = new(0.0f);
        private float cameraFOV = 90; // degrees

        private System.Numerics.Vector3 cloudsBoxCenter = new(0.0f);
        private float cloudsBoxSideLength = 2.0f;
        private float cloudsBoxHeight = 2.0f;
        private Vector4 shapeSettings = new Vector4(1f,1f, 1f, 1f); 

        private static readonly Vector2i defaultWindowSize = new(1600, 900);
        public Application() : base(defaultWindowSize)
        {
            Title = "Clouds";

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
            
            program.SetVec3("cameraPos", new Vector3(cameraPosition.X, cameraPosition.Y, cameraPosition.Z));

            computeShader = new ComputeShader("../../../shaders/Perlin3D_CS.glsl");

            SetCloudBoxUniforms();
            SetViewMatrix();
            SetProjectionMatrix();
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

        private void SetupPerlinGeneratedTextures()         // shape and details 3D textures calculated inside compute shader using perlin 3D noise generation
        {
            int TexUnit = 1;      // 1 becouse first texture is used inside SetupTexture()

            int Shape3DTexHandle = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture0 + TexUnit);
            GL.BindTexture(TextureTarget.Texture3D, Shape3DTexHandle);

            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Linear);

            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);
            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapR, (int)TextureWrapMode.Clamp);

            GL.TexImage3D(TextureTarget.Texture3D, 0, PixelInternalFormat.Rgba, Shape3DTexSize, Shape3DTexSize, Shape3DTexSize, 0, PixelFormat.Rgba, PixelType.UnsignedByte, Enumerable.Repeat<byte>(0, 4 * Shape3DTexSize * Shape3DTexSize* Shape3DTexSize).ToArray());

            TexUnit++;

            int Detail3DTexHandle = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture0 + TexUnit);
            GL.BindTexture(TextureTarget.Texture3D, Detail3DTexHandle);

            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Linear);

            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);
            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapR, (int)TextureWrapMode.Clamp);

            GL.TexImage3D(TextureTarget.Texture3D, 0, PixelInternalFormat.Rgba, Detail3DTexSize, Detail3DTexSize, Detail3DTexSize, 0, PixelFormat.Rgba, PixelType.UnsignedByte, Enumerable.Repeat<byte>(0, 4 * Detail3DTexSize * Detail3DTexSize * Detail3DTexSize).ToArray());
        }

        // TODO: decide if byte type is the best
        private (byte[] data, int textureSize) GetCloudTextureData()
        {
            int texSize = 512;
            // mock
            return (Enumerable.Repeat<byte>(50, 4 * texSize * texSize).ToArray(), texSize);
        }

        private void GeneratePerlinTextures()
        {
            GL.BindImageTexture(1, Shape3DTexHandle, 0, false, 0, TextureAccess.ReadWrite, SizedInternalFormat.Rgba32f);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture3D, Shape3DTexHandle);

            GL.BindImageTexture(2, Detail3DTexHandle, 0, false, 0, TextureAccess.ReadWrite, SizedInternalFormat.Rgba32f);
            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture3D, Detail3DTexHandle);

            computeShader.Use();
            GL.DispatchCompute(32,32,1);

            // make sure writing to image has finished before read
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);
        }

        private void SetCloudBoxUniforms()
        {
            program.SetVec3("cloudsBoxCenter", new Vector3(cloudsBoxCenter.X, cloudsBoxCenter.Y, cloudsBoxCenter.Z));
            program.SetFloat("cloudsBoxSideLength", cloudsBoxSideLength);
            program.SetFloat("cloudsBoxHeight", cloudsBoxHeight);
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
            if (ImGui.TreeNodeEx("Camera", ImGuiTreeNodeFlags.DefaultOpen))
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
