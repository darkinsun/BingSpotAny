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