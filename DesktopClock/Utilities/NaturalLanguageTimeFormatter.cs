namespace DesktopClock.Utilities;

/// <summary>
/// Converts DateTime to natural language time format (e.g., "Half past two", "Noon", "Eleven o'clock").
/// </summary>
public class NaturalLanguageTimeFormatter(
    bool its = true,
    bool capitalizeFirst = true,
    bool useOClock = true
    )
{
    private static readonly string[] _hourNames =
    [
        "midnight", "one", "two", "three", "four", "five", "six",
        "seven", "eight", "nine", "ten", "eleven", "noon"
    ];

    private static readonly string[] _minuteNames =
    [
        "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten",
        "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen"
    ];

    /// <summary>
    /// Converts a DateTime to natural language time format.
    /// </summary>
    /// <param name="dateTime">The DateTime to convert.</param>
    /// <returns>A natural language representation of the time.</returns>
    public string Format( DateTime dateTime )
    {
        string itsPrefix = its ? "it's " : "";
        string timeString = $"{itsPrefix}{GetTimeString( dateTime )}";
        if ( capitalizeFirst && timeString.Length > 0 )
        {
            timeString = timeString[ ..1 ].ToUpper() + timeString[ 1.. ];
        }

        return timeString;
    }

    private string GetTimeString( DateTime dateTime )
    {
        int hour = dateTime.Hour;
        int minute = dateTime.Minute;
        string hourName = GetHourName( hour );
        string nextHourName = GetHourName( hour + 1 );
        string minutesPast = GetMinuteName( minute );
        string minutesTo = GetMinuteName( 60 - minute );
        return (hour, minute) switch {
            // noon or midnight
            (12, 0 ) or (0, 0 ) => hourName,

            // Handle exact hours
            (_, 0 ) => $"{hourName}{( useOClock ? " o'clock" : "" )}",

            // Handle small minutes past
            (_, < 10 ) => $"{minutesPast} minute{( minute == 1 ? "" : "s" )} past {hourName}",

            // Handle quarter past
            (_, 15 ) => $"quarter past {hourName}",

            // Handle half past
            (_, 30 ) => $"half past {hourName}",

            // Handle quarter to
            (_, 45 ) => $"quarter to {nextHourName}",

            // Handle %5 to
            (_, > 30 ) when minute % 5 == 0 => $"{minutesTo} to {nextHourName}",

            // Handle small minutes to
            (_, > 55 ) => $"{minutesTo} minutes to {nextHourName}",

            // "noon thirteen sounds wrong"
            (12, _ ) or (0, _ ) => $"{minutesPast} past {hourName}",

            // Handle minutes past
            _ => $"{hourName} {minutesPast}",
        };
    }

    private static string GetHourName( int hour ) => hour switch {
        12 => _hourNames[ 12 ],
        24 => _hourNames[ 0 ],
        _ => _hourNames[ hour % 12 ]
    };

    private static string GetMinuteName( int minute ) => minute switch {
        // Handle out of range first
        < 0 or > 60 => throw new ArgumentOutOfRangeException( nameof( minute ), minute, "Minute must be between 0 and 59" ),

        // Handle simple cases (0-19) directly from array
        < 20 => _minuteNames[ minute ],

        // For minutes >= 20, construct compound numbers consistently
        _ => GetCompoundMinuteName( minute )
    };

    private static string GetCompoundMinuteName( int minute )
    {
        var tens = minute / 10;
        var ones = minute % 10;

        var tensName = tens switch {
            2 => "twenty",
            3 => "thirty",
            4 => "forty",
            5 => "fifty",
            6 => "sixty",
            _ => throw new ArgumentOutOfRangeException( nameof( minute ), $"Unexpected tens value: {tens}" )
        };

        return ones == 0 ? tensName : $"{tensName}-{_minuteNames[ ones ]}";
    }
}

