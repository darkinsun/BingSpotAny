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
using System.IO;
using System.Runtime.InteropServices;

namespace BingSpotAny
{
    public class WallpaperSettings
    {
        // Default relative paths for the automatically generated scripts
        public string WindowsScriptPath { get; set; } = Path.Combine("scripts", "set_wallpaper.bat");
        public string LinuxScriptPath { get; set; } = Path.Combine("scripts", "set_wallpaper.sh");
        public string MacOsScriptPath { get; set; } = Path.Combine("scripts", "set_wallpaper.apple");
        public string CustomOsScriptPath { get; set; } = string.Empty;
        
        // Post-execution script configuration
        public bool EnablePostScript { get; set; } = false;
        public string PostScriptPath { get; set; } = string.Empty;

        // Current applied wallpaper path
        public string CurrentWallpaperPath { get; set; } = string.Empty;

        // NEW: Option to save all downloaded wallpapers to the archive folder
        public bool ArchiveAllWallpapers { get; set; } = true; 
        
        // Watermark Settings
        public bool EnableWatermark { get; set; } = true;
        public string WatermarkFontFamily { get; set; } = "sans-serif";
        public int WatermarkFontSize { get; set; } = 18;
        public string WatermarkColor { get; set; } = "#FFFFFF";
        public string WatermarkPosition { get; set; } = "TopRight";

        // NEW: Auto-Change and Timer Settings
        public bool AutoChangeEnabled { get; set; } = false;
        public string AutoChangeTime { get; set; } = "09:00"; // 24-hour format (HH:mm)
        public string DefaultProvider { get; set; } = "Bing";  // "Bing" or "SpotLight"

        // High-precision state tracking (Unix Timestamp in seconds)
        public long LastAutoChangeTime { get; set; } = 0; 
        
        // OS Integration
        public bool RunAtStartup { get; set; } = false;

        // --- CROSS-PLATFORM PATH RESOLVER ---
        // Provides the correct writable directory based on the operating system
        public static string GetBaseDataDirectory()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows portable behavior: use the application's executable directory
                return AppDomain.CurrentDomain.BaseDirectory;
            }
            
            // Linux & macOS behavior: use the user's local data folder (e.g., ~/.local/share/)
            string systemDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(systemDataFolder, "BingSpotAny");
        }
    }
}