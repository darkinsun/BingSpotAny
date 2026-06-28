using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.ApplicationLifetimes;
using BingSpotAny.ViewModels;
using BingSpotAny.Models;     
using BingSpotAny.Providers;

namespace BingSpotAny
{
    public partial class App : Application
    {
        // Global Application Version Variable
        public const string AppVersion = "1.1.1";
        private System.Timers.Timer? _autoChangeTimer;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
        // Static manager for easy global access
        public static INotificationManager? NotificationManager { get; private set; }
        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;
            }
            // Catch-up check on initial launch
            TriggerWallpaperCheck();
            StartBackgroundTimer();
            StartUpdateCheckerTimer();

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
                // CRITICAL FIX: Queue the window operation on the UI thread.
                // This completely prevents race conditions caused by rapid double-clicks on the tray icon.
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        if (desktop.MainWindow == null)
                        {
                            CreateAndShowWindow(desktop);
                        }
                        else
                        {
                            // Only call Show if it is actually hidden to prevent Avalonia rendering bugs
                            if (!desktop.MainWindow.IsVisible)
                            {
                                desktop.MainWindow.Show();
                            }
                            
                            // Restore from minimized state if necessary
                            if (desktop.MainWindow.WindowState == Avalonia.Controls.WindowState.Minimized)
                            {
                                desktop.MainWindow.WindowState = Avalonia.Controls.WindowState.Normal;
                            }
                            
                            desktop.MainWindow.Activate();
                            
                            // Native trick to force the window to the absolute foreground
                            desktop.MainWindow.Topmost = true; 
                            desktop.MainWindow.Topmost = false; 
                        }
                    }
                    catch (System.InvalidOperationException)
                    {
                        // Ensure the dead reference is completely cleared before creating a new one
                        desktop.MainWindow = null; 
                        NotificationManager = null;
                        CreateAndShowWindow(desktop);
                    }
                });
            }
        }

        private static void CreateAndShowWindow(IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel()
            };

            desktop.MainWindow = mainWindow;

            // Initialize the Notification Manager bound directly to the live window instance
            NotificationManager = new WindowNotificationManager(mainWindow)
            {
                Position = NotificationPosition.BottomRight,
                MaxItems = 3
            };

            // TRAY APP BEST PRACTICE: Do not destroy the window on close. Hide it instead.
            desktop.MainWindow.Closing += (s, e) =>
            {
                e.Cancel = true; // Abort the actual destruction of the window

                var win = (Avalonia.Controls.Window)s!;
                // CRITICAL FIX: Pushing the Hide command to the next UI thread cycle 
                // prevents the OS close-loop from clashing with Avalonia's visibility state,
                // eliminating the "ghost window" or "double-click to hide" bug.
                Avalonia.Threading.Dispatcher.UIThread.Post(() => win.Hide());
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

        // --- AUTO UPDATE CHECKER ---
        private void StartUpdateCheckerTimer()
        {
            Task.Run(async () =>
            {
                System.Diagnostics.Debug.WriteLine("[UPDATE-CHECK] Timer started. Waiting for 1 minute...");
                await Task.Delay(TimeSpan.FromMinutes(1));
                System.Diagnostics.Debug.WriteLine("[UPDATE-CHECK] Delay finished. Contacting UpdateChecker...");

                var update = await UpdateChecker.CheckForUpdatesAsync();

                if (update != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[UPDATE-CHECK] SUCCESS: Update found! Version: {update.TagName}");
                    ShowUpdateNotification(update.TagName, update.HtmlUrl);
                }
                else
                {
                    // If you see this in the console, your UpdateChecker is returning null.
                    // This means either network failed, or AppVersion >= GitHub Version.
                    System.Diagnostics.Debug.WriteLine("[UPDATE-CHECK] FAILED or NO UPDATE: UpdateChecker returned null.");
                }
            });
        }

        // Method to show notifications globally
        // --- NATIVE & BACKGROUND NOTIFICATION ROUTING ---
        public static void ShowUpdateNotification(string newVersion, string url)
        {
            // Always dispatch to the UI thread to ensure we are operating within the Visual Tree
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                var desktop = Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
                var activeWindow = desktop?.MainWindow;

                // Only use the overlay if the window is truly visible and active
                if (activeWindow != null && activeWindow.IsVisible && NotificationManager != null)
                {
                    var notification = new Notification(
                        "BingSpotAny Update",
                        $"Version {newVersion} is out! Click here to download.",
                        NotificationType.Information);

                    notification.OnClick = () => {
                        var aboutWin = new AboutWindow();
                        aboutWin.Show();
                        aboutWin.Activate();
                    };

                    NotificationManager.Show(notification);
                }
                else
                {
                    // Fallback to Native OS Notification if window is hidden
                    FireNativeBackgroundNotification(newVersion, url);
                }
            });
        }

        private static void FireNativeBackgroundNotification(string newVersion, string url)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[NOTIFICATIONS] Firing native background notification...");
                if (OperatingSystem.IsWindows())
                {
                    System.Diagnostics.Debug.WriteLine("[NOTIFICATIONS] Target OS: Windows. Preparing Classic Balloon Tip...");
                    
                    string psCommand = @"
                        Add-Type -AssemblyName System.Windows.Forms
                        $notify = New-Object System.Windows.Forms.NotifyIcon
                        $notify.Icon = [System.Drawing.SystemIcons]::Information
                        $notify.BalloonTipTitle = 'BingSpotAny Update'
                        $notify.BalloonTipText = 'Version " + newVersion + @" is out! Open About in tray menu to update.'
                        $notify.BalloonTipIcon = 'Info'
                        $notify.Visible = $true
                        $notify.ShowBalloonTip(5000)
                        Start-Sleep -Seconds 6
                        $notify.Dispose()";
                    
                    // FINAL PRODUCTION SETTINGS:
                    // CreateNoWindow = true and WindowStyle Hidden ensures no black box pops up.
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "powershell",
                        Arguments = $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -Command \"{psCommand}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });
                }
                else if (OperatingSystem.IsLinux())
                {
                    // Standard notification daemon execution for Linux environments 
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "notify-send",
                        Arguments = $"\"BingSpotAny\" \"Version {newVersion} is out! Open About in tray menu to update.\" -u normal",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });
                }
                else if (OperatingSystem.IsMacOS())
                {
                    // Standard AppleScript notification trigger
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "osascript",
                        Arguments = $"-e 'display notification \"Version {newVersion} is out! Open About in tray menu to update.\" with title \"BingSpotAny\"'",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NOTIFICATIONS] Native background trigger failed: {ex.Message}");
                
                // Ultimate Fallback: If native OS scripts are completely blocked by user permissions, force the UI window
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    var aboutWin = new AboutWindow();
                    aboutWin.Show();
                    aboutWin.Activate();
                });
            }
        }
    }
}