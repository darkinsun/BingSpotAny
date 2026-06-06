using System.Collections.Generic;
using System.Threading.Tasks;

namespace BingSpotAny.Models
{
    // Represents the unified wallpaper object for the UI
    public class WallpaperItem
    {
        public string LocalPath { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
    }

    // Interface enforcing structure for both Bing and Spotlight providers
    public interface IWallpaperProvider
    {
        Task<List<WallpaperItem>> GetWallpapersAsync(bool archiveAll);
    }
}