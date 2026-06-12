using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace DeskLite.Services;

public static class WindowHelper
{
    private const int GwlExstyle = -20;
    private const int WsExTransparent = 0x00000020;
    private const int WmNchitTest = 0x0084;
    private const int HtClient = 1;
    private const int HtLeft = 10;
    private const int HtRight = 11;
    private const int HtTop = 12;
    private const int HtTopLeft = 13;
    private const int HtTopRight = 14;
    private const int HtBottom = 15;
    private const int HtBottomLeft = 16;
    private const int HtBottomRight = 17;
    private const int AccentEnableBlurbehind = 3;
    private const int AccentEnableAcrylicBlurbehind = 4;
    private const int WcaAccentPolicy = 19;

    private static readonly HashSet<IntPtr> ResizeHookedWindows = [];

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

    [DllImport("user32.dll")]
    private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

    public static void SetClickThrough(Window window, bool enabled)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        var style = GetWindowLong(hwnd, GwlExstyle);
        if (enabled)
        {
            SetWindowLong(hwnd, GwlExstyle, style | WsExTransparent);
        }
        else
        {
            SetWindowLong(hwnd, GwlExstyle, style & ~WsExTransparent);
        }
    }

    public static void EnableGlassBackdrop(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        if (!TryApplyAccent(hwnd, AccentEnableAcrylicBlurbehind, 0x660A1B31))
        {
            TryApplyAccent(hwnd, AccentEnableBlurbehind, 0x220A1B31);
        }
    }

    public static void EnableBorderlessResize(Window window, int grip = 8)
    {
        void AttachHook()
        {
            var helper = new WindowInteropHelper(window);
            if (helper.Handle == IntPtr.Zero || !ResizeHookedWindows.Add(helper.Handle))
            {
                return;
            }

            var source = HwndSource.FromHwnd(helper.Handle);
            if (source is null)
            {
                ResizeHookedWindows.Remove(helper.Handle);
                return;
            }

            source.AddHook((IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
            {
                if (msg != WmNchitTest)
                {
                    return IntPtr.Zero;
                }

                var x = (short)(lParam.ToInt64() & 0xFFFF);
                var y = (short)((lParam.ToInt64() >> 16) & 0xFFFF);
                var point = window.PointFromScreen(new System.Windows.Point(x, y));
                var w = window.ActualWidth;
                var h = window.ActualHeight;

                if (w <= 0 || h <= 0 || point.X < 0 || point.Y < 0 || point.X > w || point.Y > h)
                {
                    return IntPtr.Zero;
                }

                var dpi = VisualTreeHelper.GetDpi(window);
                var gripX = grip * dpi.DpiScaleX;
                var gripY = grip * dpi.DpiScaleY;

                var onLeft = point.X <= gripX;
                var onRight = point.X >= w - gripX;
                var onTop = point.Y <= gripY;
                var onBottom = point.Y >= h - gripY;

                handled = true;
                if (onTop && onLeft)
                {
                    return (IntPtr)HtTopLeft;
                }

                if (onTop && onRight)
                {
                    return (IntPtr)HtTopRight;
                }

                if (onBottom && onLeft)
                {
                    return (IntPtr)HtBottomLeft;
                }

                if (onBottom && onRight)
                {
                    return (IntPtr)HtBottomRight;
                }

                if (onLeft)
                {
                    return (IntPtr)HtLeft;
                }

                if (onRight)
                {
                    return (IntPtr)HtRight;
                }

                if (onTop)
                {
                    return (IntPtr)HtTop;
                }

                if (onBottom)
                {
                    return (IntPtr)HtBottom;
                }

                handled = false;
                return (IntPtr)HtClient;
            });
        }

        var helper = new WindowInteropHelper(window);
        if (helper.Handle != IntPtr.Zero)
        {
            AttachHook();
        }
        else
        {
            window.SourceInitialized += (_, _) => AttachHook();
        }
    }

    private static bool TryApplyAccent(IntPtr hwnd, int state, uint tint)
    {
        var accent = new AccentPolicy
        {
            AccentState = state,
            AccentFlags = 2,
            GradientColor = tint
        };

        var size = Marshal.SizeOf<AccentPolicy>();
        var accentPtr = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(accent, accentPtr, false);
            var data = new WindowCompositionAttributeData
            {
                Attribute = WcaAccentPolicy,
                SizeOfData = size,
                Data = accentPtr
            };

            return SetWindowCompositionAttribute(hwnd, ref data) != 0;
        }
        finally
        {
            Marshal.FreeHGlobal(accentPtr);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AccentPolicy
    {
        public int AccentState;
        public int AccentFlags;
        public uint GradientColor;
        public int AnimationId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowCompositionAttributeData
    {
        public int Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }
}
