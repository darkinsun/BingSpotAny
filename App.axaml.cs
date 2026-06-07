using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BingSpotAny.ViewModels;
using BingSpotAny.Models;     
using BingSpotAny.Providers;  

namespace BingSpotAny
{
    public partial class App : Application
    {
        private System.Timers.Timer? _autoChangeTimer;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;
            }

            // Catch-up check on initial launch
            TriggerWallpaperCheck();

            StartBackgroundTimer();
            base.OnFrameworkInitializationCompleted();
        }

        public static void TriggerWallpaperCheck()
        {
            if (Application.Current is App currentApp)
            {
                Task.Run(async () => await currentApp.CheckAutoChangeAsync());
            }
        }

        // --- CENTRAL WINDOW MANAGER ---
        public static void ShowMainWindow()
        {
            if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                try
                {
                    if (desktop.MainWindow == null)
                    {
                        CreateAndShowWindow(desktop);
                    }
                    else
                    {
                        desktop.MainWindow.Show();
                        desktop.MainWindow.WindowState = Avalonia.Controls.WindowState.Normal;
                        desktop.MainWindow.Activate(); 
                    }
                }
                catch (System.InvalidOperationException)
                {
                    // If the window reference remains but it is physically closed (Zombie Window)
                    CreateAndShowWindow(desktop);
                }
            }
        }

        private static void CreateAndShowWindow(IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel()
            };

            // ZOMBIE WINDOW DEFINITIVE SOLUTION: Reset the reference when the window closes!
            desktop.MainWindow.Closed += (s, e) => 
            { 
                desktop.MainWindow = null; 
            };

            desktop.MainWindow.Show();
        }

        // --- TRAY ICON MENU EVENTS ---
        
        // When "Mainwindow" is selected from the tray menu
        private void ShowMainWindow_Click(object? sender, EventArgs e)
        {
            ShowMainWindow();
        }

        // When you double-click the tray icon (logo)
        private void TrayIcon_Clicked(object? sender, EventArgs e)
        {
            ShowMainWindow();
        }

        private void ShowSettings_Click(object? sender, EventArgs e)
        {
            var settingsWin = new SettingsWindow();
            settingsWin.Show();
            settingsWin.Activate(); 
        }

        private void ShowAbout_Click(object? sender, EventArgs e)
        {
            var aboutWin = new AboutWindow();
            aboutWin.Show();
            aboutWin.Activate();
        }

        private void Exit_Click(object? sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        // --- BACKGROUND TIMER ENGINE ---
        private void StartBackgroundTimer()
        {
            _autoChangeTimer = new System.Timers.Timer(30000); // Pulse every 30 seconds
            _autoChangeTimer.Elapsed += async (sender, e) => await CheckAutoChangeAsync();
            _autoChangeTimer.Start();
        }

        private async Task CheckAutoChangeAsync()
        {
            try 
            {
                var settings = SettingsManager.LoadSettings();
                if (!settings.AutoChangeEnabled) return;

                long currentUnixTime = DateTimeOffset.Now.ToUnixTimeSeconds();
                bool shouldExecute = false;

                if (TimeSpan.TryParse(settings.AutoChangeTime, out TimeSpan targetTimeOfDay))
                {
                    DateTime targetDateTime = DateTime.Today.Add(targetTimeOfDay);
                    long targetUnixTime = new DateTimeOffset(targetDateTime).ToUnixTimeSeconds();

                    bool isTimePassed = currentUnixTime >= targetUnixTime;
                    bool notExecutedYet = settings.LastAutoChangeTime < targetUnixTime;
                    bool isFirstRun = settings.LastAutoChangeTime == 0;

                    if ((isTimePassed && notExecutedYet) || isFirstRun)
                    {
                        shouldExecute = true;
                    }
                }

                if (shouldExecute)
                {
                    System.Diagnostics.Debug.WriteLine($"[AUTO-CHANGE] Execution started for Provider: {settings.DefaultProvider}");
                    
                    IWallpaperProvider provider = settings.DefaultProvider == "SpotLight" 
                        ? new SpotlightProvider() 
                        : new BingProvider();

                    var wallpapers = await provider.GetWallpapersAsync(settings.ArchiveAllWallpapers);
                    
                    if (wallpapers != null && wallpapers.Count > 0)
                    {
                        string targetImagePath = wallpapers[0].LocalPath;
                        var wallpaperManager = new WallpaperManager(settings);
                        bool isSuccess = await wallpaperManager.ApplyWallpaperAsync(targetImagePath);

                        if (isSuccess)
                        {
                            System.Diagnostics.Debug.WriteLine("[AUTO-CHANGE] SUCCESS: Wallpaper applied.");
                            
                            settings.CurrentWallpaperPath = targetImagePath;
                            settings.LastAutoChangeTime = currentUnixTime; 
                            SettingsManager.SaveSettings(settings);
                        }
                        else 
                        {
                            System.Diagnostics.Debug.WriteLine("[AUTO-CHANGE] ERROR: Script execution failed.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AUTO-CHANGE] FATAL CRASH: {ex.Message}");
            }
        }
    }
}