using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopClock.Modules;
using DesktopClock.Properties;
using H.NotifyIcon;
using H.NotifyIcon.EfficiencyMode;
using WpfWindowPlacement;

namespace DesktopClock;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
[ObservableObject]
public partial class MainWindow : Window
{
    private readonly List<IWindowModule> _modules = [
        new ClockModule(),
        new TextColorModule(),
        new BackgroundColorModule(),
        new UnreadMailModule()
        ];
    private TaskbarIcon _trayIcon;
    private DispatcherTimer _topmostEnforcementTimer;

    /// <summary>
    /// The current date and time as a formatted string.
    /// </summary>
    [ObservableProperty]
    private string _currentTimeOrCountdownString;

    /// <summary>
    /// Text for notifications displayed next to the clock (e.g., icons for unread mail).
    /// </summary>
    [ObservableProperty]
    private string _notificationText = string.Empty;

    /// <summary>
    /// Collection of colors to display as a left-to-right gradient background.
    /// If empty or null, background is transparent.
    /// </summary>
    [ObservableProperty]
    private IReadOnlyList<Color> _backgroundGradientColors = [];

    /// <summary>
    /// Indicates whether the background is pulsating.
    /// </summary>
    [ObservableProperty]
    private bool _isPulsating;

    /// <summary>
    /// Gets the effective window opacity. Returns 1.0 when siren is active, otherwise returns the setting value.
    /// </summary>
    public double EffectiveOpacity => IsPulsating ? 1.0 : Settings.Default.WindowOpacity;

    /// <summary>
    /// Gets the effective click-through state. Returns false when pulsing (to allow clicking), otherwise returns the setting value.
    /// </summary>
    public bool EffectiveClickThrough => !IsPulsating && Settings.Default.ClickThrough;

    partial void OnIsPulsatingChanged( bool value )
    {
        OnPropertyChanged( nameof( EffectiveOpacity ) );
        ApplyClickThrough();
    }

    /// <summary>
    /// Starts the pulsating background effect and disables click-through.
    /// </summary>
    public void StartPulsing()
    {
        if ( IsPulsating )
        {
            return; // Already pulsing
        }

        IsPulsating = true;
    }

    /// <summary>
    /// Stops the pulsating background effect and restores click-through state.
    /// </summary>
    public void StopPulsing()
    {
        if ( !IsPulsating )
        {
            return; // Not pulsing
        }

        IsPulsating = false;
    }

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        Settings.Default.PropertyChanged += ( s, e ) => Dispatcher.Invoke( () => Settings_PropertyChanged( s, e ) );

        // Always hide from taskbar, use tray only
        ShowInTaskbar = false;

        // Initialize modules
        InitializeModules();

        // The context menu is shared between right-clicking the window and the tray icon.
        ContextMenu = Resources[ "MainContextMenu" ] as ContextMenu;

        ConfigureTrayIcon( true );

