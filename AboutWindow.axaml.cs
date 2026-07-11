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
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Diagnostics;
using System.ComponentModel;
using System.Threading.Tasks;

namespace BingSpotAny
{
    public partial class AboutWindow : Window, INotifyPropertyChanged
    {
        // Ensure the property notifies the UI on change
        private bool _isUpdateAvailable;
        public bool IsUpdateAvailable
        {
            get => _isUpdateAvailable;
            set
            {
                if (_isUpdateAvailable != value)
                {
                    _isUpdateAvailable = value;
                    // Notify UI that the property has changed
                    OnPropertyChanged(nameof(IsUpdateAvailable));
                }
            }
        }

        // Expose global version to the UI
        public string VersionText => $"Version {App.AppVersion}";
        private string _updateUrl = string.Empty;
        public AboutWindow()
        {
            InitializeComponent();
            DataContext = this; // Bind UI to this code-behind class
            
            // Fire and forget update check when window opens
            _ = CheckUpdateAsync();
        }

        public new event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) 
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        // Ensure the update check runs on the UI thread
        private async Task CheckUpdateAsync()
        {
            var update = await UpdateChecker.CheckForUpdatesAsync();
            if (update != null)
            {
                _updateUrl = update.HtmlUrl;
                // Use Dispatcher to update property safely on the UI thread
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    IsUpdateAvailable = true;
                });
            }
        }

        // Bound to the "Click for new release" button
        public void OpenUpdateLinkCommand()
        {
            if (!string.IsNullOrEmpty(_updateUrl))
            {
                OpenUrl(_updateUrl);
            }
        }
        private void Close_Click(object? sender, RoutedEventArgs e)
        {
            // Close the about window smoothly
            this.Close();
        }

        private void Project_Click(object? sender, RoutedEventArgs e)
        {
            // URL to the main GitHub repository
            string projectUrl = "https://github.com/darkinsun/BingSpotAny";
            OpenUrl(projectUrl);
        }
        private void License_Click(object? sender, RoutedEventArgs e)
        {
            // URL directly pointing to the LICENSE file in your GitHub repository
            string licenseUrl = "https://github.com/darkinsun/BingSpotAny/blob/main/LICENSE";
            OpenUrl(licenseUrl);
        }
        private void Donate_Click(object? sender, RoutedEventArgs e)
        {
            // URL to the specific donation page or sponsor link within GitHub
            // Example: "https://github.com/sponsors/yourusername" or a specific markdown file
            string donateUrl = "https://github.com/darkinsun/BingSpotAny/blob/main/DONATE.md"; 
            OpenUrl(donateUrl);
        }
        
        private void OpenUrl(string url)
        {
            try
            {
                // Opens the specified URL in the operating system's default web browser
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"[ABOUT] Failed to open link: {ex.Message}");
            }
        }
    }
}