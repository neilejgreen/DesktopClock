namespace DesktopClock.Modules;

/// <summary>
/// Interface for modules that can be added to the main window to extend functionality.
/// </summary>
public interface IWindowModule : IDisposable
{
    /// <summary>
    /// Initializes the module with access to the main window.
    /// </summary>
    /// <param name="window">The main window instance that the module can control.</param>
    void Initialize( MainWindow window );
}

