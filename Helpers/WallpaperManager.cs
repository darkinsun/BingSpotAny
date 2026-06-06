using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace BingSpotAny
{
    public class WallpaperManager
    {
        private readonly WallpaperSettings _settings;

        public WallpaperManager(WallpaperSettings settings)
        {
            _settings = settings;
            EnsureDefaultScriptsExist();
        }

        // Helper method to safely resolve absolute vs relative paths
        private string ResolvePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;
            
            // If the user selected an external absolute path (e.g., /usr/bin/script.sh or C:\scripts\script.bat)
            // Do NOT combine it with the app folder.
            if (Path.IsPathRooted(path)) return path;

            // Otherwise, treat it as a relative path inside the application folder
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
        }

        public async Task<bool> ApplyWallpaperAsync(string imagePath)
        {
            string scriptPath = DetermineScriptPath();
            string resolvedScriptPath = ResolvePath(scriptPath);
            
            Console.WriteLine($"[DEBUG] Çözümlenen Script Yolu: {resolvedScriptPath}");
            Console.WriteLine($"[DEBUG] Gönderilen Resim Yolu: {imagePath}");

            if (string.IsNullOrEmpty(resolvedScriptPath) || !File.Exists(resolvedScriptPath))
            {
                // Absolute configuration fallback if file missing or empty
                Console.WriteLine($"[DEBUG] Error: Script file couldn't be found.!");
                return false;
            }

            // Execute primary wallpaper script
            bool mainScriptSuccess = await RunScriptAsync(resolvedScriptPath, imagePath);

            // Execute optional user-defined post-script upon success
            if (mainScriptSuccess && _settings.EnablePostScript && !string.IsNullOrEmpty(_settings.PostScriptPath))
            {
                string resolvedPostScriptPath = ResolvePath(_settings.PostScriptPath);
                if (File.Exists(resolvedPostScriptPath))
                {
                    await RunScriptAsync(resolvedPostScriptPath, imagePath);
                }
            }

            return mainScriptSuccess;
        }

        private string DetermineScriptPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return _settings.WindowsScriptPath;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return _settings.LinuxScriptPath;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return _settings.MacOsScriptPath;

            return _settings.CustomOsScriptPath;
        }

        private async Task<bool> RunScriptAsync(string scriptPath, string targetArgument)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : "/bin/sh",
                    // For Windows pass via /c, for Unix pass via direct script shell execution
                    Arguments = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
                        ? $"/c \"\"{scriptPath}\" \"{targetArgument}\"\""
                        : $"\"{scriptPath}\" \"{targetArgument}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var process = Process.Start(startInfo);
                if (process == null) return false;

                await process.WaitForExitAsync();

                string errorOutput = await process.StandardError.ReadToEndAsync();
                if (!string.IsNullOrWhiteSpace(errorOutput))
                {
                    Console.WriteLine($"\n[macOS DETAILED ERROR] -> {errorOutput}\n");
                }
                
                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] C# error while trying to start the script: {ex.Message}");
                return false;
            }
        }

        private void EnsureDefaultScriptsExist()
        {
            string winPath = ResolvePath(_settings.WindowsScriptPath);
            string linuxPath = ResolvePath(_settings.LinuxScriptPath);
            string macPath = ResolvePath(_settings.MacOsScriptPath);

            // Safely create directories if they don't exist based on resolved paths
            string? winDir = Path.GetDirectoryName(winPath);
            if (!string.IsNullOrEmpty(winDir) && !Directory.Exists(winDir)) Directory.CreateDirectory(winDir);

            string? linuxDir = Path.GetDirectoryName(linuxPath);
            if (!string.IsNullOrEmpty(linuxDir) && !Directory.Exists(linuxDir)) Directory.CreateDirectory(linuxDir);

            string? macDir = Path.GetDirectoryName(macPath);
            if (!string.IsNullOrEmpty(macDir) && !Directory.Exists(macDir)) Directory.CreateDirectory(macDir);

            // Provision Default Windows Batch File
            if (!File.Exists(winPath))
            {
                string winContent = "@echo off\r\n" +
                                    ":: Windows native wallpaper backup switcher via PowerShell core\r\n" +
                                    "powershell -ExecutionPolicy Bypass -Command \"RUNDLL32.EXE user32.dll,UpdatePerUserSystemParameters; SystemParametersInfo 20, 0, '%1', 1\"";
                File.WriteAllText(winPath, winContent);
            }

            // Provision Default Linux Bash File
            if (!File.Exists(linuxPath))
            {
                string linuxContent = "#!/bin/sh\n" +
                                      "# Linux wallpaper switcher leveraging Variety engine\n" +
                                      "variety --set-wallpaper \"$1\"";
                File.WriteAllText(linuxPath, linuxContent);
                
                // Grant executable permissions on Unix systems
                try { Process.Start("chmod", $"+x \"{linuxPath}\"")?.WaitForExit(); } catch { }
            }

            if (!File.Exists(macPath))
            {
                string macContent = "#!/bin/sh\n" +
                                    "# macOS Wallpaper Switcher for BingSpotAny\n" +
                                    "# Changes the wallpaper by sending an AppleScript command to System Events.\n\n" +
                                    "IMAGE_PATH=\"$1\"\n\n" +
                                    "# Check if the argument is empty\n" +
                                    "if [ -z \"$IMAGE_PATH\" ]; then\n" +
                                    "    exit 1\n" +
                                    "fi\n\n" +
                                    "# Update the picture of every desktop (supports multi-monitor setups)\n" +
                                    "osascript -e \"tell application \\\"System Events\\\" to set picture of every desktop to \\\"$IMAGE_PATH\\\"\"";

                File.WriteAllText(macPath, macContent);

                // Grant executable permissions on Unix systems (macOS dahil)
                try { Process.Start("chmod", $"+x \"{macPath}\"")?.WaitForExit(); } catch { }
            }
        }
    }
}