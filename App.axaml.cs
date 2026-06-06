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

        // Renamed and fortified: Pushes the task to a completely independent thread pool
        public static void TriggerWallpaperCheck()
        {
            if (Application.Current is App currentApp)
            {
                // Task.Run ensures the background process survives even if the Settings window closes instantly
                Task.Run(async () => await currentApp.CheckAutoChangeAsync());
            }
        }

        // --- TRAY ICON MENU EVENTS ---
        private void ShowMainWindow_Click(object? sender, EventArgs e)
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // If the window has never been opened before, or if it was closed and set to null:
                if (desktop.MainWindow == null)
                {
                    // Create the window from scratch in RAM
                    desktop.MainWindow = new MainWindow { DataContext = new MainWindowViewModel() };
                    
                    // Clear the reference when the window is closed (when X is pressed) so it can be deleted from RAM.
                    desktop.MainWindow.Closed += (s, args) => 
                    {
                        desktop.MainWindow = null;
                    };
                    
                    desktop.MainWindow.Show();
                }
                else
                {
                    // If the window is already open (just behind another application), bring it to the foreground.
                    desktop.MainWindow.Activate(); 
                }
            }
        }
        private void ShowSettings_Click(object? sender, EventArgs e)
        {
            var settingsWin = new SettingsWindow();
            settingsWin.Show();
            settingsWin.Activate(); 
        }

        // Open Mainwindow on doubleclcik or click.
        private void TrayIcon_Clicked(object? sender, EventArgs e)
        {
            ShowMainWindow_Click(sender, e);
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
                    // Construct today's exact target DateTime
                    DateTime targetDateTime = DateTime.Today.Add(targetTimeOfDay);
                    long targetUnixTime = new DateTimeOffset(targetDateTime).ToUnixTimeSeconds();

                    // Logic 1: Has the clock passed the target time today?
                    bool isTimePassed = currentUnixTime >= targetUnixTime;
                    
                    // Logic 2: Is the last execution time older than today's target time?
                    bool notExecutedYet = settings.LastAutoChangeTime < targetUnixTime;

                    // Logic 3: Is this the very first time the app is running?
                    bool isFirstRun = settings.LastAutoChangeTime == 0;

                    System.Diagnostics.Debug.WriteLine($"[TIME CHECK] Current: {currentUnixTime}, Target: {targetUnixTime}, LastRun: {settings.LastAutoChangeTime}");

                    if (isTimePassed && notExecutedYet || isFirstRun)
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
                            
                            // Mark exact execution moment to prevent duplicate runs
                            settings.CurrentWallpaperPath = targetImagePath;
                            settings.LastAutoChangeTime = currentUnixTime; 
                            SettingsManager.SaveSettings(settings);
                        }
                        else 
                        {
                            System.Diagnostics.Debug.WriteLine("[AUTO-CHANGE] ERROR: Script execution failed.");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[AUTO-CHANGE] ERROR: Provider returned 0 wallpapers.");
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