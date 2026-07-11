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
using Avalonia.Platform.Storage;
using Avalonia.Media;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BingSpotAny
{
    public partial class SettingsWindow : Window
    {
        private WallpaperSettings _settings = new WallpaperSettings();

        public SettingsWindow()
        {
            InitializeComponent();
            LoadCurrentSettings();
        }

        private async void LoadCurrentSettings()
        {
            _settings = SettingsManager.LoadSettings();

            // Check OS Startup Registration
            bool isRegistered = StartupManager.IsRegistered();
            var toggleStartup = this.FindControl<ToggleSwitch>("ToggleStartup");
            if (toggleStartup != null) toggleStartup.IsChecked = isRegistered;

            // Prompt user if not in startup and setting hasn't been explicitly turned off
            if (!isRegistered && !_settings.RunAtStartup)
            {
                await Task.Delay(500); 
                
                // Call the new interactive Yes/No Dialog
                var dialog = new YesNoDialog("System Integration", "BingSpotAny is not set to run at system startup.\n\nWould you like to enable this to ensure automatic daily wallpaper changes?");
                
                // Wait for the user's response. Clicking 'X' naturally returns false (or null).
                var result = await dialog.ShowDialog<bool>(this);

                if (result)
                {
                    // If the user clicked "Yes", flip the UI switch to ON.
                    // The actual OS registration will happen when they click "Save".
                    if (toggleStartup != null) toggleStartup.IsChecked = true;
                }
            }
            
            // Automation Settings
            var toggleAuto = this.FindControl<ToggleSwitch>("ToggleAutoChange");
            if (toggleAuto != null) toggleAuto.IsChecked = _settings.AutoChangeEnabled;

            var txtTime = this.FindControl<TextBox>("TxtAutoTime");
            if (txtTime != null) txtTime.Text = _settings.AutoChangeTime;

            var comboProvider = this.FindControl<ComboBox>("ComboProvider");
            if (comboProvider != null) comboProvider.SelectedIndex = _settings.DefaultProvider == "SpotLight" ? 1 : 0;

            // Visual Settings
            var toggleArchive = this.FindControl<ToggleSwitch>("ToggleArchive");
            if (toggleArchive != null) toggleArchive.IsChecked = _settings.ArchiveAllWallpapers;

            var toggleWatermark = this.FindControl<ToggleSwitch>("ToggleWatermark");
            if (toggleWatermark != null) toggleWatermark.IsChecked = _settings.EnableWatermark;

            var comboPos = this.FindControl<ComboBox>("ComboPosition");
            if (comboPos != null && comboPos.Items != null)
            {
                foreach (var itemObj in comboPos.Items)
                {
                    if (itemObj is ComboBoxItem item && item.Content != null && item.Content.ToString() == _settings.WatermarkPosition)
                    {
                        comboPos.SelectedItem = item;
                        break;
                    }
                }
            }

            // Typography & Color Settings
            var numFontSize = this.FindControl<NumericUpDown>("NumFontSize");
            if (numFontSize != null) numFontSize.Value = _settings.WatermarkFontSize;

            var pickerColor = this.FindControl<ColorPicker>("PickerColor");
            if (pickerColor != null)
            {
                if (Color.TryParse(_settings.WatermarkColor, out Color parsedColor))
                {
                    pickerColor.Color = parsedColor;
                }
            }

            var comboFont = this.FindControl<ComboBox>("ComboFontFamily");
            if (comboFont != null)
            {
                // Retrieve all system fonts dynamically, remove duplicates, and sort alphabetically
                var systemFonts = FontManager.Current.SystemFonts
                    .Select(f => f.Name)
                    .Distinct()
                    .OrderBy(n => n)
                    .ToList();
                
                comboFont.ItemsSource = systemFonts;
      
                string? targetFont = systemFonts.FirstOrDefault(f => f == _settings.WatermarkFontFamily);

                  if (string.IsNullOrEmpty(targetFont))
                {
                    // Priority font list
                    var safeFallbacks = new[] { "Arial", "Segoe UI", "Helvetica", "SF Pro", "Noto Sans", "DejaVu Sans", "Liberation Sans" };
                    
                    // Find which of these safe fonts is available in the system.
                    targetFont = safeFallbacks.FirstOrDefault(safe => systemFonts.Contains(safe));
                }

                // If none of the safe fonts are available (a very rare case), choose the first one in the alphabetical list.
                comboFont.SelectedItem = targetFont ?? systemFonts.FirstOrDefault();
            }

            // Advanced Scripts
            var txtLinux = this.FindControl<TextBox>("TxtLinuxScript");
            if (txtLinux != null) txtLinux.Text = _settings.LinuxScriptPath;

            var txtWindows = this.FindControl<TextBox>("TxtWindowsScript");
            if (txtWindows != null) txtWindows.Text = _settings.WindowsScriptPath;

            var txtMac = this.FindControl<TextBox>("TxtMacScript");
            if (txtMac != null) txtMac.Text = _settings.MacOsScriptPath;

            var txtCustom = this.FindControl<TextBox>("TxtCustomScript");
            if (txtCustom != null) txtCustom.Text = _settings.CustomOsScriptPath;

            var togglePostScript = this.FindControl<ToggleSwitch>("TogglePostScript");
            if (togglePostScript != null) togglePostScript.IsChecked = _settings.EnablePostScript;

            var txtPostScript = this.FindControl<TextBox>("TxtPostScript");
            if (txtPostScript != null) txtPostScript.Text = _settings.PostScriptPath;
        }

        private async void BrowseScript_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                string targetTextBoxName = btn.Tag.ToString() ?? string.Empty;
                var targetTextBox = this.FindControl<TextBox>(targetTextBoxName);

                if (targetTextBox != null)
                {
                    // Define the absolute path to the local 'scripts' directory
                    string scriptsDir = System.IO.Path.Combine(WallpaperSettings.GetBaseDataDirectory(), "scripts");
                    
                    if (!System.IO.Directory.Exists(scriptsDir)) 
                    {
                        System.IO.Directory.CreateDirectory(scriptsDir);
                    }
                    
                    var startLocation = await this.StorageProvider.TryGetFolderFromPathAsync(scriptsDir);

                    var files = await this.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                    {
                        Title = "Select Script File",
                        AllowMultiple = false,
                        SuggestedStartLocation = startLocation
                    });

                    if (files != null && files.Count > 0)
                    {
                        targetTextBox.Text = files[0].Path.LocalPath;
                    }
                }
            }
        }

        private void Save_Click(object? sender, RoutedEventArgs e)
        {
            // Automation Settings
            _settings.AutoChangeEnabled = this.FindControl<ToggleSwitch>("ToggleAutoChange")?.IsChecked ?? false;
            _settings.AutoChangeTime = this.FindControl<TextBox>("TxtAutoTime")?.Text ?? "09:00";
            _settings.RunAtStartup = this.FindControl<ToggleSwitch>("ToggleStartup")?.IsChecked ?? false;

            if (this.FindControl<ComboBox>("ComboProvider")?.SelectedItem is ComboBoxItem providerItem)
            {
                _settings.DefaultProvider = providerItem.Content?.ToString() ?? "Bing";
            }

            // Visual Settings
            _settings.ArchiveAllWallpapers = this.FindControl<ToggleSwitch>("ToggleArchive")?.IsChecked ?? true;
            _settings.EnableWatermark = this.FindControl<ToggleSwitch>("ToggleWatermark")?.IsChecked ?? true;
            if (this.FindControl<ComboBox>("ComboPosition")?.SelectedItem is ComboBoxItem posItem)
            {
                _settings.WatermarkPosition = posItem.Content?.ToString() ?? "TopRight";
            }

            // Typography & Color Settings
            _settings.WatermarkFontFamily = this.FindControl<ComboBox>("ComboFontFamily")?.SelectedItem?.ToString() ?? "sans-serif";
            _settings.WatermarkFontSize = (int)(this.FindControl<NumericUpDown>("NumFontSize")?.Value ?? 18);
            
            var pickerColor = this.FindControl<ColorPicker>("PickerColor");
            if (pickerColor != null)
            {
                _settings.WatermarkColor = pickerColor.Color.ToString(); // Outputs #AARRGGBB format
            }

            // Advanced Scripts
            _settings.LinuxScriptPath = this.FindControl<TextBox>("TxtLinuxScript")?.Text ?? "";
            _settings.WindowsScriptPath = this.FindControl<TextBox>("TxtWindowsScript")?.Text ?? "";
            _settings.MacOsScriptPath = this.FindControl<TextBox>("TxtMacScript")?.Text ?? "";
            _settings.CustomOsScriptPath = this.FindControl<TextBox>("TxtCustomScript")?.Text ?? "";
            _settings.EnablePostScript = this.FindControl<ToggleSwitch>("TogglePostScript")?.IsChecked ?? false;
            _settings.PostScriptPath = this.FindControl<TextBox>("TxtPostScript")?.Text ?? "";

         // Handle OS Startup Integration
            if (_settings.RunAtStartup)
                StartupManager.Register();
            else
                StartupManager.Unregister();

            // Persist configuration
            SettingsManager.SaveSettings(_settings);

            // IMPORTANT: If auto-change is enabled, force an immediate update check
            if (_settings.AutoChangeEnabled)
            {
                App.TriggerWallpaperCheck(); 
            }

            this.Close();
        }

        private void Cancel_Click(object? sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}