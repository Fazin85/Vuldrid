using Silk.NET.GLFW;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Vuldrid.Examples
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex
    {
        public Vector2 Position;
        public RgbaFloat Color;

        public Vertex(Vector2 position, RgbaFloat color)
        {
            Position = position;
            Color = color;
        }
    }

    public unsafe class HelloWorld
    {
        private readonly Glfw _glfw;
        private WindowHandle* _window;
        private GraphicsDevice _graphicsDevice;
        private CommandList _commandList;
        private DeviceBuffer _vertexBuffer;
        private DeviceBuffer _indexBuffer;
        private Pipeline _pipeline;

        public HelloWorld()
        {
            _glfw = Glfw.GetApi();
        }

        public void Run()
        {
            if (!_glfw.Init())
            {
                throw new Exception("Failed to initialize GLFW");
            }

            _glfw.WindowHint(WindowHintClientApi.ClientApi, ClientApi.NoApi);
            _window = _glfw.CreateWindow(960, 540, "Vuldrid Hello World (GLFW)", null, null);

            if (_window == null)
            {
                throw new Exception("Failed to create GLFW window");
            }

            GraphicsDeviceOptions options = new()
            {
                PreferStandardClipSpaceYDirection = true,
                PreferDepthRangeZeroToOne = true
            };

            IntPtr windowHandle = (IntPtr)_window;
            SwapchainDescription scDesc = new(
                windowHandle,
                960, 540,
                null,
                true);

            _graphicsDevice = GraphicsDevice.CreateVulkan(options, scDesc);
            ResourceFactory factory = _graphicsDevice.ResourceFactory;

            CreateResources(factory);

            _commandList = factory.CreateCommandList();

            Console.WriteLine("Initialization successful. Running loop...");

            while (!_glfw.WindowShouldClose(_window))
            {
                _glfw.PollEvents();
                Draw();
            }

            _graphicsDevice.WaitForIdle();
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
            _pipeline.Dispose();
            _commandList.Dispose();
            _graphicsDevice.Dispose();
            _glfw.Terminate();
        }

        private void CreateResources(ResourceFactory factory)
        {
            Vertex[] vertices =
            [
                new Vertex(new Vector2(-.75f, .75f), RgbaFloat.Red),
                new Vertex(new Vector2(.75f, .75f), RgbaFloat.Green),
                new Vertex(new Vector2(-.75f, -.75f), RgbaFloat.Blue),
                new Vertex(new Vector2(.75f, -.75f), RgbaFloat.Yellow)
            ];

            _vertexBuffer = factory.CreateBuffer(new BufferDescription((uint)(vertices.Length * Marshal.SizeOf<Vertex>()), BufferUsage.VertexBuffer));
            _graphicsDevice.UpdateBuffer(_vertexBuffer, 0, vertices);

            ushort[] indices = [0, 1, 2, 3];
            _indexBuffer = factory.CreateBuffer(new BufferDescription((uint)(indices.Length * sizeof(ushort)), BufferUsage.IndexBuffer));
            _graphicsDevice.UpdateBuffer(_indexBuffer, 0, indices);

            string vertPath = Path.Combine(AppContext.BaseDirectory, "Shaders", "shader.vert.spv");
            string fragPath = Path.Combine(AppContext.BaseDirectory, "Shaders", "shader.frag.spv");

            byte[] vertexShaderSpirv = File.ReadAllBytes(vertPath);
            byte[] fragmentShaderSpirv = File.ReadAllBytes(fragPath);

            Shader vertexShader = factory.CreateShader(new ShaderDescription(ShaderStages.Vertex, vertexShaderSpirv, "main"));
            Shader fragmentShader = factory.CreateShader(new ShaderDescription(ShaderStages.Fragment, fragmentShaderSpirv, "main"));

            GraphicsPipelineDescription pipelineDesc = new()
            {
                BlendState = BlendStateDescription.SingleOverrideBlend,
                DepthStencilState = DepthStencilStateDescription.DepthOnlyLessEqual,
                RasterizerState = new RasterizerStateDescription(
                    FaceCullMode.None,
                    PolygonFillMode.Solid,
                    FrontFace.Clockwise,
                    true,
                    false),
                PrimitiveTopology = PrimitiveTopology.TriangleStrip,
                ResourceLayouts = [],
                ShaderSet = new ShaderSetDescription(
                    [
                        new VertexLayoutDescription(
                            new VertexElementDescription("Position", VertexElementFormat.Float2, VertexElementSemantic.Position),
                            new VertexElementDescription("Color", VertexElementFormat.Float4, VertexElementSemantic.Color))
                    ],
                    [vertexShader, fragmentShader]),
                Outputs = _graphicsDevice.MainSwapchain.Framebuffer.OutputDescription
            };

            _pipeline = factory.CreateGraphicsPipeline(pipelineDesc);
        }

        private void Draw()
        {
            _commandList.Begin();
            _commandList.SetFramebuffer(_graphicsDevice.MainSwapchain.Framebuffer);
            _commandList.SetViewport(0, new Viewport(0, 0, 960, 540, 0, 1));
            _commandList.SetScissorRect(0, 0, 0, 960, 540);
            _commandList.ClearColorTarget(0, RgbaFloat.Black);

            _commandList.SetPipeline(_pipeline);
            _commandList.SetVertexBuffer(0, _vertexBuffer);
            _commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
            _commandList.DrawIndexed(4, 1, 0, 0, 0);

            _commandList.End();

            _graphicsDevice.SubmitCommands(_commandList);
            _graphicsDevice.SwapBuffers();
        }
    }

    public class Program
    {
        public static void Main()
        {
            new HelloWorld().Run();
        }
    }
}
