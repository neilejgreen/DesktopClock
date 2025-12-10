using System.Windows.Media;
using DesktopClock.Data;
using DesktopClock.Properties;

namespace DesktopClock.Modules;

/// <summary>
/// Module that changes the window background based on various conditions.
/// Each active condition adds its color to create a gradient background.
/// </summary>
public class BackgroundColorModule : IWindowModule
{
    private readonly OutlookCalendarService _calendarService;
    private MainWindow _window;
    private bool _disposed;

    // Check for meetings every 30 seconds
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds( 30 );

    public BackgroundColorModule()
    {
        _calendarService = new OutlookCalendarService();

        // Listen to settings changes
        Settings.Default.PropertyChanged += OnSettingsChanged;
    }

    public void Initialize( MainWindow window )
    {
        _window = window ?? throw new ArgumentNullException( nameof( window ) );

        // Check immediately on initialization
        _ = StartCheckingForMeetings();
    }

    private async Task StartCheckingForMeetings()
    {
        while ( !_disposed )
        {
            CheckForUpcomingMeeting();
            await Task.Delay( _checkInterval );
        }

        CheckForUpcomingMeeting();
    }

    private void OnSettingsChanged( object sender, System.ComponentModel.PropertyChangedEventArgs e )
    {
        if ( _disposed || _window == null )
        {
            return;
        }

        if ( e.PropertyName is
            nameof( Settings.UpcomingMeetingBackgroundColor ) or
            nameof( Settings.MeetingInProgressBackgroundColor ) or
            nameof( Settings.Default.MeetingLookAheadMinutes ) )
        {
            CheckForUpcomingMeeting();
        }
    }

    private void CheckForUpcomingMeeting()
    {
        try
        {
            // Build list of active condition colors
            List<Color> activeColors = [];

            if ( !_calendarService.IsAvailable )
            {
                // Outlook not available - add debug color
                activeColors.Add( Colors.Purple );
            }
            else
            {
                // Check for currently active Teams meeting
                var hasMeetingInProgress = _calendarService.GetCurrentMeeting() is not null;

                // Check for upcoming meetings
                var lookAhead = TimeSpan.FromMinutes( Settings.Default.MeetingLookAheadMinutes );
                bool hasMeetingUpcoming = _calendarService.GetUpcomingMeeting( lookAhead ) is not null;

                if ( hasMeetingInProgress )
                {
                    activeColors.Add( Settings.Default.MeetingInProgressBackgroundColor );
                }

                // Add colors for each active condition
                if ( hasMeetingUpcoming )
                {
                    activeColors.Add( Settings.Default.UpcomingMeetingBackgroundColor );
                }
            }

            // Update the gradient colors collection
            UpdateBackgroundColors( activeColors );
        }
        catch
        {
            // If there's an error accessing Outlook, clear background
            UpdateBackgroundColors( [] );
        }
    }

    private void UpdateBackgroundColors( List<Color> colors )
    {
        if ( _disposed || _window == null )
        {
            return;
        }

        _window.Dispatcher.Invoke( () => {
            _window.BackgroundGradientColors.Clear();
            foreach ( var color in colors )
            {
                _window.BackgroundGradientColors.Add( color );
            }
        } );
    }

    public void Dispose()
    {
        if ( _disposed )
        {
            return;
        }

        Settings.Default.PropertyChanged -= OnSettingsChanged;
        _calendarService?.Dispose();

        _disposed = true;
    }
}
