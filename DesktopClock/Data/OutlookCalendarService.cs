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
        if ( _disposed )
        {
            throw new ObjectDisposedException( nameof( OutlookCalendarService ) );
        }

        try
        {
            // Get or create cached Outlook objects
            _outlookApp ??= new Application();

            _namespace ??= _outlookApp.GetNamespace( "MAPI" );

            _calendarFolder ??= _namespace.GetDefaultFolder( OlDefaultFolders.olFolderCalendar );

            var calendarItems = _calendarFolder.Items;

            var now = DateTime.Now;
            var endTime = now.Add( lookAhead );

            // Sort items by start time and include recurring appointments
            calendarItems.Sort( "[Start]" );
            calendarItems.IncludeRecurrences = true;

            // Filter for appointments in the time range
            var filter = $"[Start] >= '{now:g}' AND [Start] <= '{endTime:g}'";
            var filteredItems = calendarItems.Restrict( filter );

            foreach ( AppointmentItem item in filteredItems )
            {
                try
                {
                    // Check if it's a meeting (not just an appointment) and not canceled
                    if ( item.MeetingStatus is ( OlMeetingStatus.olMeeting or
                             OlMeetingStatus.olMeetingReceived ) and
                          not OlMeetingStatus.olMeetingCanceled )
                    {
                        // Ensure the meeting is actually in the future
                        if ( item.Start > now && item.Start <= endTime )
                        {
                            var meetingInfo = new MeetingInfo {
                                Subject = item.Subject ?? string.Empty,
                                StartTime = new DateTimeOffset( item.Start ),
                                Location = item.Location ?? string.Empty,
                                Duration = TimeSpan.FromMinutes( item.Duration ),
                                Organizer = item.Organizer ?? string.Empty
                            };

                            return meetingInfo;
                        }
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
    /// Gets information about the next meeting starting within 10 minutes.
    /// </summary>
    /// <returns>Meeting information if found, null otherwise.</returns>
    public MeetingInfo GetUpcomingMeetingInNext10Minutes()
    {
        return GetUpcomingMeeting( TimeSpan.FromMinutes( 10 ) );
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
