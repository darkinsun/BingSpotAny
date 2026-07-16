/*
 * BEGIN LICENSE
 * Copyright (c) 2026 BingSpotAny Contributors
 * * This program is free software: you can redistribute it and/or modify it
 * under the terms of the GNU General Public License version 3, as published
 * by the Free Software Foundation.
 *
 * This program is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranties of
 * MERCHANTABILITY, SATISFACTORY QUALITY, or FITNESS FOR A PARTICULAR
 * PURPOSE.  See the GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with this program.  If not, see <http://www.gnu.org/licenses/>.
 * END LICENSE
 */
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
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

            // Otherwise, treat it as a relative path inside the platform-specific application folder
            return Path.Combine(WallpaperSettings.GetBaseDataDirectory(), path);
        }

        public async Task<bool> ApplyWallpaperAsync(string imagePath)
        {
            string scriptPath = DetermineScriptPath();
            string resolvedScriptPath = ResolvePath(scriptPath);
            
            Console.WriteLine($"[DEBUG] Resolved Script Path: {resolvedScriptPath}");
            Console.WriteLine($"[DEBUG] Target Image Path: {imagePath}");

            if (string.IsNullOrEmpty(resolvedScriptPath) || !File.Exists(resolvedScriptPath))
            {
                // Absolute configuration fallback if file missing or empty
                Console.WriteLine($"[DEBUG] Error: Script file couldn't be found!");
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
                    Console.WriteLine($"\n[DETAILED SCRIPT ERROR] -> {errorOutput}\n");
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
            string appBaseDir = AppDomain.CurrentDomain.BaseDirectory;
            string baseDataDir = WallpaperSettings.GetBaseDataDirectory();
            string scriptsTargetDir = Path.Combine(baseDataDir, "scripts");

            // Normalize paths to safely compare them (removes trailing slashes)
            string normalizedAppBase = Path.GetFullPath(appBaseDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string normalizedDataDir = Path.GetFullPath(baseDataDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            // Windows paths are case-insensitive, Unix paths are case-sensitive
            bool isSameDirectory = string.Equals(
                normalizedAppBase, 
                normalizedDataDir, 
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

            // Check if an update is needed by comparing settings with the script version
            bool isUpdateNeeded = _settings.ScriptsVersion != App.ScriptVersion;

            if (isUpdateNeeded)
            {
                // IMPORTANT: Prevent backup and deletion if running in portable mode (source == target).
                // Deleting the target directory in portable mode would destroy the shipped default scripts.
                // Backup existing scripts if the directory exists and has files
                if (!isSameDirectory && Directory.Exists(scriptsTargetDir))
                {
                    string[] existingFiles = Directory.GetFiles(scriptsTargetDir, "*", SearchOption.AllDirectories);
                    if (existingFiles.Length > 0)
                    {
                        try
                        {
                            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                            string safeOldVersion = string.IsNullOrWhiteSpace(_settings.ScriptsVersion) || _settings.ScriptsVersion == "0.0.0" 
                                ? "legacy" 
                                : _settings.ScriptsVersion;
                                
                            string backupZipPath = Path.Combine(baseDataDir, $"scripts_backup_v{safeOldVersion}_{timestamp}.zip");

                            ZipFile.CreateFromDirectory(scriptsTargetDir, backupZipPath);
                            Console.WriteLine($"[INFO] Old scripts backed up safely to: {backupZipPath}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[ERROR] Failed to backup existing scripts: {ex.Message}");
                        }
                    }

                    // Delete the directory safely to start fresh with the new version
                    try { Directory.Delete(scriptsTargetDir, true); } catch { }
                }

                // Update the settings with the new version and save immediately
                _settings.ScriptsVersion = App.ScriptVersion;
                SettingsManager.SaveSettings(_settings);
            }

            // --- PROVISIONING LOGIC ---

            string winPath = ResolvePath(_settings.WindowsScriptPath);
            string linuxPath = ResolvePath(_settings.LinuxScriptPath);
            string macPath = ResolvePath(_settings.MacOsScriptPath);

            // Safely create user data directories if they don't exist
            string? winDir = Path.GetDirectoryName(winPath);
            if (!string.IsNullOrEmpty(winDir) && !Directory.Exists(winDir)) Directory.CreateDirectory(winDir);

            string? linuxDir = Path.GetDirectoryName(linuxPath);
            if (!string.IsNullOrEmpty(linuxDir) && !Directory.Exists(linuxDir)) Directory.CreateDirectory(linuxDir);

            string? macDir = Path.GetDirectoryName(macPath);
            if (!string.IsNullOrEmpty(macDir) && !Directory.Exists(macDir)) Directory.CreateDirectory(macDir);

            // --- SEED PATTERN: Check the physical installation folder for shipped scripts ---

            // 1. Provision Windows Script
            if (!File.Exists(winPath))
            {
                string shippedWinPath = Path.Combine(appBaseDir, _settings.WindowsScriptPath);
                if (File.Exists(shippedWinPath))
                {
                    File.Copy(shippedWinPath, winPath);
                }
                else
                {
                    // Fallback if shipped file is missing: Uses a self-contained PowerShell script
    // to invoke Windows SystemParametersInfo via P/Invoke.
    string winContent = "@echo off\r\n" +
                        "set \"IMAGE_PATH=%~1\"\r\n" +
                        "if \"%IMAGE_PATH%\"==\"\" exit /b 1\r\n" +
                        "powershell -ExecutionPolicy Bypass -WindowStyle Hidden -Command \"Add-Type -TypeDefinition 'using System; using System.Runtime.InteropServices; public class Wallpaper { [DllImport(\\\"user32.dll\\\", CharSet=CharSet.Auto)] public static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni); public static void Set(string path) { SystemParametersInfo(20, 0, path, 3); } }'; [Wallpaper]::Set('%IMAGE_PATH%')\"";

                    File.WriteAllText(winPath, winContent);
                }
            }

            // 2. Provision Linux Script
            if (!File.Exists(linuxPath))
            {
                string shippedLinuxPath = Path.Combine(appBaseDir, _settings.LinuxScriptPath);
                if (File.Exists(shippedLinuxPath))
                {
                    // Copy your 370-line masterpiece!
                    File.Copy(shippedLinuxPath, linuxPath);
                }
                else
                {
                    // Fallback if shipped file is missing
                    string linuxContent = "#!/bin/sh\n" +
                                          "# Linux wallpaper switcher leveraging Variety engine\n" +
                                          "variety --set-wallpaper \"$1\"";
                    File.WriteAllText(linuxPath, linuxContent);
                }

                // Always ensure it's executable
                try { Process.Start("chmod", $"+x \"{linuxPath}\"")?.WaitForExit(); } catch { }
            }

            // 3. Provision macOS Script
            if (!File.Exists(macPath))
            {
                string shippedMacPath = Path.Combine(appBaseDir, _settings.MacOsScriptPath);
                if (File.Exists(shippedMacPath))
                {
                    File.Copy(shippedMacPath, macPath);
                }
                else
                {
                    // Fallback if shipped file is missing
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
                }

                // Always ensure it's executable
                try { Process.Start("chmod", $"+x \"{macPath}\"")?.WaitForExit(); } catch { }
            }
        }
    }
}