using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopClock.Modules;
using DesktopClock.Properties;
using H.NotifyIcon;
using H.NotifyIcon.EfficiencyMode;
using Microsoft.Win32;
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

    private EventHandler _displayChangedHandler;

    private TaskbarIcon _trayIcon;

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
    /// Indicates whether the window is movable (not click-through).
    /// </summary>
    [ObservableProperty]
    private bool _isMovable;

    /// <summary>
    /// Gets the effective window opacity. Returns 1.0 when siren is active, otherwise returns the setting value.
    /// </summary>
    public double EffectiveOpacity => IsPulsating ? 1.0 : Settings.Default.WindowOpacity;

    partial void OnIsPulsatingChanged( bool value )
    {
        OnPropertyChanged( nameof( EffectiveOpacity ) );
        UpdateWindowState();
    }

    partial void OnIsMovableChanged( bool value )
    {
        UpdateWindowState();
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
    }

    private void InitializeModules() =>
        _modules.ForEach( module => module.Initialize( this ) );

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
                    // Toggle movable on double-click
                    IsMovable = !IsMovable;
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

        // Drag the window to move it when movable.
        if ( e.ChangedButton == MouseButton.Left && IsMovable )
        {
            DragMove();
        }
    }

    private void Window_SourceInitialized( object sender, EventArgs e )
    {
        this.SetPlacement( Settings.Default.Placement );

        // Make window click-through if enabled.
        UpdateWindowState();

        _displayChangedHandler = ( _, __ ) => UpdateWindowState();

        IsVisibleChanged += OnIsVisibleChangedChanged;
        SystemEvents.DisplaySettingsChanged += _displayChangedHandler;

    }

    private void OnIsVisibleChangedChanged( object _, DependencyPropertyChangedEventArgs e )
    {
        UpdateWindowState();
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

        // Dispose all modules
        _modules.ForEach( module => module.Dispose() );

        // Stop the file watcher before saving.
        Settings.Default.Dispose();

        if ( Settings.CanBeSaved )
        {
            Settings.Default.Save();
        }

        IsVisibleChanged -= OnIsVisibleChangedChanged;
        SystemEvents.DisplaySettingsChanged -= _displayChangedHandler;

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

    public void UpdateWindowState()
    {
        Win32.UpdateWindow( this, isClickable: IsPulsating || IsMovable );
    }

}
