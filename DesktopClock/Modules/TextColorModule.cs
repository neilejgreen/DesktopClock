using System;
using System.Threading.Tasks;
using DesktopClock.Properties;
using DesktopClock.Utilities;

namespace DesktopClock.Modules;

/// <summary>
/// Module that displays the current time on the window.
/// </summary>
public class TextColorModule : IWindowModule
{
    private MainWindow _window;
    private bool _disposed;

    public void Initialize( MainWindow window )
    {
        _window = window ?? throw new ArgumentNullException( nameof( window ) );
        // Listen to settings changes
        Settings.Default.PropertyChanged += OnSettingsChanged;
        _ = StartAutoColorUpdate();
    }

    public async Task StartAutoColorUpdate()
    {
        while ( (_disposed, _window) is (false, { } ) )
        {
            TryUpdateTextColor();
            await Task.Delay( TimeSpan.FromSeconds( Settings.Default.AutoColorUpdateInterval ) );
        }
    }

    private void OnSettingsChanged( object sender, System.ComponentModel.PropertyChangedEventArgs e )
    {
        if ( _disposed || _window == null )
        {
            return;
        }

        if ( e.PropertyName is nameof( Settings.AutoAdjustTextColor ) )
        {
            Settings.Default.OverrideTextColor = null;
            // Immediately try to update text color when relevant settings change
            TryUpdateTextColor();
        }
    }

    /// <summary>
    /// Tries to update the text lightness based on the background behind the clock if auto adjustment is enabled.
    /// </summary>
    private void TryUpdateTextColor()
    {
        if ( _disposed || _window == null )
        {
            return;
        }

        if ( !Settings.Default.AutoAdjustTextColor )
        {
            return;
        }

        // Run color detection on dispatcher to ensure we have window bounds
        _window.Dispatcher.Invoke( () => {
            try
            {
                // Adjust the current text color to contrast with the background
                var optimalTextColor = ScreenColorDetector.GetOptimalTextColorWithHue();

                // Update the override text color without modifying the saved setting
                Settings.Default.OverrideTextColor = optimalTextColor;
            }
            catch
            {
                // Ignore errors to prevent crashes from screen capture issues
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
        _disposed = true;
    }
}

