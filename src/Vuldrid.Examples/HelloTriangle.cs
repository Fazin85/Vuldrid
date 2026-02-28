using Silk.NET.GLFW;

namespace Vuldrid.Examples
{
    public unsafe class HelloTriangle
    {
        private readonly Glfw _glfw;
        private WindowHandle* _window;
        private GraphicsDevice _graphicsDevice;
        private CommandList _commandList;

        public HelloTriangle()
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
            _window = _glfw.CreateWindow(960, 540, "Vuldrid Hello Triangle (GLFW)", null, null);

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

            // This verifies that GLFW surface creation and Vulkan initialization are working.
            _graphicsDevice = GraphicsDevice.CreateVulkan(options, scDesc);

            _commandList = _graphicsDevice.ResourceFactory.CreateCommandList();

            Console.WriteLine("Initialization successful. Running loop...");

            while (!_glfw.WindowShouldClose(_window))
            {
                _glfw.PollEvents();
                Draw();
            }

            _graphicsDevice.WaitForIdle();
            _commandList.Dispose();
            _graphicsDevice.Dispose();
            _glfw.Terminate();
        }

        private void Draw()
        {
            _commandList.Begin();
            _commandList.SetFramebuffer(_graphicsDevice.MainSwapchain.Framebuffer);

            // Pulsing color to show it's alive
            float time = (float)_glfw.GetTime();
            RgbaFloat clearColor = new(
                (MathF.Sin(time) + 1f) / 2f,
                (MathF.Cos(time) + 1f) / 2f,
                0.5f,
                1.0f);

            _commandList.ClearColorTarget(0, clearColor);
            _commandList.End();

            _graphicsDevice.SubmitCommands(_commandList);
            _graphicsDevice.SwapBuffers();
        }
    }

    public class Program
    {
        public static void Main()
        {
            new HelloTriangle().Run();
        }
    }
}
