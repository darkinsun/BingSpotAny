using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Diagnostics;

namespace BingSpotAny
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
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