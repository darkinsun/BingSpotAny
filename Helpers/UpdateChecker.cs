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
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BingSpotAny
{
    public class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;
    }

    public static class UpdateChecker
    {
        // Update this manually before every new release
        public const string CurrentVersion = App.AppVersion;
        private const string ApiUrl = "https://api.github.com/repos/darkinsun/BingSpotAny/releases/latest";

        public static async Task<GitHubRelease?> CheckForUpdatesAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("BingSpotAny", CurrentVersion));

                var response = await client.GetStringAsync(ApiUrl);
                var release = JsonSerializer.Deserialize<GitHubRelease>(response);

                if (release != null)
                {
                    string latestVersionStr = release.TagName.TrimStart('v', 'V');
                    
                    if (Version.TryParse(CurrentVersion, out var current) && 
                        Version.TryParse(latestVersionStr, out var latest))
                    {
                        if (latest > current)
                        {
                            return release;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UPDATE CHECKER] Failed: {ex.Message}");
            }
            
            return null;
        }
    }
}