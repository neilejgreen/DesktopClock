using System.Runtime.InteropServices;
using Microsoft.Office.Interop.Outlook;
using Exception = System.Exception;

namespace DesktopClock.Data;

/// <summary>
/// Service for accessing Outlook calendar via COM interop.
/// </summary>
public class OutlookCalendarService : IDisposable
{
    private Application _outlookApp;
    private NameSpace _namespace;
    private MAPIFolder _calendarFolder;
    private MAPIFolder _inboxFolder;
    private bool _disposed;

    /// <summary>
    /// Gets a value indicating whether Outlook is available and accessible.
    /// </summary>
    public bool IsAvailable
    {
        get
        {
            try
            {
                return GetOutlookApplication() != null;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Gets the Outlook application instance, creating it if necessary.
    /// </summary>
    private Application GetOutlookApplication()
    {
        if ( _outlookApp != null )
        {
            return _outlookApp;
        }

        try
        {
            // Create or get existing Outlook instance
            // COM will return existing instance if Outlook is already running
            _outlookApp = new Application();
        }
        catch
        {
            // Outlook is not installed or cannot be accessed
            throw new InvalidOperationException( "Outlook is not installed or cannot be accessed." );
        }

        return _outlookApp;
    }

    /// <summary>
    /// Gets information about the next meeting starting within the specified time span.
    /// </summary>
    /// <param name="lookAhead">The time span to look ahead for meetings.</param>
    /// <returns>Meeting information if found, null otherwise.</returns>
    public MeetingInfo GetUpcomingMeeting( TimeSpan lookAhead )
    {
        ThrowIfDisposed();

        var now = DateTime.Now;
        var endTime = now.Add( lookAhead );
        var filter = $"[Start] >= '{now:g}' AND [Start] <= '{endTime:g}'";

        return FindMeeting( filter );
    }

    /// <summary>
    /// Gets information about the next meeting starting within 10 minutes.
    /// </summary>
    /// <returns>Meeting information if found, null otherwise.</returns>
    public MeetingInfo GetUpcomingMeetingInNext10Minutes()
    {
        return GetUpcomingMeeting( TimeSpan.FromMinutes( 10 ) );
    }

    /// <summary>
    /// Gets information about the currently active meeting (meeting that has started but not ended).
    /// </summary>
    /// <returns>Meeting information if found, null otherwise.</returns>
    public MeetingInfo GetCurrentMeeting()
    {
        ThrowIfDisposed();

        var now = DateTime.Now;
        var filter = $"[Start] <= '{now:g}' AND [End] >= '{now:g}'";

        return FindMeeting( filter );
    }

    /// <summary>
    /// Throws ObjectDisposedException if the service has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if ( _disposed )
        {
            throw new ObjectDisposedException( nameof( OutlookCalendarService ) );
        }
    }

    /// <summary>
    /// Ensures Outlook calendar folder is initialized.
    /// </summary>
    private void EnsureCalendarFolderInitialized()
    {
        _outlookApp ??= new Application();
        _namespace ??= _outlookApp.GetNamespace( "MAPI" );
        _calendarFolder ??= _namespace.GetDefaultFolder( OlDefaultFolders.olFolderCalendar );
    }

    /// <summary>
    /// Finds a meeting matching the given filter and additional condition.
    /// </summary>
    /// <param name="filter">The Outlook filter string.</param>
    /// <returns>Meeting information if found, null otherwise.</returns>
    private MeetingInfo FindMeeting( string filter )
    {
        try
        {
            EnsureCalendarFolderInitialized();

            var calendarItems = _calendarFolder.Items;

            // Sort items by start time and include recurring appointments
            calendarItems.Sort( "[Start]" );
            calendarItems.IncludeRecurrences = true;

            // Filter for appointments in the time range
            var filteredItems = calendarItems.Restrict( filter );

            foreach ( AppointmentItem item in filteredItems )
            {
                try
                {
                    // Check if it's a meeting (not just an appointment) and not canceled
                    // Also exclude all-day and multi-day meetings
                    bool
                        isMeeting = IsMeeting( item ),
                        isAllDayOrMultiDay = IsAllDayOrMultiDay( item );

                    if ( isMeeting && !isAllDayOrMultiDay )
                    {
                        return CreateMeetingInfo( item );
                    }
                }
                catch
                {
                    // If we can't access the item properties, skip it and continue
                    continue;
                }
            }

            return null;
        }
        catch ( COMException ex )
        {
            throw new InvalidOperationException( "Failed to access Outlook calendar: " + ex.Message, ex );
        }
        catch ( Exception ex )
        {
            throw new InvalidOperationException( "Unexpected error accessing Outlook: " + ex.Message, ex );
        }
    }

    /// <summary>
    /// Checks if an appointment item is a valid meeting.
    /// </summary>
    private static bool IsMeeting( AppointmentItem item )
    {
        // Check traditional meeting status (meetings with attendees)
        var hasValidMeetingStatus = item.MeetingStatus is not OlMeetingStatus.olMeetingCanceled and not OlMeetingStatus.olNonMeeting;

        // Also check if it has recipients (attendees), which indicates it's a meeting
        // even if the MeetingStatus isn't set correctly (common with Teams meetings)
        var hasRecipients = !string.IsNullOrEmpty( item.RequiredAttendees ) || !string.IsNullOrEmpty( item.OptionalAttendees );

        // Check if it's a Teams meeting by looking at location or body
        var location = item.Location ?? string.Empty;
        var body = item.Body ?? string.Empty;
        var isTeamsMeeting = IsTeamsMeeting( location, body );

        // It's a meeting if:
        // 1. It has a valid meeting status, OR
        // 2. It has recipients/attendees, OR
        // 3. It's identified as a Teams meeting
        return hasValidMeetingStatus || hasRecipients || isTeamsMeeting;
    }

    /// <summary>
    /// Checks if an appointment is an all-day or multi-day meeting.
    /// </summary>
    private static bool IsAllDayOrMultiDay( AppointmentItem item )
    {
        // Check if it's marked as an all-day event
        if ( item.AllDayEvent )
        {
            return true;
        }

        // Check if it spans multiple days (more than 24 hours)
        var duration = TimeSpan.FromMinutes( item.Duration );
        if ( duration.TotalHours >= 24 )
        {
            return true;
        }

        // Check if start and end dates are on different days
        var endTime = item.Start.AddMinutes( item.Duration );
        if ( item.Start.Date != endTime.Date )
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Creates a MeetingInfo object from an AppointmentItem.
    /// </summary>
    private static MeetingInfo CreateMeetingInfo( AppointmentItem item )
    {
        string location = item.Location ?? string.Empty;

        return new MeetingInfo {
            Subject = item.Subject ?? string.Empty,
            StartTime = new DateTimeOffset( item.Start ),
            Location = location,
            Duration = TimeSpan.FromMinutes( item.Duration ),
            Organizer = item.Organizer ?? string.Empty
        };
    }

    /// <summary>
    /// Determines if a meeting is a Teams meeting by checking location and body for Teams indicators.
    /// </summary>
    private static bool IsTeamsMeeting( string location, string body )
    {
        if ( string.IsNullOrWhiteSpace( location ) && string.IsNullOrWhiteSpace( body ) )
        {
            return false;
        }

        var searchText = ( location + " " + body ).ToLowerInvariant();

        // Common Teams meeting indicators
        return searchText.Contains( "microsoft teams" ) ||
               searchText.Contains( "teams.microsoft.com" ) ||
               searchText.Contains( "teams.live.com" ) ||
               searchText.Contains( "conf.teams.microsoft.com" ) ||
               searchText.Contains( "join.microsoft.com" ) ||
               ( searchText.Contains( "teams" ) && ( searchText.Contains( "meeting" ) || searchText.Contains( "join" ) ) );
    }

    /// <summary>
    /// Gets the count of unread emails in the Outlook inbox.
    /// </summary>
    /// <returns>The number of unread emails, or 0 if unavailable.</returns>
    public int GetUnreadMailCount()
    {
        ThrowIfDisposed();

        try
        {
            EnsureInboxFolderInitialized();

            // Get unread items count from the inbox
            return _inboxFolder.UnReadItemCount;
        }
        catch ( Exception )
        {
            // If Outlook is not available or any error occurs, return 0
            return 0;
        }
    }

    /// <summary>
    /// Ensures Outlook inbox folder is initialized.
    /// </summary>
    private void EnsureInboxFolderInitialized()
    {
        _outlookApp ??= new Application();
        _namespace ??= _outlookApp.GetNamespace( "MAPI" );
        _inboxFolder ??= _namespace.GetDefaultFolder( OlDefaultFolders.olFolderInbox );
    }

    public void Dispose()
    {
        if ( !_disposed )
        {
            _outlookApp = null;
            _disposed = true;
        }
    }
}

/// <summary>
/// Information about an Outlook meeting.
/// </summary>
public class MeetingInfo
{
    /// <summary>
    /// Gets or sets the meeting subject.
    /// </summary>
    public string Subject { get; set; }

    /// <summary>
    /// Gets or sets the meeting start time with timezone information.
    /// </summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// Gets or sets the meeting location.
    /// </summary>
    public string Location { get; set; }

    /// <summary>
    /// Gets or sets the meeting duration.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets the meeting organizer.
    /// </summary>
    public string Organizer { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is a Microsoft Teams meeting.
    /// </summary>
    public bool IsTeamsMeeting { get; set; }

    /// <summary>
    /// Gets a formatted string representation of the meeting.
    /// </summary>
    public override string ToString()
    {
        // Use DateTimeOffset.Now to properly compare with StartTime (which is also DateTimeOffset)
        var now = DateTimeOffset.Now;
        var timeUntil = StartTime - now;
        var timeUntilStr = timeUntil.TotalMinutes < 1
            ? "starting now"
            : $"in {timeUntil.TotalMinutes:F0} minute{( timeUntil.TotalMinutes == 1 ? "" : "s" )}";

        var locationStr = string.IsNullOrWhiteSpace( Location ) ? "" : $" at {Location}";
        return $"{Subject} {timeUntilStr}{locationStr}";
    }
}
