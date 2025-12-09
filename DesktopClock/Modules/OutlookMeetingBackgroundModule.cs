using System.Windows.Media;
using DesktopClock.Data;
using DesktopClock.Properties;

namespace DesktopClock.Modules;

/// <summary>
/// Module that changes the window background to Outlook blue when there is an upcoming meeting.
/// </summary>
public class OutlookMeetingBackgroundModule : IWindowModule
{
    private readonly OutlookCalendarService _calendarService;
    private MainWindow _window;
    private bool _disposed;

    // Check for meetings every 30 seconds
    private const int CheckIntervalMs = 3000;

    public OutlookMeetingBackgroundModule()
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
            await Task.Delay( CheckIntervalMs );
        }

        CheckForUpcomingMeeting();
    }

    private void OnSettingsChanged( object sender, System.ComponentModel.PropertyChangedEventArgs e )
    {
        if ( _disposed || _window == null )
        {
            return;
        }

        if ( e.PropertyName is nameof( Settings.UpcomingMeetingBackgroundColor ) or
         nameof( Settings.MeetingInProgressBackgroundColor ) )
        {
            CheckForUpcomingMeeting();
        }
        else if ( e.PropertyName == nameof( Settings.Default.MeetingLookAheadMinutes ) )
        {
            // Re-check when lookahead changes
            CheckForUpcomingMeeting();
        }
    }

    private void CheckForUpcomingMeeting()
    {
        try
        {
            if ( !_calendarService.IsAvailable )
            {
                // Outlook not available, ensure background is transparent
                SetBackground( Status.NoService );
                return;
            }

            // First check for currently active Teams meeting (highest priority)
            var currentMeeting = _calendarService.GetCurrentMeeting();
            if ( currentMeeting != null && currentMeeting.IsTeamsMeeting )
            {
                SetBackground( Status.MeetingInProgress );
                return;
            }

            // Then check for upcoming meetings
            var lookAhead = TimeSpan.FromMinutes( Settings.Default.MeetingLookAheadMinutes );
            var upcomingMeeting = _calendarService.GetUpcomingMeeting( lookAhead );
            bool hasMeeting = upcomingMeeting != null;
            if ( hasMeeting )
            {
                // System.Windows.MessageBox.Show( $"Upcoming meeting: {upcomingMeeting}" );
                SetBackground( Status.MeetingUpcoming );
            }
            else
            {
                SetBackground( Status.NoMeeting );
            }
        }
        catch
        {
            // If there's an error accessing Outlook, set background to transparent
            SetBackground( Status.NoMeeting );
        }
    }

    private enum Status
    {
        NoService,
        NoMeeting,
        MeetingUpcoming,
        MeetingInProgress
    }

    private void SetBackground( Status status )
    {
        if ( _disposed || _window == null )
        {
            return;
        }
        Color meetingColor = status switch {
            Status.NoService => Colors.Purple,
            Status.NoMeeting => Colors.Transparent,
            Status.MeetingUpcoming => Settings.Default.UpcomingMeetingBackgroundColor,
            Status.MeetingInProgress => Settings.Default.MeetingInProgressBackgroundColor,
            _ => Colors.Transparent
        };

        Settings.Default.OverrideBackgroundColor = meetingColor;
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

