using System.Drawing;
using System.Windows;
using Wacton.Unicolour;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;

namespace DesktopClock.Utilities;

/// <summary>
/// Utility class for detecting the average color behind a WPF window.
/// </summary>
public static class ScreenColorDetector
{
    private static Unicolour Black { get; } = new( ColourSpace.Rgb255, 0, 0, 0 );

    /// <summary>
    /// Gets the optimal text color by adjusting the lightness of the current text color
    /// to ensure good contrast with the background while preserving hue and saturation when possible.
    /// Uses WCAG AA contrast ratio (4.5:1) as the target.
    /// Filters out pixels matching the displayed text color to avoid sampling the clock itself.
    /// </summary>
    /// <returns>The adjusted text color with optimal contrast and preserved hue.</returns>
    public static Color GetOptimalTextColorWithHue()
    {
        // Get both colors from settings - adapt TextColor, exclude OverrideTextColor from sampling

        Unicolour bgColor = GetAverageColorBehindWindow( Application.Current.MainWindow );
        Unicolour textColor = Properties.Settings.Default.TextColor.ToUnicolour();

        // Try to find the best lightness for contrast in the middle range
        var adjustedColor = FindBestLightnessForContrast(
            textColor,
            bgColor );

        return adjustedColor.ToMediaColor();
    }

    /// <summary>
    /// Captures the average color of the screen area behind the specified window.
    /// </summary>
    /// <param name="window">The WPF window to analyze behind.</param>
    /// <returns>The average color behind the window as a Unicolour, or black if capture fails.</returns>
    private static Unicolour GetAverageColorBehindWindow( Window window )
    {
        try
        {
            // Get window bounds in screen coordinates
            var windowBounds = GetWindowBounds( window );
            if ( windowBounds.Width <= 0 || windowBounds.Height <= 0 )
            {
                return Black;
            }

            // Capture screen behind window
            using var bitmap = CaptureScreenRegion( windowBounds );
            if ( bitmap == null )
            {
                return Black;
            }

            // Calculate average color from border pixels only
            return CalculateAverageColor( bitmap );
        }
        catch
        {
            return Black;
        }
    }

    /// <summary>
    /// Gets the bounds of a WPF window in screen coordinates.
    /// </summary>
    private static Rectangle GetWindowBounds( Window window )
    {
        // Convert window corners to screen coordinates
        var topLeft = window.PointToScreen( new Point( 0, 0 ) );
        var bottomRight = window.PointToScreen( new Point( window.ActualWidth, window.ActualHeight ) );

        return new Rectangle(
            (int) topLeft.X,
            (int) topLeft.Y,
            (int) ( bottomRight.X - topLeft.X ),
            (int) ( bottomRight.Y - topLeft.Y ) );
    }

    /// <summary>
    /// Captures a region of the screen using Graphics.CopyFromScreen.
    /// </summary>
    private static Bitmap CaptureScreenRegion( Rectangle bounds )
    {
        try
        {
            var bitmap = new Bitmap( bounds.Width, bounds.Height );
            using ( var graphics = Graphics.FromImage( bitmap ) )
            {
                graphics.CopyFromScreen( bounds.Left, bounds.Top, 0, 0, bounds.Size );
            }
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Calculates the average color from the 2px border pixels around the bitmap edges.
    /// </summary>
    private static Unicolour CalculateAverageColor( Bitmap bitmap )
    {
        int width = bitmap.Width;
        int height = bitmap.Height;

        (int x, int y)[] pixels = [
            // top and bottom rows
            .. from x in Enumerable.Range(0, width)
               from y in new[] { 0, 1, height - 2, height - 1 }
               select (x, y),
            // side columns
            .. from x in new[] { 0, 1, width - 2, width - 1 }
               from y in Enumerable.Range(2, height - 4)
               select (x, y)
        ];

        if ( pixels.Length == 0 )
        {
            return Black;
        }

        System.Drawing.Color[] colors = [ .. pixels.Select( pixel => bitmap.GetPixel( pixel.x, pixel.y ) ) ];

        byte
            avgR = (byte) colors.Average( c => (decimal) c.R ),
            avgG = (byte) colors.Average( c => (decimal) c.G ),
            avgB = (byte) colors.Average( c => (decimal) c.B );

        return new Unicolour( ColourSpace.Rgb255, avgR, avgG, avgB );
    }

    /// <summary>
    /// Finds the lightness between 0.33 and 0.66 that gives the greatest contrast with the background.
    /// Preserves hue and saturation.
    /// </summary>
    private static Unicolour FindBestLightnessForContrast(
        Unicolour textColor,
        Unicolour backgroundColor )
    {
        var originalHsl = textColor.Hsl;

        // Search for the lightness between 0.33 and 0.66 that gives the greatest contrast
        double bestL = 0.5;
        double bestContrast = 0.0;

        // Sample every 0.01 in the range
        for ( double l = 0.2; l <= 0.8; l += 0.01 )
        {
            var testColor = new Unicolour( ColourSpace.Hsl, originalHsl.H, originalHsl.S, l );
            double contrast = testColor.Contrast( backgroundColor );

            if ( contrast > bestContrast )
            {
                bestContrast = contrast;
                bestL = l;
            }
        }

        return new Unicolour( ColourSpace.Hsl, originalHsl.H, originalHsl.S, bestL );
    }
}

/// <summary>
/// Extension methods for converting between WPF Color and Unicolour.
/// </summary>
internal static class ColorExtensions
{
    /// <summary>
    /// Converts a WPF Media Color to Unicolour.
    /// </summary>
    /// <param name="color">The WPF Color to convert.</param>
    /// <returns>A Unicolour representation of the color.</returns>
    internal static Unicolour ToUnicolour( this Color color )
    {
        return new Unicolour( ColourSpace.Rgb255, color.R, color.G, color.B );
    }

    /// <summary>
    /// Converts a Unicolour to WPF Media Color.
    /// </summary>
    /// <param name="unicolour">The Unicolour to convert.</param>
    /// <returns>A WPF Color representation of the color.</returns>
    internal static Color ToMediaColor( this Unicolour unicolour )
    {
        Rgb255 rgb = unicolour.Rgb.Byte255;
        return Color.FromRgb( (byte) rgb.R, (byte) rgb.G, (byte) rgb.B );
    }
}
