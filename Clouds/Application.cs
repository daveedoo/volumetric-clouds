using Clouds.GLWrappers;
using ImGuiNET;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Runtime.CompilerServices;
using OpenTK.Windowing.Common;
using System.Threading.Channels;

namespace Clouds
{
    struct AnimationSettings
    {
        public System.Numerics.Vector2 ShapeOffset;
        public System.Numerics.Vector2 DetailOffset;
        public System.Numerics.Vector2 ShapeSpeed;
        public System.Numerics.Vector2 DetailSpeed;
    }
    public class Application : Window
    {
        private GLWrappers.Program program;
        private Camera _camera;
        private GLWrappers.ComputeShader computeShader;
        private int vaoId;
        private int Shape3DTexHandle;
        private int Detail3DTexHandle;
        private const int Shape3DTexSize = 32;
        private const int Detail3DTexSize = 32;
        
        private Vector2i windowSize = defaultWindowSize;
        private Vector3 cameraPosition = new(5.0f, 3.0f, 0.0f);
        private Vector3 lightPos = new(1.0f,0.2f,0.0f);
        private int lightmarchStepCount = 20;
        private float cloudAbsorption = 1.0f;
        private float sunAbsorption = 0.2f;
        private float minLightEnergy = 0.2f;
        private float densityEps = 0.01f;

        private System.Numerics.Vector3 cloudsBoxCenter = new(0.0f);
        private float cloudsBoxSideLength = 2.0f;
        private float cloudsBoxHeight = 2.0f;
        private System.Numerics.Vector4 shapeSettings = new System.Numerics.Vector4(10f,5f, 4f, 2f);
        private System.Numerics.Vector4 detailSettings = new System.Numerics.Vector4(4f, 3f, 2f, 2f);

        private float globalCoverage = 0.5f;
        private float globalDensity = 0.5f;

        AnimationSettings animation_settings = new AnimationSettings();

        private static readonly Vector2i defaultWindowSize = new(1600, 900);
        public Application() : base(defaultWindowSize)
        {
            Title = "Clouds";
            _camera = new Camera(cameraPosition, (float)windowSize.X / windowSize.Y);
            _camera.Yaw = 180;
            _camera.Pitch = -10;
            SetupShaders();
            SetupVAO();
            SetupCloudsTexture(); 
            SetupPerlinGeneratedTextures();
            SetupBlueNoiseTexture();
            GeneratePerlinTextures();
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
            using Shader vertexShader = new(ShaderType.VertexShader, "../../../shaders/vertex.glsl");
            using Shader fragmentShader = new(ShaderType.FragmentShader, "../../../shaders/fragment.glsl");
            program = new(vertexShader, fragmentShader);
            

            computeShader = new ComputeShader("../../../shaders/Perlin3D_CS.glsl");

            SetCameraPosition();
            SetCloudBoxUniforms();
            SetGlobalUniforms();
        }

        private void SetupCloudsTexture()
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

        private void SetupBlueNoiseTexture()
        {
            const int cloudsTextureUnit = 3; /// we have already 3 textures initialized

            int texId = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture0 + cloudsTextureUnit);
            GL.BindTexture(TextureTarget.Texture2D, texId);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);

