using Avalonia;
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using System.Diagnostics; // Required for Debug outputs

namespace BingSpotAny
{
    internal sealed class Program
    {
        // Object to hold our universal file lock, preventing the garbage collector from releasing it early
        private static FileStream? _instanceLock; 
        private const string PipeName = "BingSpotAny_IPC_Pipe";

        [STAThread]
        public static void Main(string[] args)
        {
            // Define the path for our lock file in the OS's temporary directory
            string lockFilePath = Path.Combine(Path.GetTempPath(), "BingSpotAny_SingleInstance.lock");

            try
            {
                // Attempt to create/open the file with FileShare.None (Strict Lock).
                // If the app crashes or is force-closed, the OS automatically releases this file lock.
                _instanceLock = new FileStream(lockFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                Debug.WriteLine("[SINGLE-INSTANCE] Successfully acquired the file lock. Starting as the primary instance.");
            }
            catch (IOException)
            {
                // If we enter this catch block, the file is already locked! (Another instance is running)
                Debug.WriteLine("[SINGLE-INSTANCE] File lock denied. Another instance is already running.");
                
                // Send a wake-up signal to the primary instance and exit silently.
                NotifyFirstInstance();
                return; 
            }

            // We are the primary instance. Start listening for secondary launch attempts in the background.
            Task.Run(() => ListenForSecondaryInstances());

            // Start the Avalonia application normally
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
                Debug.WriteLine("[SINGLE-INSTANCE] Attempting to notify the primary instance via Named Pipe...");
                
                // Just establishing a connection is enough to trigger the primary instance's listener.
                using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                client.Connect(1000); // Wait up to 1 second for the connection
                
                Debug.WriteLine("[SINGLE-INSTANCE] Notification sent successfully. Exiting secondary instance.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SINGLE-INSTANCE] Failed to notify the primary instance: {ex.Message}");
            }
        }

        private static async Task ListenForSecondaryInstances()
        {
            Debug.WriteLine("[SINGLE-INSTANCE] Named Pipe server is now listening for secondary instances.");
            
            while (true)
            {
                try
                {
                    using var server = new NamedPipeServerStream(PipeName, PipeDirection.In);
                    await server.WaitForConnectionAsync();

                    Debug.WriteLine("[SINGLE-INSTANCE] Connection received from a secondary instance. Waking up Main Window...");

                    // A secondary instance tried to open and connected to us!
                    // Instruct Avalonia's UI Thread to bring the main window to the front.
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        App.ShowMainWindow();
                    });
                }
                catch (Exception ex)
                {
                    // If the connection drops or an error occurs, log it and resume listening.
                    Debug.WriteLine($"[SINGLE-INSTANCE] IPC Listener Error: {ex.Message}");
                }
            }
        }
    }
}