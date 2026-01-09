using System.Windows.Input;
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

    // Track dismissed meeting to prevent re-pulsing
    private MeetingInfo _dismissedMeeting;

    public BackgroundColorModule()
    {
        _calendarService = new OutlookCalendarService();

        // Listen to settings changes
        Settings.Default.PropertyChanged += OnSettingsChanged;
    }

    public void Initialize( MainWindow window )
    {
        _window = window ?? throw new ArgumentNullException( nameof( window ) );

        // Subscribe to mouse clicks for pulsing interactions
        _window.MouseDown += OnWindowMouseDown;

        // Check immediately on initialization
        _ = StartCheckingForMeetings();
    }

    private void OnWindowMouseDown( object sender, MouseButtonEventArgs e )
    {
        if ( !_window.IsPulsating )
        {
            return;
        }

        if ( e.ChangedButton == MouseButton.Middle )
        {
            // Middle-click: Dismiss the current meeting
            _dismissedMeeting = _currentMeeting;
            e.Handled = true;
            CheckForUpcomingMeeting();
        }
        else if ( e.ChangedButton == MouseButton.Left )
        {
            // Left-click: Just stop pulsing
            _window.StopPulsing();
            e.Handled = true;
        }
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

                // Clear tracked meetings
                _currentMeeting = null;
                _dismissedMeeting = null;
            }
            else
            {
                MeetingInfo currentMeeting = GetCurrentMeeting();

                // Check for upcoming meetings
                TimeSpan lookAhead = TimeSpan.FromMinutes( Settings.Default.MeetingLookAheadMinutes );
                MeetingInfo upcomingMeeting = _calendarService.GetUpcomingMeeting( lookAhead );

                // Detect when a meeting newly becomes current
                bool meetingNewlyBecameCurrent = currentMeeting != null &&
                    ( _currentMeeting == null ||
                      _currentMeeting.Subject != currentMeeting.Subject ||
                      _currentMeeting.StartTime != currentMeeting.StartTime );

                // Detect when a current meeting ends
                bool currentMeetingEnded = (_currentMeeting, currentMeeting) is (not null, null );

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
            UpdateBackgroundColors( [ Colors.Purple ] );

            // Clear tracked meetings
            _currentMeeting = null;
            _dismissedMeeting = null;
        }
    }

    private MeetingInfo GetCurrentMeeting()
    {
        // Clear dismissed meeting if it's in the past
        if ( _dismissedMeeting != null && _dismissedMeeting.StartTime.Add( _dismissedMeeting.Duration ) < DateTimeOffset.Now )
        {
            _dismissedMeeting = null;
        }

        // Check for currently active meeting
        MeetingInfo currentMeeting = _calendarService.GetCurrentMeeting();

        // If current meeting matches dismissed meeting, treat it as null
        if ( (currentMeeting?.Subject, currentMeeting.StartTime)
             == (_dismissedMeeting?.Subject, _dismissedMeeting?.StartTime) )
        {
            currentMeeting = null;
        }

        return currentMeeting;
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

        if ( _window != null )
        {
            _window.MouseDown -= OnWindowMouseDown;
        }

        Settings.Default.PropertyChanged -= OnSettingsChanged;
        _calendarService?.Dispose();

        _disposed = true;
    }
}
