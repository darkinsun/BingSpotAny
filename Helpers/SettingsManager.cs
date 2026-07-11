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
using System.Diagnostics;
using System.Text.Json;

namespace BingSpotAny
{
    public static class SettingsManager
    {
        private static readonly string SettingsFilePath = Path.Combine(WallpaperSettings.GetBaseDataDirectory(), "settings.json");
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public static WallpaperSettings LoadSettings()
        {
            try
            {
                if (!File.Exists(SettingsFilePath))
                {
                    // Create defaults if file does not exist
                    var defaultSettings = new WallpaperSettings();
                    SaveSettings(defaultSettings);
                    return defaultSettings;
                }

                string jsonContent = File.ReadAllText(SettingsFilePath);
                return JsonSerializer.Deserialize<WallpaperSettings>(jsonContent, JsonOptions) ?? new WallpaperSettings();
            }
            catch (Exception)
            {
                // Fallback to defaults if JSON parsing fails
                return new WallpaperSettings();
            }
        }

        public static void SaveSettings(WallpaperSettings settings)
        {
            try
            {
                string jsonContent = JsonSerializer.Serialize(settings, JsonOptions);
                File.WriteAllText(SettingsFilePath, jsonContent);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }
    }
}