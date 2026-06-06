using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BingSpotAny.Models;

namespace BingSpotAny.Providers
{
    public class SpotlightProvider : IWallpaperProvider
    {
        private static readonly HttpClient _httpClient;

        // We will directly read the public RSS feed of the community archive.
        private const string RssUrl = "https://windows10spotlight.com/feed";

        static SpotlightProvider()
        {
            _httpClient = new HttpClient();
            // We use a full Linux/Firefox identity (User-Agent) to bypass firewalls
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (X11; Linux x86_64; rv:151.0.2) Gecko/20100101 Firefox/151.0.2");
            _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        }

        public async Task<List<WallpaperItem>> GetWallpapersAsync(bool archiveAll)
        {
            var resultList = new List<WallpaperItem>();

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string baseFolder = Path.Combine(baseDir, "Wallpapers", "SpotLight");
            string currentFolder = Path.Combine(baseFolder, "current");
            string archiveFolder = Path.Combine(baseFolder, "archive");
            string favouritesFolder = Path.Combine(baseFolder, "favourites");

            Directory.CreateDirectory(currentFolder);
            Directory.CreateDirectory(archiveFolder);
            Directory.CreateDirectory(favouritesFolder);

            var downloadedFileNames = new HashSet<string>();
            var currentSettings = SettingsManager.LoadSettings();

            try
            {
                Debug.WriteLine("Reading the RSS feed of the free archive...");
                
                // Download RSS XML data as raw text
                string rssContent = await _httpClient.GetStringAsync(RssUrl);

                // Regex to extract <item> blocks in XML
                var itemRegex = new Regex(@"<item>(.*?)</item>", RegexOptions.Singleline);
                var items = itemRegex.Matches(rssContent);

                int count = 0;
                foreach (Match itemMatch in items)
                {
                    if (count >= 8) break; // Take only the last 8 images to avoid bloating the interface

                    string itemXml = itemMatch.Groups[1].Value;

                    // Extract Title (Description text)
                    string description = "Windows Spotlight Image";
                    var titleMatch = Regex.Match(itemXml, @"<title>(.*?)</title>");
                    if (titleMatch.Success)
                    {
                        // Clear CDATA blocks and HTML special characters (e.g. &)
                        description = titleMatch.Groups[1].Value.Replace("<![CDATA[", "").Replace("]]>", "").Trim();
                        description = System.Net.WebUtility.HtmlDecode(description);
                    }

                    // Scrape high-resolution JPG URL from XML (wp-content/uploads/YYYY/MM/... format)
                    var urlMatch = Regex.Match(itemXml, @"(https://windows10spotlight\.com/wp-content/uploads/\d{4}/\d{2}/[^""'\s]+?\.jpg)");
                    
                    if (urlMatch.Success)
                    {
                        string imageUrl = urlMatch.Groups[1].Value;
                        
                        // WordPress sometimes gives thumbnails of images (eg: image-1024x576.jpg).
                        // To download the original quality, we use Regex to remove those size extensions.
                        imageUrl = Regex.Replace(imageUrl, @"-\d+x\d+(?=\.jpg$)", "");

                        Uri uri = new Uri(imageUrl);
                        string fileName = Path.GetFileName(uri.LocalPath);

                        string localCurrentPath = Path.Combine(currentFolder, fileName);
                        string localArchivePath = Path.Combine(archiveFolder, fileName);
                        
                        downloadedFileNames.Add(fileName);

                        if (!File.Exists(localCurrentPath))
                        {
                            byte[] imageBytes = await _httpClient.GetByteArrayAsync(imageUrl);
                            
                            // If it's enabled in the settings, click Watermark.
                            if (currentSettings.EnableWatermark)
                            {
                                imageBytes = ImageWatermarkHelper.ApplyWatermark(imageBytes, description, currentSettings);
                            }

                            await File.WriteAllBytesAsync(localCurrentPath, imageBytes);
                        }

                        if (archiveAll && !File.Exists(localArchivePath))
                        {
                            File.Copy(localCurrentPath, localArchivePath);
                        }

                        resultList.Add(new WallpaperItem
                        {
                            LocalPath = localCurrentPath,
                            Description = description,
                            ProviderName = "SpotLight"
                        });
                        
                        count++;
                    }
                }

                // Clean up old files
                var currentFiles = Directory.GetFiles(currentFolder);
                foreach (var file in currentFiles)
                {
                    string existingFileName = Path.GetFileName(file);
                    if (!downloadedFileNames.Contains(existingFileName))
                    {
                        File.Delete(file);
                    }
                }
                
                Debug.WriteLine($"SUCCESSFUL: number of {resultList.Count} Spotlight images were etched.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SpotlightProvider Error (RSS Web Scraper): {ex.Message}");
            }

            return resultList;
        }
    }
}