            var blueNoiseSize = 128;
            byte[] data = GetBlueNoiseData(blueNoiseSize);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, blueNoiseSize, blueNoiseSize, 0, PixelFormat.Rgba, PixelType.UnsignedByte/* or different? */, data);

            program.SetInt("blueNoiseTexture", cloudsTextureUnit);
        }

        private void SetupPerlinGeneratedTextures()         // shape and details 3D textures calculated inside compute shader using perlin 3D noise generation
        {
            int TexUnit = 1;      // 1 becouse first texture is used inside SetupTexture()

            Shape3DTexHandle = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture0 + TexUnit);
            GL.BindTexture(TextureTarget.Texture3D, Shape3DTexHandle);

            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Linear);

            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);
            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapR, (int)TextureWrapMode.Clamp);

            GL.TexImage3D(TextureTarget.Texture3D, 0, PixelInternalFormat.Rgba32f, Shape3DTexSize, Shape3DTexSize, Shape3DTexSize, 0, PixelFormat.Rgba, PixelType.UnsignedByte, GenerateRandom3DBytes(Shape3DTexSize));
            program.SetInt("shapeTexture", TexUnit);
            TexUnit++;

            Detail3DTexHandle = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture0 + TexUnit);
            GL.BindTexture(TextureTarget.Texture3D, Detail3DTexHandle);

            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Linear);

            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);
            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapR, (int)TextureWrapMode.Clamp);

            GL.TexImage3D(TextureTarget.Texture3D, 0, PixelInternalFormat.Rgba32f, Detail3DTexSize, Detail3DTexSize, Detail3DTexSize, 0, PixelFormat.Rgba, PixelType.UnsignedByte, GenerateRandom3DBytes(Detail3DTexSize));
            program.SetInt("detailsTexture", TexUnit);
        }

        private byte[] GenerateRandom3DBytes(int arraySize)
        {
            var rand = new Random();
            var res = new byte[4 * (int)Math.Pow(arraySize, 3)];
            rand.NextBytes(res);
            return res;
        }

        // TODO: decide if byte type is the best
        private (byte[] data, int textureSize) GetCloudTextureData()
        {
            int texSize = 128;
            // mock
            return (Enumerable.Repeat<byte>(255, 4 * texSize * texSize).ToArray(), texSize);
        }

        // TODO: decide if byte type is the best
        private byte[] GetBlueNoiseData(int arraySize)
        {
            var rand = new Random();
            var res = new byte[4 * (int)Math.Pow(arraySize, 2)];
            rand.NextBytes(res);
            return res;
        }

        private void GeneratePerlinTextures()
        {
            // https://stackoverflow.com/questions/45282300/writing-to-an-empty-3d-texture-in-a-compute-shader?fbclid=IwAR2Bk9P__lQ4EDLkbbFIvU_zauYKAsd1HFl6ZOQO5z8NzOoT9716FByflEs - layered should be true in BindImageTexture function?
            GL.BindImageTexture(1, Shape3DTexHandle, 0, true, 0, TextureAccess.ReadWrite, SizedInternalFormat.Rgba32f);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture3D, Shape3DTexHandle);

            GL.BindImageTexture(2, Detail3DTexHandle, 0, true, 0, TextureAccess.ReadWrite, SizedInternalFormat.Rgba32f);
            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture3D, Detail3DTexHandle);

            computeShader.Use();

            // OpenTK use Vector4 from OpenTK.Mathematics and ImGUI need Vector4 from System.Numerics
            computeShader.SetVec4("shapeSettings", new Vector4(shapeSettings.X, shapeSettings.Y, shapeSettings.Z, shapeSettings.W));
            computeShader.SetVec4("detailSettings", new Vector4(detailSettings.X, detailSettings.Y, detailSettings.Z, detailSettings.W));
            GL.DispatchCompute(32,32,1);

            // make sure writing to image has finished before read
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderImageAccessBarrierBit);
        }


        // Here simulation of animation is gonna be calculated depending on actual frame time (and buffers gonna be updated)
        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);
            var time = args.Time;
            UpdateAnimation((float)time);
        }

        float accumulatedTime = 0;
        private void UpdateAnimation(float dt)
        {
            accumulatedTime += dt / 100;
            var currTime = accumulatedTime - (int)accumulatedTime;

            animation_settings.ShapeOffset = animation_settings.ShapeSpeed * (new System.Numerics.Vector2(currTime, currTime));
            animation_settings.DetailOffset = animation_settings.DetailSpeed * (new System.Numerics.Vector2(currTime, currTime));

            program.SetVec2("shapeOffset", new Vector2(animation_settings.ShapeOffset.X, animation_settings.ShapeOffset.Y) );
            program.SetVec2("detailsOffset", new Vector2(animation_settings.DetailOffset.X, animation_settings.DetailOffset.Y));
        }

        private void SetCameraPosition()
        {
            program.SetVec3("cameraPos", _camera.Position);
        }
        private void SetCloudBoxUniforms()
        {
            program.SetVec3("cloudsBoxCenter", new Vector3(cloudsBoxCenter.X, cloudsBoxCenter.Y, cloudsBoxCenter.Z));
            program.SetFloat("cloudsBoxSideLength", cloudsBoxSideLength);
            program.SetFloat("cloudsBoxHeight", cloudsBoxHeight);
        }

        private void SetGlobalUniforms()
        {
            program.SetFloat("globalCoverage", globalCoverage);
            program.SetFloat("globalDensity", globalDensity);

            program.SetColor4("clearColor", clearColor);

            program.SetFloat("densityEps", densityEps);
            // Uncomment when lightmarching will be used to change cloud color (will impact result)
            //program.SetVec3("lightPos", lightPos);
            //program.SetInt("lightmarchStepCount",lightmarchStepCount);
            //program.SetFloat("cloudAbsorption", cloudAbsorption);
            //program.SetFloat("minLightEnergy", minLightEnergy);
            //program.SetFloat("sunAbsorption", sunAbsorption);
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

            var keyboardInput = KeyboardState;
            var mouseInput = MouseState;

            if (keyboardInput.IsKeyDown(Keys.Escape))
            {
                Close();
            }

            const float cameraSpeed = 30.5f;
            const float sensitivity = 0.2f;

            if (keyboardInput.IsKeyDown(Keys.W))
            {
                _camera.Position += _camera.Front * cameraSpeed * (float)e.Time; // Forward
                SetCameraPosition();
            }

            if (keyboardInput.IsKeyDown(Keys.S))
            {
                _camera.Position -= _camera.Front * cameraSpeed * (float)e.Time; // Backwards
                SetCameraPosition();
            }
            if (keyboardInput.IsKeyDown(Keys.A))
            {
                _camera.Position -= _camera.Right * cameraSpeed * (float)e.Time; // Left
                SetCameraPosition();
            }
            if (keyboardInput.IsKeyDown(Keys.D))
            {
                _camera.Position += _camera.Right * cameraSpeed * (float)e.Time; // Right
                SetCameraPosition();
            }
            if (keyboardInput.IsKeyDown(Keys.Space))
            {
                _camera.Position += _camera.Up * cameraSpeed * (float)e.Time; // Up
                SetCameraPosition();
            }
            if (keyboardInput.IsKeyDown(Keys.LeftShift))
            {
                _camera.Position -= _camera.Up * cameraSpeed * (float)e.Time; // Down
                SetCameraPosition();
            }


            if (!mouseInput.IsButtonDown(MouseButton.Right) && !mouseInput.IsButtonDown(MouseButton.Middle))
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
            if(ImGui.TreeNodeEx("Texture generation", ImGuiTreeNodeFlags.DefaultOpen))  
            {
                if(ImGui.DragFloat4("Shape settings", ref shapeSettings, 0.01f))
                {
                    GeneratePerlinTextures();
                }
                if (ImGui.DragFloat4("Detail settings", ref detailSettings, 0.01f))
                {
                    GeneratePerlinTextures();
                }
                ImGui.TreePop();
            }
            if (ImGui.TreeNodeEx("Texture settings", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.DragFloat4("Shape settings", ref shapeSettings, 0.01f))
                {
                    GeneratePerlinTextures();
                }
                if (ImGui.DragFloat4("Detail settings", ref detailSettings, 0.01f))
                {
                    GeneratePerlinTextures();
                }
                ImGui.TreePop();
            }
            if (ImGui.TreeNodeEx("Animation", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.DragFloat2("Shape animation", ref animation_settings.ShapeSpeed, 0.01f);
                ImGui.DragFloat2("Detail animation", ref animation_settings.DetailSpeed, 0.01f);
                ImGui.TreePop();
            }

            if (ImGui.TreeNodeEx("Ray marching", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.DragInt("Lighmarching step count", ref lightmarchStepCount))
                {
                    SetGlobalUniforms();
                }
                if (ImGui.DragFloat("Cloud absorption", ref cloudAbsorption))
                {
                    SetGlobalUniforms();
                }
                if (ImGui.DragFloat("Sun absorption", ref sunAbsorption))
                {
                    SetGlobalUniforms();
                }
                if (ImGui.DragFloat("Minimum light energy", ref minLightEnergy))
                {
                    SetGlobalUniforms();
                }
                if (ImGui.DragFloat("Density epsilon", ref densityEps))
                {
                    SetGlobalUniforms();
                }
            }

            if (ImGui.TreeNodeEx("Global", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.DragFloat("Global Coverage", ref globalCoverage, 0.01f, 0.0f, 1.0f))
                {
                    SetGlobalUniforms();
                }
                if (ImGui.DragFloat("Global Density", ref globalDensity, 0.01f, 0.0f, 0.5f))
                {
                    SetGlobalUniforms();
                }
                var backgroundColor = new System.Numerics.Vector3(clearColor.R, clearColor.G, clearColor.B);
                if (ImGui.ColorPicker3("Background color", ref backgroundColor))
                {
                    clearColor.R = backgroundColor[0];
                    clearColor.G = backgroundColor[1];
                    clearColor.B = backgroundColor[2];
                    SetGlobalUniforms();
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
