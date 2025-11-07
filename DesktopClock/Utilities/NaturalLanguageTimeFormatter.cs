using System;

namespace DesktopClock.Utilities;

/// <summary>
/// Converts DateTime to natural language time format (e.g., "Half past two", "Noon", "Eleven o'clock").
/// </summary>
public static class NaturalLanguageTimeFormatter
{
    private static readonly string[] HourNames = new[]
    {
        "twelve", "one", "two", "three", "four", "five", "six",
        "seven", "eight", "nine", "ten", "eleven", "twelve"
    };

    private static readonly string[] MinuteNames = new[]
    {
        "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine",
        "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen",
        "eighteen", "nineteen", "twenty", "twenty-one", "twenty-two", "twenty-three",
        "twenty-four", "twenty-five", "twenty-six", "twenty-seven", "twenty-eight",
        "twenty-nine", "thirty"
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
            // Use word format for exact hours (e.g., "Eleven o'clock")
            hourName = HourNames[hour12];
            return CapitalizeFirst($"{hourName} o'clock");
        }

        // Handle half past
        if (minute == 30)
        {
            hourName = HourNames[hour12];
            return CapitalizeFirst($"half past {hourName}");
        }

        // Handle quarter past
        if (minute == 15)
        {
            hourName = HourNames[hour12];
            return CapitalizeFirst($"quarter past {hourName}");
        }

        // Handle quarter to
        if (minute == 45)
        {
            var nextHour = (hour12 + 1) % 12;
            hourName = HourNames[nextHour];
            return CapitalizeFirst($"quarter to {hourName}");
        }

        // Handle minutes to (only when minute > 30 and minute % 5 == 0)
        // This covers 35, 40, 50, 55 (30 is "Half past", 45 is "Quarter to")
        if (minute > 30 && minute % 5 == 0)
        {
            var minutesTo = 60 - minute;
            var nextHourName = HourNames[(hour12 + 1) % 12];
            var minutesToName = GetMinuteName(minutesTo);
            return CapitalizeFirst($"{minutesToName} minute{(minutesTo == 1 ? "" : "s")} to {nextHourName}");
        }

        // Handle minutes past (for all other cases)
        var minuteName = GetMinuteName(minute);
        hourName = HourNames[hour12];
        return CapitalizeFirst($"{minuteName} minute{(minute == 1 ? "" : "s")} past {hourName}");
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
    private static string CapitalizeFirst(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        if (text.Length == 1)
        {
            return char.ToUpper(text[0]).ToString();
        }

        return char.ToUpper(text[0]) + text.Substring(1);
    }
}

