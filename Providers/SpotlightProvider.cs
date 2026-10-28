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
        private const string HomeUrl = "https://windows10spotlight.com/";

        static SpotlightProvider()
        {
            _httpClient = new HttpClient();
            // We use a full Linux/Firefox identity (User-Agent) to bypass firewalls
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (X11; Linux x86_64; rv:159.0) Gecko/20100101 Firefox/159.0");
            _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        }

        public async Task<List<WallpaperItem>> GetWallpapersAsync(bool archiveAll)
        {
            var resultList = new List<WallpaperItem>();

            string baseDir = WallpaperSettings.GetBaseDataDirectory();
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
                Debug.WriteLine("Scraping the homepage of windows10spotlight.com directly...");

                // Download RSS XML data as raw text
                string htmlContent = await _httpClient.GetStringAsync(HomeUrl);

                // Regex to extract <item> blocks in XML
                var articleRegex = new Regex(@"<article\b[^>]*>(.*?)</article>", RegexOptions.Singleline);
                var articles = articleRegex.Matches(htmlContent);

                int count = 0;
                var processedUrls = new HashSet<string>();

                foreach (Match articleMatch in articles)
                {
                    if (count >= 8) break; // Take only the last 8 images

                    string articleHtml = articleMatch.Groups[1].Value;

                    string description = "Windows Spotlight Image";
                    var titleMatch = Regex.Match(articleHtml, @"<span class=""entry-title hidden"">(.*?)</span>");
                    if (titleMatch.Success)
                    {
                        description = titleMatch.Groups[1].Value.Trim();
                        description = System.Net.WebUtility.HtmlDecode(description);
                    }

                    var urlMatch = Regex.Match(articleHtml, @"(https://windows10spotlight\.com/wp-content/uploads/\d{4}/\d{2}/[^""'\s]+?\.jpg)");

                    if (urlMatch.Success)
                    {
                        string imageUrl = urlMatch.Groups[1].Value;
                        // WordPress sometimes gives thumbnails of images (eg: image-1024x576.jpg).
                        // To download the original quality, we use Regex to remove those size extensions.
                        imageUrl = Regex.Replace(imageUrl, @"-\d+x\d+(?=\.jpg$)", "");

                        // Prevent duplicate processing of thumbnails/originals in the same article block
                        if (processedUrls.Contains(imageUrl)) continue;
                        processedUrls.Add(imageUrl);

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