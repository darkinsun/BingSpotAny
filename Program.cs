using Avalonia;
using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace BingSpotAny
{
    internal sealed class Program
    {
        private static Mutex? _mutex;
        private const string MutexName = "BingSpotAny_SingleInstance_Mutex";
        private const string PipeName = "BingSpotAny_IPC_Pipe";

        [STAThread]
        public static void Main(string[] args)
        {
            // Request a lock (Mutex) from the OS
            _mutex = new Mutex(true, MutexName, out bool createdNew);

            if (!createdNew)
            {
                // The lock is already acquired! The application is already running.
                // Send a "wake up and bring to front" signal to the first instance and exit immediately.
                NotifyFirstInstance();
                return; 
            }

            // We are the first instance. Start the server in the background to listen for secondary launches.
            Task.Run(() => ListenForSecondaryInstances());

            // Start Avalonia normally
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();

        private static void NotifyFirstInstance()
        {
            try
            {
                // Just establishing a connection is enough to trigger the first instance.
                using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                client.Connect(1000); // Wait up to 1 second
            }
            catch { }
        }

        private static async Task ListenForSecondaryInstances()
        {
            while (true)
            {
                try
                {
                    using var server = new NamedPipeServerStream(PipeName, PipeDirection.In);
                    await server.WaitForConnectionAsync();

                    // A second instance tried to open and connected to us!
                    // Tell Avalonia's UI Thread to bring the main window to the front.
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        App.ShowMainWindow();
                    });
                }
                catch
                {
                    // If the connection drops or an error occurs, swallow it silently and resume listening.
                }
            }
        }
    }
}