using DesktopClock.Data;

namespace DesktopClock.Modules;

/// <summary>
/// Module that displays a notification icon when there are unread emails.
/// </summary>
public class UnreadMailModule : IWindowModule
{
    private readonly OutlookCalendarService _outlookService;
    private MainWindow _window;
    private bool _disposed;
    private System.Threading.Timer _updateTimer;

    public UnreadMailModule()
    {
        _outlookService = new OutlookCalendarService();
    }

    public void Initialize( MainWindow window )
    {
        _window = window ?? throw new ArgumentNullException( nameof( window ) );

        // Initial update
        UpdateUnreadMailIndicator();

        // Start periodic updates every 30 seconds
        _updateTimer = new System.Threading.Timer(
            _ => UpdateUnreadMailIndicator(),
            null,
            TimeSpan.FromSeconds( 30 ),
            TimeSpan.FromSeconds( 30 )
        );
    }

    private void UpdateUnreadMailIndicator()
    {
        if ( _disposed || _window == null )
        {
            return;
        }

        try
        {
            int unreadCount = _outlookService.GetUnreadMailCount();
            string notificationText = unreadCount > 0 ? "📨" : string.Empty;

            _window.Dispatcher.Invoke( () => {
                _window.NotificationText = notificationText;
            } );
        }
        catch
        {
            // If Outlook is not available or any error occurs, don't show the icon
            _window.Dispatcher.Invoke( () => {
                _window.NotificationText = string.Empty;
            } );
        }
    }

    public void Dispose()
    {
        if ( _disposed )
        {
            return;
        }

        _updateTimer?.Dispose();
        _outlookService?.Dispose();
        _disposed = true;
    }
}