        // Initialize timer to periodically enforce Topmost behavior
        InitializeTopmostEnforcement();
    }

    private void InitializeModules() =>
        _modules.ForEach( module => module.Initialize( this ) );

    /// <summary>
    /// Initializes a timer to periodically enforce the Topmost window behavior.
    /// This prevents the window from losing its always-on-top status over time.
    /// </summary>
    private void InitializeTopmostEnforcement()
    {
        _topmostEnforcementTimer = new DispatcherTimer {
            Interval = TimeSpan.FromSeconds( 5 ) // Check every 5 seconds
        };
        _topmostEnforcementTimer.Tick += ( s, e ) => EnforceTopmost();
        _topmostEnforcementTimer.Start();
    }

    /// <summary>
    /// Ensures the window remains topmost when the setting is enabled.
    /// </summary>
    private void EnforceTopmost()
    {
        if ( Settings.Default.Topmost && !Topmost )
        {
            Topmost = false; // Reset first to trigger the change
            Topmost = true;
        }
    }

    /// <summary>
    /// Closes the app.
    /// </summary>
    [RelayCommand]
    public void Exit() => Application.Current.Shutdown();

    private void ConfigureTrayIcon( bool showIcon )
    {
        if ( showIcon )
        {
            if ( _trayIcon == null )
            {
                // Construct the tray from the resources defined.
                _trayIcon = Resources[ "TrayIcon" ] as TaskbarIcon;
                _trayIcon.ContextMenu = Resources[ "MainContextMenu" ] as ContextMenu;
                _trayIcon.ContextMenu.DataContext = this;
                _trayIcon.ForceCreate( enablesEfficiencyMode: false );
                _trayIcon.TrayLeftMouseDoubleClick += ( _, _ ) => {
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
    private void Settings_PropertyChanged( object sender, PropertyChangedEventArgs e )
    {
        switch ( e.PropertyName )
        {

            case nameof( Settings.Default.ClickThrough ):
                ApplyClickThrough();
                break;

            case nameof( Settings.Default.TextColor ):
                // Clear override when base text color changes (so new lightness is immediately visible)
                Settings.Default.OverrideTextColor = null;
                break;
        }
    }

    private void Window_MouseDown( object sender, MouseButtonEventArgs e )
    {
        // Stop pulsing on click
        if ( IsPulsating && e.ChangedButton == MouseButton.Left )
        {
            StopPulsing();
            e.Handled = true;
            return;
        }

        // Drag the window to move it when click-through is disabled.
        if ( e.ChangedButton == MouseButton.Left && !Settings.Default.ClickThrough )
        {
            DragMove();
        }
    }

    private void Window_SourceInitialized( object sender, EventArgs e )
    {
        this.SetPlacement( Settings.Default.Placement );

        // Make window click-through if enabled.
        ApplyClickThrough();
    }

    private void Window_ContentRendered( object sender, EventArgs e )
    {
        // Make sure the user is aware that their changes will not be saved.
        if ( !Settings.CanBeSaved )
        {
            MessageBox.Show( this,
                "Settings can't be saved because of an access error.\n\n" +
                $"Make sure {Title} is in a folder that doesn't require admin privileges, " +
                "and that you got it from the original source: https://github.com/danielchalmers/DesktopClock.\n\n" +
                "If the problem still persists, create a new issue at the link with as many details as possible.",
                Title, MessageBoxButton.OK, MessageBoxImage.Warning );
        }
    }

    private void Window_Closing( object sender, CancelEventArgs e )
    {
        // Save the last text and the placement to preserve dimensions and position of the clock.
        Settings.Default.LastDisplay = CurrentTimeOrCountdownString;
        Settings.Default.Placement = this.GetPlacement();

        // Stop and dispose the topmost enforcement timer
        _topmostEnforcementTimer?.Stop();

        // Dispose all modules
        _modules.ForEach( module => module.Dispose() );

        // Stop the file watcher before saving.
        Settings.Default.Dispose();

        if ( Settings.CanBeSaved )
        {
            Settings.Default.Save();
        }

        App.SetRunOnStartup( Settings.Default.RunOnStartup );
    }

    private void Window_StateChanged( object sender, EventArgs e )
    {
        if ( WindowState == WindowState.Minimized )
        {
            // Save resources while minimized.
            if ( OperatingSystem.IsWindowsVersionAtLeast( 10, 0, 16299 ) )
            {
                EfficiencyModeUtilities.SetEfficiencyMode( true );
            }
        }
        else
        {
            // Run like normal without withholding resources.
            if ( OperatingSystem.IsWindowsVersionAtLeast( 10, 0, 16299 ) )
            {
                EfficiencyModeUtilities.SetEfficiencyMode( false );
            }
        }
    }

    private void ApplyClickThrough()
    {
        try
        {
            var hwnd = new WindowInteropHelper( this ).Handle;
            const int GWL_EXSTYLE = -20;
            const int WS_EX_TRANSPARENT = 0x00000020;
            const int WS_EX_LAYERED = 0x00080000;
            const int WS_EX_TOOLWINDOW = 0x00000080;

            int exStyle = GetWindowLong( hwnd, GWL_EXSTYLE );
            int newStyle = EffectiveClickThrough
                ? exStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_TOOLWINDOW
                : ( exStyle & ~( WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW ) ) | WS_EX_LAYERED; // keep layered for opacity/visuals
            SetWindowLong( hwnd, GWL_EXSTYLE, newStyle );
        }
        catch
        {
            // Ignore failures.
        }
    }

    [DllImport( "user32.dll", SetLastError = true )]
    private static extern int GetWindowLong( IntPtr hWnd, int nIndex );

    [DllImport( "user32.dll", SetLastError = true )]
    private static extern int SetWindowLong( IntPtr hWnd, int nIndex, int dwNewLong );
}
