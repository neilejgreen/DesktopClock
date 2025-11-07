using System;

namespace DesktopClock.Utilities;

/// <summary>
/// Converts DateTime to natural language time format (e.g., "Half past two", "Noon", "Eleven o'clock").
/// </summary>
public class NaturalLanguageTimeFormatter(bool its = true, bool capitalizeFirst = true)
{
    private static readonly string[] HourNames =
    [
        "twelve", "one", "two", "three", "four", "five", "six",
        "seven", "eight", "nine", "ten", "eleven", "twelve"
    ];

    private static readonly string[] MinuteNames =
    [
        "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine",
        "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen",
        "eighteen", "nineteen", "twenty", "twenty-one", "twenty-two", "twenty-three",
        "twenty-four", "twenty-five", "twenty-six", "twenty-seven", "twenty-eight",
        "twenty-nine", "thirty"
    ];

    /// <summary>
    /// Converts a DateTime to natural language time format.
    /// </summary>
    /// <param name="dateTime">The DateTime to convert.</param>
    /// <returns>A natural language representation of the time.</returns>
    public string Format(DateTime dateTime)
    {
        string itsPrefix = its ? "It's " : "";
        string timeString = $"{itsPrefix}{GetTimeString(dateTime)}";
        timeString = capitalizeFirst ? CapitalizeFirst(timeString) : timeString;
        return timeString;
    }

    private string GetTimeString(DateTime dateTime)
    {
        var hour = dateTime.Hour;
        var minute = dateTime.Minute;
        var hour12 = hour % 12;
        var hourName = HourNames[hour12];
        var nextHourName = HourNames[(hour12 + 1) % 12];
        var minutesPast = GetMinuteName(minute);
        var minutesTo = GetMinuteName(60 - minute);
        return (hour, minute) switch
        {
            // Special cases
            (12, 0) => "noon",
            (0, 0) => "midnight",

            // Handle exact hours (for hours other than 0 and 12)
            (_, 0) => $"{hourName} o'clock",

            (_, < 10) => $"{minutesPast} minute{(minute == 1 ? "" : "s")} past {hourName}",

            // Handle quarter past
            (_, 15) => $"quarter past {hourName}",

            // Handle half past
            (_, 30) => $"half past {hourName}",

            // Handle quarter to
            (_, 45) => $"quarter to {nextHourName}",

            // Handle small minutes to
            (_, >= 55) => $"{minutesTo} minutes to {nextHourName}",

            // Handle %5 to
            (_, int min) when min % 5 == 0 => $"{minutesTo} to {nextHourName}",

            // Handle minutes past
            _ => $"{hourName} {minutesPast}",
        };
    }

    private static string GetMinuteName(int minute)
    {
        if (minute <= 30)
        {
            return MinuteNames[minute];
        }

        // For minutes > 30, we need to construct the name
        // This is used for "past" format when minute > 30 and not a multiple of 5
        if (minute <= 59)
        {
            if (minute <= 39)
            {
                // 31-39: "thirty-one" through "thirty-nine"
                var ones = minute % 10;
                if (ones == 0)
                {
                    return "thirty";
                }
                return $"thirty-{MinuteNames[ones]}";
            }
            else if (minute <= 49)
            {
                // 40-49: "forty" through "forty-nine"
                var ones = minute % 10;
                if (ones == 0)
                {
                    return "forty";
                }
                return $"forty-{MinuteNames[ones]}";
            }
            else
            {
                // 50-59: "fifty" through "fifty-nine"
                var ones = minute % 10;
                if (ones == 0)
                {
                    return "fifty";
                }
                return $"fifty-{MinuteNames[ones]}";
            }
        }

        return minute.ToString();
    }

    /// <summary>
    /// Capitalizes only the first letter of the string.
    /// </summary>
    private static string CapitalizeFirst(string text) =>
        text switch
        {
            null or "" => text,
            _ => text.Substring(0, 1).ToUpper() + text.Substring(1)
        };
}

