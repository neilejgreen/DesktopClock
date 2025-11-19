using System;

namespace DesktopClock.Data;

/// <summary>
/// Interface for data providers that can supply display text to the main window.
/// </summary>
public interface IDataProvider
{
    /// <summary>
    /// Gets the current display text.
    /// </summary>
    string GetDisplayText();

    /// <summary>
    /// Event that fires when the data changes and the display should be updated.
    /// </summary>
    event EventHandler DataChanged;
}

