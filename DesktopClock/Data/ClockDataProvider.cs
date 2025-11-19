using System;
using System.Globalization;
using DesktopClock.Properties;
using DesktopClock.Utilities;

namespace DesktopClock.Data;

/// <summary>
/// Data provider that supplies clock/time information.
/// </summary>
public class ClockDataProvider : IDataProvider, IDisposable
{
    private readonly SystemClockTimer _systemClockTimer;

    /// <summary>
    /// Event that fires when the time changes.
    /// </summary>
    public event EventHandler DataChanged;

    public ClockDataProvider()
    {
        _systemClockTimer = new SystemClockTimer();
        _systemClockTimer.SecondChanged += (s, e) => DataChanged?.Invoke(this, EventArgs.Empty);
        _systemClockTimer.Start();
    }

    /// <summary>
    /// Gets the current time as a formatted string.
    /// </summary>
    public string GetDisplayText()
    {
        var now = DateTimeOffset.Now;

        if (Settings.Default.UseNaturalLanguage)
        {
            return new NaturalLanguageTimeFormatter(its: true, capitalizeFirst: false, useOClock: true).Format(now.DateTime);
        }

        return Tokenizer.FormatWithTokenizerOrFallBack(now, Settings.Default.Format, CultureInfo.DefaultThreadCurrentCulture);
    }

    public void Dispose()
    {
        _systemClockTimer?.Dispose();
    }
}

