using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32; // Required for Windows Registry access

namespace BingSpotAny
{
    public static class StartupManager
    {
        // Define app name and Windows Registry path as constants for cleaner code
        private const string AppName = "BingSpotAny";
        private const string WindowsRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        public static bool IsRegistered()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Check Windows Registry instead of a startup file
                    using RegistryKey? key = Registry.CurrentUser.OpenSubKey(WindowsRegistryKey, false);
                    return key?.GetValue(AppName) != null;
                }

                // Linux & macOS: Fallback to file-based check
                string path = GetStartupFilePath();
                return !string.IsNullOrEmpty(path) && File.Exists(path);
            }
            catch
            {
                return false;
            }
        }

        public static void Register()
        {
            try
            {

                // Cross-platform app base directory (e.g., bin/Debug/net8.0/)
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string exeName = Assembly.GetEntryAssembly()?.GetName().Name ?? AppName;
                string exePath = Path.Combine(baseDir, exeName);


                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    exePath += ".exe";


                    // If running via dotnet run (no native binary compiled yet), fallback to invoking the DLL directly
                    string execCommand = File.Exists(exePath) ? $"\"{exePath}\"" : $"dotnet \"{Path.Combine(baseDir, exeName + ".dll")}\"";


                    // Register to Windows Registry directly (No VBScript needed)
                    using RegistryKey? key = Registry.CurrentUser.OpenSubKey(WindowsRegistryKey, true);
                    key?.SetValue(AppName, execCommand);
                }
                else
                {
                    // --- LINUX & MACOS SECTION ---
                    string execCommand = File.Exists(exePath) ? $"\"{exePath}\"" : $"dotnet \"{Path.Combine(baseDir, exeName + ".dll")}\"";
                    string path = GetStartupFilePath();

                    if (string.IsNullOrEmpty(path)) return;

                    string? dir = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        string desktopContent = "[Desktop Entry]\n" +
                                                "Type=Application\n" +
                                                $"Name={AppName}\n" +
                                                $"Exec={execCommand}\n" +
                                                "Terminal=false\n" +
                                                "Hidden=false\n" +
                                                "NoDisplay=false\n";
                        File.WriteAllText(path, desktopContent);
                        // Standard rw-r--r-- permission is sufficient for XDG autostart
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        // Launchd cannot execute a single string command with spaces (like a shell does). 
                        // We must provide the executable and its arguments separately within an XML Array.
                        string plistArgs;

                        if (File.Exists(exePath))
                        {
                            // If the application is compiled as a native executable (.app or self-contained)
                            plistArgs = $"    <string>{exePath}</string>\n";
                        }
                        else
                        {
                            // If running via 'dotnet run' or directly invoking the DLL
                            // Since Launchd doesn't know environment variables (like PATH), we must provide the absolute path to 'dotnet'
                            string dotnetPath = "/usr/local/share/dotnet/dotnet";
                            string dllPath = Path.Combine(baseDir, exeName + ".dll");

                            plistArgs = $"    <string>{dotnetPath}</string>\n" +
                                        $"    <string>{dllPath}</string>\n";
                        }

                        string plistContent = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
                                              "<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n" +
                                              "<plist version=\"1.0\">\n" +
                                              "<dict>\n" +
                                              "  <key>Label</key>\n" +
                                              "  <string>com.bingspotany.app</string>\n" +
                                              "  <key>ProgramArguments</key>\n" +
                                              "  <array>\n" +
                                              plistArgs +
                                              "  </array>\n" +
                                              "  <key>RunAtLoad</key>\n" +
                                              "  <true/>\n" +
                                              "</dict>\n" +
                                              "</plist>";

                        File.WriteAllText(path, plistContent);
                    }

                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to register startup: {ex.Message}");
            }
        }

        public static void Unregister()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Unregister from Windows Registry
                    using RegistryKey? key = Registry.CurrentUser.OpenSubKey(WindowsRegistryKey, true);
                    key?.DeleteValue(AppName, false);
                }
                else
                {
                    // Linux & macOS: Delete the autostart files
                    string path = GetStartupFilePath();
                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to unregister startup: {ex.Message}");
            }
        }

        private static string GetStartupFilePath()
        {

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                string configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") ??
                                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
                return Path.Combine(configHome, "autostart", "BingSpotAny.desktop");
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "LaunchAgents", "com.bingspotany.app.plist");
            }

            return string.Empty;
        }
    }
}