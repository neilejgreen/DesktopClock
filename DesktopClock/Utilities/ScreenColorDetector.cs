using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace DesktopClock.Utilities;

/// <summary>
/// Utility class for detecting the average color behind a WPF window.
/// </summary>
public static class ScreenColorDetector
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight,
        IntPtr hdcSrc, int nXSrc, int nYSrc, uint dwRop);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

    private const uint SRCCOPY = 0x00CC0020;

    /// <summary>
    /// Captures the average color of the screen area behind the specified window.
    /// </summary>
    /// <param name="window">The WPF window to analyze behind.</param>
    /// <param name="sampleSize">Number of pixels to sample for average calculation (default: 100)</param>
    /// <returns>The average color behind the window, or Color.Black if capture fails.</returns>
    public static System.Windows.Media.Color GetAverageColorBehindWindow(Window window, int sampleSize = 100)
    {
        try
        {
            // Get window bounds in screen coordinates
            var windowBounds = GetWindowBounds(window);
            if (windowBounds.Width <= 0 || windowBounds.Height <= 0)
                return System.Windows.Media.Colors.Black;

            // Capture screen behind window
            using var bitmap = CaptureScreenRegion(windowBounds);
            if (bitmap == null)
                return System.Windows.Media.Colors.Black;

            // Calculate average color from sampled pixels
            return CalculateAverageColor(bitmap, sampleSize);
        }
        catch
        {
            return System.Windows.Media.Colors.Black;
        }
    }

    /// <summary>
    /// Gets the bounds of a WPF window in screen coordinates.
    /// </summary>
    private static Rectangle GetWindowBounds(Window window)
    {
        var source = PresentationSource.FromVisual(window) as HwndSource;
        if (source == null)
            return Rectangle.Empty;

        // Get DPI scaling
        var dpiScale = VisualTreeHelper.GetDpi(window);
        var scaleX = dpiScale.DpiScaleX;
        var scaleY = dpiScale.DpiScaleY;

        // Convert WPF coordinates to screen coordinates
        var left = (int)(window.Left * scaleX);
        var top = (int)(window.Top * scaleY);
        var width = (int)(window.ActualWidth * scaleX);
        var height = (int)(window.ActualHeight * scaleY);

        return new Rectangle(left, top, width, height);
    }

    /// <summary>
    /// Captures a region of the screen using GDI.
    /// </summary>
    private static Bitmap CaptureScreenRegion(Rectangle bounds)
    {
        IntPtr screenDC = IntPtr.Zero;
        IntPtr memDC = IntPtr.Zero;
        IntPtr hBitmap = IntPtr.Zero;
        IntPtr hOld = IntPtr.Zero;

        try
        {
            // Get screen DC
            screenDC = GetDC(IntPtr.Zero);
            if (screenDC == IntPtr.Zero)
                return null;

            // Create compatible DC and bitmap
            memDC = CreateCompatibleDC(screenDC);
            if (memDC == IntPtr.Zero)
                return null;

            hBitmap = CreateCompatibleBitmap(screenDC, bounds.Width, bounds.Height);
            if (hBitmap == IntPtr.Zero)
                return null;

            hOld = SelectObject(memDC, hBitmap);

            // Copy screen content to bitmap
            if (!BitBlt(memDC, 0, 0, bounds.Width, bounds.Height, screenDC, bounds.X, bounds.Y, SRCCOPY))
                return null;

            // Create managed bitmap from HBITMAP
            return Image.FromHbitmap(hBitmap);
        }
        finally
        {
            // Cleanup
            if (hOld != IntPtr.Zero)
                SelectObject(memDC, hOld);
            if (hBitmap != IntPtr.Zero)
                DeleteObject(hBitmap);
            if (memDC != IntPtr.Zero)
                DeleteDC(memDC);
            if (screenDC != IntPtr.Zero)
                ReleaseDC(IntPtr.Zero, screenDC);
        }
    }

    /// <summary>
    /// Calculates the average color from a bitmap by sampling pixels.
    /// </summary>
    private static System.Windows.Media.Color CalculateAverageColor(Bitmap bitmap, int sampleSize)
    {
        var random = new Random();
        long totalR = 0, totalG = 0, totalB = 0;
        int validSamples = 0;

        // Sample random pixels for efficiency
        for (int i = 0; i < sampleSize; i++)
        {
            var x = random.Next(0, bitmap.Width);
            var y = random.Next(0, bitmap.Height);

            var pixel = bitmap.GetPixel(x, y);
            totalR += pixel.R;
            totalG += pixel.G;
            totalB += pixel.B;
            validSamples++;
        }

        if (validSamples == 0)
            return System.Windows.Media.Colors.Black;

        // Calculate averages
        var avgR = (byte)(totalR / validSamples);
        var avgG = (byte)(totalG / validSamples);
        var avgB = (byte)(totalB / validSamples);

        return System.Windows.Media.Color.FromRgb(avgR, avgG, avgB);
    }

    /// <summary>
    /// Determines if a color is considered "dark" based on its luminance.
    /// </summary>
    /// <param name="color">The color to analyze.</param>
    /// <returns>True if the color is dark, false if light.</returns>
    public static bool IsColorDark(System.Windows.Media.Color color)
    {
        // Calculate luminance using standard formula
        var luminance = ((0.299 * color.R) + (0.587 * color.G) + (0.114 * color.B)) / 255.0;
        return luminance < 0.5;
    }

    /// <summary>
    /// Gets the optimal text color by adjusting the lightness of the current text color
    /// to ensure good contrast with the background while preserving hue.
    /// </summary>
    /// <param name="currentTextColor">The current text color to adjust.</param>
    /// <param name="backgroundColor">The background color to contrast against.</param>
    /// <returns>The adjusted text color with optimal contrast.</returns>
    public static System.Windows.Media.Color GetOptimalTextColorWithHue(
        System.Windows.Media.Color currentTextColor,
        System.Windows.Media.Color backgroundColor)
    {
        // Convert colors to HSL
        var textHsl = RgbToHsl(currentTextColor);
        var bgHsl = RgbToHsl(backgroundColor);

        // If background is dark, make text light; if background is light, make text dark
        // Keep the same hue and saturation from the original text color
        var targetLightness = IsColorDark(backgroundColor) ? 0.9 : 0.1;

        // Adjust the lightness while preserving hue and saturation
        var adjustedHsl = new HslColor(textHsl.H, textHsl.S, targetLightness);

        return HslToRgb(adjustedHsl);
    }

    /// <summary>
    /// Represents a color in HSL (Hue, Saturation, Lightness) color space.
    /// </summary>
    private struct HslColor
    {
        public double H { get; }  // Hue (0-360)
        public double S { get; }  // Saturation (0-1)
        public double L { get; }  // Lightness (0-1)

        public HslColor(double h, double s, double l)
        {
            H = h;
            S = s;
            L = l;
        }
    }

    /// <summary>
    /// Converts RGB color to HSL color space.
    /// </summary>
    private static HslColor RgbToHsl(System.Windows.Media.Color rgb)
    {
        var r = rgb.R / 255.0;
        var g = rgb.G / 255.0;
        var b = rgb.B / 255.0;

        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var delta = max - min;

        var h = 0.0;
        var s = 0.0;
        var l = (max + min) / 2.0;

        if (delta != 0)
        {
            s = l > 0.5 ? delta / (2.0 - max - min) : delta / (max + min);

            if (max == r)
                h = ((g - b) / delta) + (g < b ? 6 : 0);
            else if (max == g)
                h = ((b - r) / delta) + 2;
            else if (max == b)
                h = ((r - g) / delta) + 4;

            h /= 6.0;
        }

        return new HslColor(h * 360.0, s, l);
    }

    /// <summary>
    /// Converts HSL color to RGB color space.
    /// </summary>
    private static System.Windows.Media.Color HslToRgb(HslColor hsl)
    {
        var h = hsl.H / 360.0;
        var s = hsl.S;
        var l = hsl.L;

        if (s == 0)
        {
            // Achromatic (gray)
            var gray = (byte)Math.Round(l * 255);
            return System.Windows.Media.Color.FromRgb(gray, gray, gray);
        }

        var c = (1.0 - Math.Abs((2.0 * l) - 1.0)) * s;
        var hSix = h * 6.0;
        var x = c * (1.0 - Math.Abs((hSix % 2.0) - 1.0));
        var m = l - (c / 2.0);

        double r1, g1, b1;

        if (h < 1.0 / 6.0)
        {
            r1 = c;
            g1 = x;
            b1 = 0;
        }
        else if (h < 2.0 / 6.0)
        {
            r1 = x;
            g1 = c;
            b1 = 0;
        }
        else if (h < 3.0 / 6.0)
        {
            r1 = 0;
            g1 = c;
            b1 = x;
        }
        else if (h < 4.0 / 6.0)
        {
            r1 = 0;
            g1 = x;
            b1 = c;
        }
        else if (h < 5.0 / 6.0)
        {
            r1 = x;
            g1 = 0;
            b1 = c;
        }
        else
        {
            r1 = c;
            g1 = 0;
            b1 = x;
        }

        var r = (byte)Math.Round((r1 + m) * 255);
        var g = (byte)Math.Round((g1 + m) * 255);
        var b = (byte)Math.Round((b1 + m) * 255);

        return System.Windows.Media.Color.FromRgb(r, g, b);
    }
}
