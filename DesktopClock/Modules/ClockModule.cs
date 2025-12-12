using DesktopClock.Data;
using DesktopClock.Properties;
using DesktopClock.Utilities;

namespace DesktopClock.Modules;


/// <summary>
/// Module that displays the current time on the window.
/// </summary>
public class ClockModule : IWindowModule
{
    private readonly SystemClockTimer _systemClockTimer;
    private MainWindow _window;
    private bool _disposed;
    private readonly DateTimeFormatter _formatter = new();

    public ClockModule()
    {
        _systemClockTimer = new SystemClockTimer();
        _systemClockTimer.SecondChanged += OnTimerTick;

        // Listen to settings changes
        Settings.Default.PropertyChanged += OnSettingsChanged;
    }

    public void Initialize( MainWindow window )
    {
        _window = window ?? throw new ArgumentNullException( nameof( window ) );

        // Set initial display text
        UpdateDisplayText();

        // Start the timer
        _systemClockTimer.Start();
    }

    private void OnTimerTick( object sender, EventArgs e )
    {
        if ( _disposed || _window == null )
        {
            return;
        }

        UpdateDisplayText();
    }

    private void OnSettingsChanged( object sender, System.ComponentModel.PropertyChangedEventArgs e )
    {
        if ( _disposed || _window == null )
        {
            return;
        }

        // Update display when format settings change
        if ( e.PropertyName is nameof( Settings.Default.Format ) )
        {
            UpdateDisplayText();
        }
    }

    private void UpdateDisplayText()
    {
        if ( _window == null )
        {
            return;
        }

        string displayText = _formatter.Format( DateTime.Now, Settings.Default.Format );
        _window.Dispatcher.Invoke( () => _window.CurrentTimeOrCountdownString = displayText );
    }

    public void Dispose()
    {
        if ( _disposed )
        {
            return;
        }

        Settings.Default.PropertyChanged -= OnSettingsChanged;
        _systemClockTimer?.Dispose();
        _disposed = true;
    }
}

