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