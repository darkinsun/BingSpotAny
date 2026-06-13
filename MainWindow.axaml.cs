using System;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace BingSpotAny
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Listen for window closing request (pressing the X key)
            // Closing += MainWindow_Closing;
        }

   /*      private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            // Cancel closing request.
            e.Cancel = true;

            // Hide the window (Tray icon continues to run in the background)
            this.Hide();
        }
 */
        private void OpenArchive_Click(object? sender, RoutedEventArgs e)
        {
            // Get the active provider directly from the UI
            string providerName = GetSelectedProvider();

            string baseDir = WallpaperSettings.GetBaseDataDirectory();
            string archivePath = Path.Combine(baseDir, "Wallpapers", providerName,"archive");

            SystemOSOpenFolder(archivePath);
        }

        private void OpenFavorites_Click(object? sender, RoutedEventArgs e)
        {
            // Get the active provider directly from the UI
            string providerName = GetSelectedProvider();

            string baseDir = WallpaperSettings.GetBaseDataDirectory();
            string favoritesPath = Path.Combine(baseDir, "Wallpapers", providerName, "favourites");

            SystemOSOpenFolder(favoritesPath);
        }

        // Helper method to read the value from ComboBox1 safely
        private string GetSelectedProvider()
        {
            var comboBox = this.FindControl<ComboBox>("ComboBox1");

            if (comboBox?.SelectedItem != null)
            {
                // Handle both ComboBoxItem and direct string bindings
                if (comboBox.SelectedItem is ComboBoxItem item)
                {
                    return item.Content?.ToString() ?? "Bing";
                }

                return comboBox.SelectedItem.ToString() ?? "Bing";
            }

            // Failsafe fallback
            return "Bing";
        }
        private void SystemOSOpenFolder(string folderPath)
        {
            try
            {
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // .NET's Universal Shell Execute Call
                Process.Start(new ProcessStartInfo
                {
                    FileName = folderPath,    // We provide the folder path directly.
                    UseShellExecute = true    // The universal bridge setting that creates magic
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FOLDER OPEN ERROR] Failed to open path {folderPath}: {ex.Message}");
            }
        }
        private void About_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var aboutWin = new AboutWindow();
          
            aboutWin.ShowDialog(this); 
        }
    }
}