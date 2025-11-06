using System;

namespace DesktopClock.Utilities;

/// <summary>
/// Converts DateTime to natural language time format (e.g., "Half past Two", "Noon", "11 o'Clock").
/// </summary>
public static class NaturalLanguageTimeFormatter
{
    private static readonly string[] HourNames = new[]
    {
        "Twelve", "One", "Two", "Three", "Four", "Five", "Six",
        "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve"
    };

    private static readonly string[] MinuteNames = new[]
    {
        "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine",
        "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen",
        "Eighteen", "Nineteen", "Twenty", "Twenty-One", "Twenty-Two", "Twenty-Three",
        "Twenty-Four", "Twenty-Five", "Twenty-Six", "Twenty-Seven", "Twenty-Eight",
        "Twenty-Nine", "Thirty"
    };

    /// <summary>
    /// Converts a DateTime to natural language time format.
    /// </summary>
    /// <param name="dateTime">The DateTime to convert.</param>
    /// <returns>A natural language representation of the time.</returns>
    public static string Format(DateTime dateTime)
    {
        var hour = dateTime.Hour;
        var minute = dateTime.Minute;
        var hour12 = hour % 12;
        string hourName;

        // Special cases
        if (minute == 0 && hour == 12)
        {
            return "Noon";
        }

        if (minute == 0 && hour == 0)
        {
            return "Midnight";
        }

        // Handle exact hours (for hours other than 0 and 12)
        if (minute == 0)
        {
            // Use word format for exact hours (e.g., "Eleven o'Clock")
            hourName = HourNames[hour12];
            return $"{hourName} o'Clock";
        }

        // Handle half past
        if (minute == 30)
        {
            hourName = HourNames[hour12];
            return $"Half past {hourName}";
        }

        // Handle quarter past
        if (minute == 15)
        {
            hourName = HourNames[hour12];
            return $"Quarter past {hourName}";
        }

        // Handle quarter to
        if (minute == 45)
        {
            var nextHour = (hour12 + 1) % 12;
            hourName = HourNames[nextHour];
            return $"Quarter to {hourName}";
        }

        // Handle minutes to (only when minute > 30 and minute % 5 == 0)
        // This covers 35, 40, 50, 55 (30 is "Half past", 45 is "Quarter to")
        if (minute > 30 && minute % 5 == 0)
        {
            var minutesTo = 60 - minute;
            var nextHourName = HourNames[(hour12 + 1) % 12];
            var minutesToName = GetMinuteName(minutesTo);
            return $"{minutesToName} minute{(minutesTo == 1 ? "" : "s")} to {nextHourName}";
        }

        // Handle minutes past (for all other cases)
        var minuteName = GetMinuteName(minute);
        hourName = HourNames[hour12];
        return $"{minuteName} minute{(minute == 1 ? "" : "s")} past {hourName}";
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
                // 31-39: "Thirty-One" through "Thirty-Nine"
                var ones = minute % 10;
                if (ones == 0)
                {
                    return "Thirty";
                }
                return $"Thirty-{MinuteNames[ones]}";
            }
            else if (minute <= 49)
            {
                // 40-49: "Forty" through "Forty-Nine"
                var ones = minute % 10;
                if (ones == 0)
                {
                    return "Forty";
                }
                return $"Forty-{MinuteNames[ones]}";
            }
            else
            {
                // 50-59: "Fifty" through "Fifty-Nine"
                var ones = minute % 10;
                if (ones == 0)
                {
                    return "Fifty";
                }
                return $"Fifty-{MinuteNames[ones]}";
            }
        }

        return minute.ToString();
    }
}

