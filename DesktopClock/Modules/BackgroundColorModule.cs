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
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds( 3 );

    // Track the current meeting to detect state changes
    private MeetingInfo _currentMeeting;

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

                // Clear tracked meeting
                _currentMeeting = null;
            }
            else
            {
                // Check for currently active meeting
                var currentMeeting = _calendarService.GetCurrentMeeting();

                // Check for upcoming meetings
                var lookAhead = TimeSpan.FromMinutes( Settings.Default.MeetingLookAheadMinutes );
                var upcomingMeeting = _calendarService.GetUpcomingMeeting( lookAhead );

                // Detect when a meeting newly becomes current
                bool meetingNewlyBecameCurrent = currentMeeting != null &&
                    ( _currentMeeting == null ||
                      _currentMeeting.Subject != currentMeeting.Subject ||
                      _currentMeeting.StartTime != currentMeeting.StartTime );

                // Detect when a current meeting ends
                bool currentMeetingEnded = _currentMeeting != null && currentMeeting == null;

                // Update tracked meeting
                _currentMeeting = currentMeeting;

                // Handle pulsing state changes
                if ( meetingNewlyBecameCurrent )
                {
                    _window?.Dispatcher.Invoke( _window.StartPulsing );
                }
                else if ( currentMeetingEnded )
                {
                    _window?.Dispatcher.Invoke( _window.StopPulsing );
                }

                // Update background colors
                if ( currentMeeting != null )
                {
                    activeColors.Add( Settings.Default.MeetingInProgressBackgroundColor );
                }

                if ( upcomingMeeting != null )
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

            // Clear tracked meeting
            _currentMeeting = null;
        }
    }

    private void UpdateBackgroundColors( List<Color> colors )
    {
        _window?.Dispatcher.Invoke( () => _window.BackgroundGradientColors = [ .. colors ] );
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
