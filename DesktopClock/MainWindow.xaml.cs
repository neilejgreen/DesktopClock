using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopClock.Data;
using DesktopClock.Properties;
using DesktopClock.Utilities;
using H.NotifyIcon;
using H.NotifyIcon.EfficiencyMode;
using WpfWindowPlacement;
using static DesktopClock.Utilities.ScreenColorDetector;

namespace DesktopClock;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
[ObservableObject]
public partial class MainWindow : Window
{
    private readonly IDataProvider _dataProvider;
    private TaskbarIcon _trayIcon;
    private int _autoColorUpdateCounter;

    /// <summary>
    /// The current date and time as a formatted string.
    /// </summary>
    [ObservableProperty]
    private string _currentTimeOrCountdownString;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        Settings.Default.PropertyChanged += (s, e) => Dispatcher.Invoke(() => Settings_PropertyChanged(s, e));

        // Always hide from taskbar, use tray only
        ShowInTaskbar = false;

        // Initialize data provider
        _dataProvider = new ClockDataProvider();
        _dataProvider.DataChanged += (s, e) => Dispatcher.Invoke(() => OnDataProviderDataChanged());

        // Restore the structure of the last state using the display text.
        CurrentTimeOrCountdownString = Settings.Default.LastDisplay ?? _dataProvider.GetDisplayText();

        // The context menu is shared between right-clicking the window and the tray icon.
        ContextMenu = Resources["MainContextMenu"] as ContextMenu;

        ConfigureTrayIcon(true);
    }

    private void OnDataProviderDataChanged()
    {
        CurrentTimeOrCountdownString = _dataProvider.GetDisplayText();
        TryUpdateTextColor();
    }

    /// <summary>
    /// Closes the app.
    /// </summary>
    [RelayCommand]
    public void Exit()
    {
        Application.Current.Shutdown();
    }

    private void ConfigureTrayIcon(bool showIcon)
    {
        if (showIcon)
        {
            if (_trayIcon == null)
            {
                // Construct the tray from the resources defined.
                _trayIcon = Resources["TrayIcon"] as TaskbarIcon;
                _trayIcon.ContextMenu = Resources["MainContextMenu"] as ContextMenu;
                _trayIcon.ContextMenu.DataContext = this;
                _trayIcon.ForceCreate(enablesEfficiencyMode: false);
                _trayIcon.TrayLeftMouseDoubleClick += (_, _) =>
                {
                    // Toggle click-through on double-click
                    Settings.Default.ClickThrough = !Settings.Default.ClickThrough;
                };
            }
        }
        else
        {
            _trayIcon?.Dispose();
            _trayIcon = null;
        }
    }

    /// <summary>
    /// Handles setting changes.
    /// </summary>
    private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Settings.Default.Format):
            case nameof(Settings.Default.UseNaturalLanguage):
                // Update display immediately when format changes
                CurrentTimeOrCountdownString = _dataProvider.GetDisplayText();
                break;

            case nameof(Settings.Default.ClickThrough):
                ApplyClickThrough();
                break;

            case nameof(Settings.Default.AutoAdjustTextColor):
                // Clear override when auto-adjustment is disabled
                if (!Settings.Default.AutoAdjustTextColor)
                {
                    Settings.Default.OverrideTextColor = null;
                    _autoColorUpdateCounter = 0; // Reset counter
                }
                break;

            case nameof(Settings.Default.TextColor):
                // Clear override when base text color changes (so new lightness is immediately visible)
                Settings.Default.OverrideTextColor = null;
                break;
        }
    }

    /// <summary>
    /// Tries to update the text lightness based on the background behind the clock if auto adjustment is enabled.
    /// </summary>
    private void TryUpdateTextColor()
    {
        if (!Settings.Default.AutoAdjustTextColor)
            return;

        // Only update at the specified interval to avoid performance issues
        _autoColorUpdateCounter++;
        if (_autoColorUpdateCounter < Settings.Default.AutoColorUpdateInterval)
            return;

        _autoColorUpdateCounter = 0;

        try
        {
            // Run color detection on dispatcher to ensure we have window bounds
            Dispatcher.Invoke(() =>
            {
                // Adjust the current text color to contrast with the background
                var optimalTextColor = ScreenColorDetector.GetOptimalTextColorWithHue();

                // Update the override text color without modifying the saved setting
                Settings.Default.OverrideTextColor = optimalTextColor;
            });
        }
        catch
        {
            // Ignore errors to prevent crashes from screen capture issues
        }
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        // Drag the window to move it when click-through is disabled.
        if (e.ChangedButton == MouseButton.Left && !Settings.Default.ClickThrough)
        {
            DragMove();
        }
    }

    private void Window_SourceInitialized(object sender, EventArgs e)
    {
        this.SetPlacement(Settings.Default.Placement);

        // Update display text
        CurrentTimeOrCountdownString = _dataProvider.GetDisplayText();

        // Show the window now that it's finished loading.
        Opacity = 1;

        // Make window click-through if enabled.
        ApplyClickThrough();
    }

    private void Window_ContentRendered(object sender, EventArgs e)
    {
        // Make sure the user is aware that their changes will not be saved.
        if (!Settings.CanBeSaved)
        {
            MessageBox.Show(this,
                "Settings can't be saved because of an access error.\n\n" +
                $"Make sure {Title} is in a folder that doesn't require admin privileges, " +
                "and that you got it from the original source: https://github.com/danielchalmers/DesktopClock.\n\n" +
                "If the problem still persists, create a new issue at the link with as many details as possible.",
                Title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        // Save the last text and the placement to preserve dimensions and position of the clock.
        Settings.Default.LastDisplay = CurrentTimeOrCountdownString;
        Settings.Default.Placement = this.GetPlacement();

        // Dispose data provider
        (_dataProvider as IDisposable)?.Dispose();

        // Stop the file watcher before saving.
        Settings.Default.Dispose();

        if (Settings.CanBeSaved)
            Settings.Default.Save();

        App.SetRunOnStartup(Settings.Default.RunOnStartup);
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            // Save resources while minimized.
            EfficiencyModeUtilities.SetEfficiencyMode(true);
        }
        else
        {
            // Run like normal without withholding resources.
            CurrentTimeOrCountdownString = _dataProvider.GetDisplayText();
            EfficiencyModeUtilities.SetEfficiencyMode(false);
        }
    }

    private void ApplyClickThrough()
    {
        try
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            const int GWL_EXSTYLE = -20;
            const int WS_EX_TRANSPARENT = 0x00000020;
            const int WS_EX_LAYERED = 0x00080000;
            const int WS_EX_TOOLWINDOW = 0x00000080;

            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            int newStyle = Settings.Default.ClickThrough
                ? exStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_TOOLWINDOW
                : (exStyle & ~(WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW)) | WS_EX_LAYERED; // keep layered for opacity/visuals
            SetWindowLong(hwnd, GWL_EXSTYLE, newStyle);
        }
        catch
        {
            // Ignore failures.
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
}
