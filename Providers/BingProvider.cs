using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using BingSpotAny.Models;

namespace BingSpotAny.Providers
{
    // Internal classes to parse the specific Bing JSON format
    internal class BingImageInfo
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("copyright")]
        public string Copyright { get; set; } = string.Empty;
    }

    internal class BingApiResponse
    {
        [JsonPropertyName("images")]
        public List<BingImageInfo> Images { get; set; } = new();
    }

    public class BingProvider : IWallpaperProvider
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string BaseUrl = "https://www.bing.com";
        // n=8 is the maximum allowed by the Bing API for a single request
        private const string ApiUrl = BaseUrl + "/HPImageArchive.aspx?format=js&idx=0&n=8&mkt=en-US";

        public async Task<List<WallpaperItem>> GetWallpapersAsync(bool archiveAll)
        {
            var resultList = new List<WallpaperItem>();

            // Define user-specific folder paths (e.g., ~/BingSpotAny/Wallpapers/Bing/)
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string baseFolder = Path.Combine(baseDir, "Wallpapers", "Bing");
            string currentFolder = Path.Combine(baseFolder, "current");
            string archiveFolder = Path.Combine(baseFolder, "archive");
            string favouritesFolder = Path.Combine(baseFolder, "favourites");

            // Ensure directory structure exists
            Directory.CreateDirectory(currentFolder);
            Directory.CreateDirectory(archiveFolder);
            Directory.CreateDirectory(favouritesFolder);

            try
            {
                // Fetch JSON from Bing API
                string jsonResponse = await _httpClient.GetStringAsync(ApiUrl);
                var data = JsonSerializer.Deserialize<BingApiResponse>(jsonResponse);

                if (data?.Images == null || data.Images.Count == 0) return resultList;

                // Track the latest filenames to clean up older files later
                var downloadedFileNames = new HashSet<string>();

                foreach (var img in data.Images)
                {
                    // Extract filename from the URL (e.g., "/th?id=OHR.Mountain_EN-US.jpg" -> "OHR.Mountain_EN-US.jpg")
                    string fileName = img.Url.Split("id=")[1].Split('&')[0];
                    string localCurrentPath = Path.Combine(currentFolder, fileName);
                    string localArchivePath = Path.Combine(archiveFolder, fileName);
                    string downloadUrl = BaseUrl + img.Url;

                    downloadedFileNames.Add(fileName);

                    // Download the file only if it doesn't already exist in the 'current' directory
                    if (!File.Exists(localCurrentPath))
                    {
                        byte[] imageBytes = await _httpClient.GetByteArrayAsync(downloadUrl);
                        var currentSettings = SettingsManager.LoadSettings();
                                if (currentSettings.EnableWatermark)
                                {
                                    imageBytes = ImageWatermarkHelper.ApplyWatermark(imageBytes, img.Copyright, currentSettings);
                                }
                        await File.WriteAllBytesAsync(localCurrentPath, imageBytes);
                    }

                    // Archive logic: Copy to 'archive' if setting is enabled and it's not already there
                    if (archiveAll && !File.Exists(localArchivePath))
                    {
                        File.Copy(localCurrentPath, localArchivePath);
                    }

                    // Add to our result list to bind to the Avalonia UI
                    resultList.Add(new WallpaperItem
                    {
                        LocalPath = localCurrentPath,
                        Description = img.Copyright,
                        ProviderName = "Bing"
                    });
                }

                // Cleanup: Delete older wallpapers from 'current' that are no longer provided by the API
                var currentFiles = Directory.GetFiles(currentFolder);
                foreach (var file in currentFiles)
                {
                    string existingFileName = Path.GetFileName(file);
                    if (!downloadedFileNames.Contains(existingFileName))
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle or log the exception (e.g., no internet connection)
                System.Diagnostics.Debug.WriteLine($"BingProvider Error: {ex.Message}");
            }

            return resultList;
        }
    }
}