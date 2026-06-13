using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Media.Imaging; 
using BingSpotAny.Models;
using BingSpotAny.Providers;
using System.Windows.Input;

namespace BingSpotAny.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        // The standard structure required to notify us that we have updated the interface.
        public event PropertyChangedEventHandler? PropertyChanged;
        // The command that the UI button will bind to
        public ICommand ApplyWallpaperCommand { get; }
        public ICommand AddToFavouritesCommand { get; }
        public ICommand SaveImageAsCommand { get; }
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private readonly IWallpaperProvider _bingProvider;
        private readonly IWallpaperProvider _spotlightProvider;
        private readonly WallpaperSettings _settings;
        private List<WallpaperItem> _currentWallpapers;
        private int _currentIndex = 0;

        // The bitmap will be linked to the Image Source in XAML.
        private Bitmap? _currentImage;
        public Bitmap? CurrentImage
        {
            get => _currentImage;
            set { _currentImage = value; OnPropertyChanged(); }
        }

        // The description text that will be linked to the TextBlock in the interface.
        private string _currentDescription = string.Empty;
        public string CurrentDescription
        {
            get => _currentDescription;
            set { _currentDescription = value; OnPropertyChanged(); }
        }

        // Provider Index (0 = Bing, 1 = Spotlight) to connect to the ComboBox in XAML
        private int _selectedProviderIndex;
        public int SelectedProviderIndex
        {
            get => _selectedProviderIndex;
            set
            {
                if (_selectedProviderIndex != value)
                {
                    _selectedProviderIndex = value;
                    OnPropertyChanged();

                    // Download new wallpapers as soon as the provider changes (no user intervention required)
                    _ = LoadWallpapersAsync();
                }
            }
        }

        public MainWindowViewModel()
        {
            // Start Settings and Providers
            _settings = SettingsManager.LoadSettings();
            _bingProvider = new BingProvider();
            _spotlightProvider = new SpotlightProvider();

            _currentWallpapers = new List<WallpaperItem>();

            // Start the download/read process in the background as soon as the application opens.
            _ = LoadWallpapersAsync();
            // Wire up the UI command to our asynchronous backend method
            ApplyWallpaperCommand = new RelayCommand(async () => await ApplyWallpaperAsync());
            AddToFavouritesCommand = new RelayCommand(async () => await AddToFavouritesAsync());
            SaveImageAsCommand = new RelayCommand(async () => await SaveImageAsAsync());
        }

        private async Task LoadWallpapersAsync()
        {
            bool archive = _settings.ArchiveAllWallpapers;

            if (SelectedProviderIndex == 0)
            {
                // Bing is selected as wallpaper provider
                _currentWallpapers = await _bingProvider.GetWallpapersAsync(archive);
            }
            else if (SelectedProviderIndex == 1)
            {
                // Spotlight is selected as wallpaper provider
                _currentWallpapers = await _spotlightProvider.GetWallpapersAsync(archive);
            }

            // When new images are downloaded, always revert to the most recent one (index 0).
            _currentIndex = 0;
            UpdatePreviewImage();
        }

        private void UpdatePreviewImage()
        {
            // If the list is full and the index is within the limits
            if (_currentWallpapers != null && _currentWallpapers.Count > 0 && _currentIndex >= 0 && _currentIndex < _currentWallpapers.Count)
            {
                var item = _currentWallpapers[_currentIndex];
                try
                {
                    // We convert the image on the local disk to an Avalonia Bitmap and send it to the interface.
                    CurrentImage = new Bitmap(item.LocalPath);
                    CurrentDescription = item.Description;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"The image couldn't be loaded: {ex.Message}");
                }
            }
            else
            {
                CurrentImage = null;
                CurrentDescription = "No description has been found.";
            }
        }

        private async Task ApplyWallpaperAsync()
        {
            // 1. Is there a valid image displayed on the screen?
            if (_currentWallpapers == null || _currentWallpapers.Count == 0 || _currentIndex < 0 || _currentIndex >= _currentWallpapers.Count)
            {
                Debug.WriteLine("MVVM ERROR: No valid wallpaper could be found to apply (List is empty).");
                return;
            }

            // 2. Get the local file path of the image currently displayed on the screen.
            string targetImagePath = _currentWallpapers[_currentIndex].LocalPath;
            Debug.WriteLine($"MVVM execution started. Target path: {targetImagePath}");

            // 3. Launch the script manager and perform the operation.
            var wallpaperManager = new WallpaperManager(_settings);
            bool isSuccess = await wallpaperManager.ApplyWallpaperAsync(targetImagePath);

            // 4. Update JSON settings if the process is successful.
            if (isSuccess)
            {
                Debug.WriteLine("MVVM SUCCESS: Wallpaper applied smoothly via command binding.");

                // Memorize the last set wallpaper and save it to the JSON file
                _settings.CurrentWallpaperPath = targetImagePath;
                SettingsManager.SaveSettings(_settings);
            }
            else
            {
                Debug.WriteLine("MVVM ERROR: Command execution failed during script runner invocation.");
            }
        }
        
        private async Task AddToFavouritesAsync()
        {
            if (_currentWallpapers == null || _currentWallpapers.Count == 0 || _currentIndex < 0 || _currentIndex >= _currentWallpapers.Count)
                return;

            var currentItem = _currentWallpapers[_currentIndex];
            string sourcePath = currentItem.LocalPath;
            string fileName = Path.GetFileName(sourcePath);

            // Cross-platform portable storage logic
            string baseDir = WallpaperSettings.GetBaseDataDirectory();
            string favFolder = Path.Combine(baseDir, "Wallpapers", currentItem.ProviderName, "favourites");
            
            if (!Directory.Exists(favFolder))
            {
                Directory.CreateDirectory(favFolder);
            }

            string destPath = Path.Combine(favFolder, fileName);
            string dialogMessage = "";

            // Only copy if the file hasn't been added to favorites before.
            if (!File.Exists(destPath))
            {
                File.Copy(sourcePath, destPath);
                dialogMessage = $"Image added to favorites.\n\nSave Path:\n{destPath}";
            }
            else
            {
                dialogMessage = $"This image is already in the favorites folder.\n\nLocation:\n{destPath}";
            }

            // Create and display an information message window on the screen
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = desktop.MainWindow;
                if (mainWindow != null)
                {
                    var customDialog = new MessageBox("Favorites", dialogMessage);
                    await customDialog.ShowDialog(mainWindow);
                }
            }
        }

        private async Task SaveImageAsAsync()
        {
            if (_currentWallpapers == null || _currentWallpapers.Count == 0 || _currentIndex < 0 || _currentIndex >= _currentWallpapers.Count)
                return;

            string sourcePath = _currentWallpapers[_currentIndex].LocalPath;
            string fileName = Path.GetFileName(sourcePath);

            // In Avalonia, we access the Window object from within MVVM and call the StorageProvider.
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var window = desktop.MainWindow;
                if (window != null)
                {
                    var storageProvider = window.StorageProvider;

                    // We are setting the user's Home directory as the default boot location.
                    string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    var startFolder = await storageProvider.TryGetFolderFromPathAsync(userProfile);

                    // Set up the save dialog box.
                    var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                    {
                        Title = "Save Wallpaper As...",
                        SuggestedFileName = fileName,
                        SuggestedStartLocation = startFolder,
                        DefaultExtension = "jpg",
                        FileTypeChoices = new[]
                        {
                            new FilePickerFileType("JPEG Image") { Patterns = new[] { "*.jpg", "*.jpeg" } }
                        }
                    });

                    // If the user pressed the "Save" button (and did not cancel)
                    if (file != null)
                    {
                        // Copy the image asynchronously to the selected destination.
                        using (var stream = await file.OpenWriteAsync())
                        using (var sourceStream = File.OpenRead(sourcePath))
                        {
                            await sourceStream.CopyToAsync(stream);
                        }
                        Debug.WriteLine($"SUCCESSFUL: Image saved as to -> {file.Path.LocalPath}");
                    }
                }
            }
        }
        
        // Navigation Methods (Avalonia can call these methods directly from the buttons)
        public void GoFirst() { _currentIndex = 0; UpdatePreviewImage(); }
        public void GoPrev() { if (_currentIndex > 0) _currentIndex--; UpdatePreviewImage(); }
        public void GoNext() { if (_currentIndex < _currentWallpapers.Count - 1) _currentIndex++; UpdatePreviewImage(); }
        public void GoLast() { _currentIndex = _currentWallpapers.Count - 1; UpdatePreviewImage(); }
    }
}