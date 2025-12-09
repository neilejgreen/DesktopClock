using System.ComponentModel;
using System.IO;
using System.Windows.Media;
using Newtonsoft.Json;
using WpfWindowPlacement;

namespace DesktopClock.Properties;

public sealed class Settings : INotifyPropertyChanged, IDisposable
{
    private readonly FileSystemWatcher _watcher;

    private static readonly Lazy<Settings> _default = new( LoadAndAttemptSave );

    private static readonly JsonSerializerSettings _jsonSerializerSettings = new() {
        // Make it easier to read by a human.
        Formatting = Formatting.Indented,

        // Prevent a single error from taking down the whole file.
        Error = ( _, e ) => e.ErrorContext.Handled = true,
    };


    static Settings()
    {
        // Settings file path from ~/.config directory.
        var configDir = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ), ".config" );
        Directory.CreateDirectory( configDir );
        var settingsFileName = Path.GetFileNameWithoutExtension( App.MainFileInfo.FullName ) + ".settings";
        FilePath = Path.Combine( configDir, settingsFileName );
    }

    // Private constructor to enforce singleton pattern.
    private Settings()
    {
        // Watch for changes in the settings file.
        _watcher = new( Path.GetDirectoryName( FilePath ), Path.GetFileName( FilePath ) ) {
            EnableRaisingEvents = true,
        };
        _watcher.Changed += FileChanged;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// The singleton instance of the local settings file.
    /// </summary>
    public static Settings Default => _default.Value;

    /// <summary>
    /// The full path to the settings file.
    /// </summary>
    public static string FilePath { get; private set; }

    /// <summary>
    /// Indicates if the settings file can be saved to.
    /// </summary>
    /// <remarks>
    /// <c>false</c> could indicate the file is in a folder that requires administrator permissions among other constraints.
    /// </remarks>
    public static bool CanBeSaved { get; private set; }

    /// <summary>
    /// Checks if the settings file exists on the disk.
    /// </summary>
    public static bool Exists => File.Exists( FilePath );

    #region "Properties"

    /// <summary>
    /// .NET format string for the time shown on the clock. Format specific parts inside { and }.
    /// </summary>
    /// <remarks>
    /// See: <see href="https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings">Custom date and time format strings</see>.
    /// </remarks>
    public string Format { get; set; } = "{ddd}, {MMM dd}, {h:mm:ss tt}";

    /// <summary>
    /// Enables natural language time format (e.g., "Half past Two", "Noon", "11 o'Clock").
    /// When enabled, the Format property is ignored for time display.
    /// </summary>
    public bool UseNaturalLanguage { get; set; } = false;

    /// <summary>
    /// Font to use for the clock's text.
    /// </summary>
    public string FontFamily
    {
        get;
        set
        {
            if ( field != value )
            {
                field = value;
                PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( nameof( FontFamily ) ) );
            }
        }
    } = "Consolas";

    /// <summary>
    /// Style of font to use for the clock's text.
    /// </summary>
    public string FontStyle
    {
        get;
        set
        {
            if ( field != value )
            {
                field = value;
                PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( nameof( FontStyle ) ) );
            }
        }
    } = "Normal";

    /// <summary>
    /// Text color for the clock's text.
    /// </summary>
    public Color TextColor
    {
        get => _textColor;
        set
        {
            if ( _textColor != value )
            {
                _textColor = value;
                PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( nameof( TextColor ) ) );
                PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( nameof( EffectiveTextColor ) ) );
            }
        }
    }
    private Color _textColor;

    /// <summary>
    /// Automatically adjust text lightness based on background behind the clock.
    /// </summary>
    public bool AutoAdjustTextColor { get; set; } = false;

    /// <summary>
    /// How frequently to check and update text lightness (in seconds).
    /// </summary>
    public int AutoColorUpdateInterval { get; set; } = 5;

    /// <summary>
    /// Override text color used by auto-adjustment feature. When null, uses TextColor.
    /// This field is not serialized.
    /// </summary>
    [JsonIgnore]
    public Color? OverrideTextColor
    {
        get => _overrideTextColor;
        set
        {
            if ( _overrideTextColor != value )
            {
                _overrideTextColor = value;
                PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( nameof( EffectiveTextColor ) ) );
            }
        }
    }
    private Color? _overrideTextColor;

    /// <summary>
    /// The actual text color to display. Returns OverrideTextColor if set, otherwise TextColor.
    /// This property is read-only and computed.
    /// </summary>
    [JsonIgnore]
    public Color EffectiveTextColor => OverrideTextColor ?? TextColor;

    /// <summary>
    /// Opacity of the window.
    /// </summary>
    public double WindowOpacity
    {
        get;
        set
        {
            if ( field != value )
            {
                field = value;
                PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( nameof( WindowOpacity ) ) );
            }
        }
    } = 1;

    /// <summary>
    /// Keeps the clock on top of other windows.
    /// </summary>
    public bool Topmost
    {
        get;
        set
        {
            if ( field != value )
            {
                field = value;
                PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( nameof( Topmost ) ) );
            }
        }
    } = true;

    /// <summary>
    /// Height of the clock window.
    /// </summary>
    public int Height
    {
        get;
        set
        {
            if ( field != value )
            {
                field = value;
                PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( nameof( Height ) ) );
            }
        }
    } = 48;

    /// <summary>
    /// Opens the app when you log in.
    /// </summary>
    public bool RunOnStartup { get; set; } = false;

    /// <summary>
    /// Makes the clock ignore mouse clicks (click-through) so underlying windows receive input.
    /// Also hides the window from Alt+Tab when enabled.
    /// </summary>
    public bool ClickThrough
    {
        get;
        set
        {
            if ( field != value )
            {
                field = value;
                PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( nameof( ClickThrough ) ) );
            }
        }
    } = true;

    /// <summary>
    /// The last text shown on the clock, saved to maintain the dimensions on the next launch.
    /// </summary>
    public string LastDisplay { get; set; }

    /// <summary>
    /// Window placement settings to preserve the location of the clock on the screen.
    /// </summary>
    public WindowPlacement Placement { get; set; }

    public Color OutlookMeetingBackgroundColor
    {
        get;
        set
        {
            if ( field != value )
            {
                field = value;
                PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( nameof( OutlookMeetingBackgroundColor ) ) );
            }
        }
    }

    public int OutlookMeetingLookAheadMinutes { get; set; }

    /// <summary>
    /// Override background color. When null, background is transparent.
    /// This field is not serialized.
    /// </summary>
    [JsonIgnore]
    public Color? OverrideBackgroundColor
    {
        get => _overrideBackgroundColor;
        set
        {
            if ( _overrideBackgroundColor != value )
            {
                _overrideBackgroundColor = value;
                PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( nameof( EffectiveBackgroundColor ) ) );
            }
        }
    }
    private Color? _overrideBackgroundColor;

    /// <summary>
    /// The actual background color to display. Returns OverrideBackgroundColor if set, otherwise OutlookMeetingBackgroundColor.
    /// This property is read-only and computed.
    /// </summary>
    [JsonIgnore]
    public Color EffectiveBackgroundColor => OverrideBackgroundColor ?? Colors.Transparent;
    #endregion "Properties"

    /// <summary>
    /// Saves to the default path in JSON format.
    /// </summary>
    public bool Save()
    {
        try
        {
            var json = JsonConvert.SerializeObject( this, _jsonSerializerSettings );

            // Attempt to save multiple times.
            for ( var i = 0; i < 4; i++ )
            {
                try
                {
                    File.WriteAllText( FilePath, json );
                    return true;
                }
                catch
                {
                    // Wait before next attempt to read.
                    Thread.Sleep( 250 );
                }
            }
        }
        catch ( JsonSerializationException )
        {
        }

        return false;
    }

    /// <summary>
    /// Populates the given settings with values from the default path.
    /// </summary>
    private static void Populate( Settings settings )
    {
        using var fileStream = new FileStream( FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite );
        using var streamReader = new StreamReader( fileStream );
        using var jsonReader = new JsonTextReader( streamReader );

        JsonSerializer.Create( _jsonSerializerSettings ).Populate( jsonReader, settings );
    }

    /// <summary>
    /// Loads from the default path in JSON format.
    /// </summary>
    private static Settings LoadFromFile()
    {
        try
        {
            var settings = new Settings();
            Populate( settings );
            return settings;
        }
        catch
        {
            return new();
        }
    }

    /// <summary>
    /// Loads from the default path in JSON format then attempts to save in order to check if it can be done.
    /// </summary>
    private static Settings LoadAndAttemptSave()
    {
        var settings = LoadFromFile();

        CanBeSaved = settings.Save();

        return settings;
    }

    /// <summary>
    /// Occurs after the watcher detects a change in the settings file.
    /// </summary>
    private void FileChanged( object sender, FileSystemEventArgs e )
    {
        try
        {
            Populate( this );
        }
        catch
        {
        }
    }


    public void Dispose()
    {
        // We don't dispose of the watcher anymore because it would actually hang indefinitely if you had multiple instances of the same clock open.
        //_watcher?.Dispose();
    }
}
