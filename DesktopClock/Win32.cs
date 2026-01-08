using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace DesktopClock;

internal static class Win32
{
    public const int GWL_EXSTYLE = -20;

    // Prefer long for style math (works on 32/64 easily)
    public const long WS_EX_TRANSPARENT = 0x00000020L;
    public const long WS_EX_LAYERED = 0x00080000L;
    public const long WS_EX_TOOLWINDOW = 0x00000080L;
    public const long WS_EX_TOPMOST = 0x00000008L;

    public const uint SWP_NOSIZE = 0x0001;
    public const uint SWP_NOMOVE = 0x0002;
    public const uint SWP_NOZORDER = 0x0004;
    public const uint SWP_FRAMECHANGED = 0x0020;
    public const uint SWP_NOACTIVATE = 0x0010;

    private static IntPtr TopMost { get; } = new( -1 );

    public static void MakeTopMost( Window window )
    {
        IntPtr hWnd = new WindowInteropHelper( window ).Handle;
        SetWindowPos(
            hWnd,
            TopMost,
            0,
            0,
            0,
            0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE );
    }


    public static void UpdateWindow( Window window, bool isClickable )
    {
        // Base styles always applied to the window
        const long WINDOW_STYLE_BASE = WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_TOPMOST;

        try
        {

            IntPtr hwnd = new WindowInteropHelper( window ).Handle;
            long exStyle = GetWindowLongPtr( hwnd, GWL_EXSTYLE ).ToInt64();

            // Ensure base styles are always set
            long newStyle = exStyle | WINDOW_STYLE_BASE | WS_EX_TRANSPARENT;

            // Remove transparency if clickable
            if ( isClickable )
            {
                newStyle &= ~WS_EX_TRANSPARENT;
            }

            SetWindowLongPtr( hwnd, GWL_EXSTYLE, new IntPtr( newStyle ) );
            MakeTopMost( window );
        }
        catch
        {
            // Ignore failures.
        }
    }

    private static IntPtr GetWindowLongPtr( IntPtr hWnd, int nIndex )
        => IntPtr.Size == 8
            ? GetWindowLongPtr64( hWnd, nIndex )
            : new IntPtr( GetWindowLong32( hWnd, nIndex ) );

    private static IntPtr SetWindowLongPtr( IntPtr hWnd, int nIndex, IntPtr dwNewLong )
        => IntPtr.Size == 8
            ? SetWindowLongPtr64( hWnd, nIndex, dwNewLong )
            : new IntPtr( SetWindowLong32( hWnd, nIndex, dwNewLong.ToInt32() ) );

    [DllImport( "user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true )]
    private static extern int GetWindowLong32( IntPtr hWnd, int nIndex );

    [DllImport( "user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true )]
    private static extern IntPtr GetWindowLongPtr64( IntPtr hWnd, int nIndex );

    [DllImport( "user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true )]
    private static extern int SetWindowLong32( IntPtr hWnd, int nIndex, int dwNewLong );

    [DllImport( "user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true )]
    private static extern IntPtr SetWindowLongPtr64( IntPtr hWnd, int nIndex, IntPtr dwNewLong );

    [DllImport( "user32.dll", SetLastError = true )]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags );

}
