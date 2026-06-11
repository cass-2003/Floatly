using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace DeskLite.Services;

public sealed class GlobalHotkeyService : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const int IdToggle = 1;
    private const int IdQuickTodo = 2;

    private readonly Window _window;
    private readonly Action _onToggleWindow;
    private readonly Action _onQuickTodo;
    private HwndSource? _source;
    private bool _registered;
    private string _showHideCombo = HotkeyComboHelper.DefaultShowHide;
    private string _quickTodoCombo = HotkeyComboHelper.DefaultQuickTodo;

    public GlobalHotkeyService(Window window, Action onToggleWindow, Action onQuickTodo)
    {
        _window = window;
        _onToggleWindow = onToggleWindow;
        _onQuickTodo = onQuickTodo;
    }

    public void Configure(string? showHide, string? quickTodo)
    {
        _showHideCombo = HotkeyComboHelper.Sanitize(showHide, HotkeyComboHelper.DefaultShowHide);
        _quickTodoCombo = HotkeyComboHelper.Sanitize(quickTodo, HotkeyComboHelper.DefaultQuickTodo);

        if (HotkeyComboHelper.Conflicts(_showHideCombo, _quickTodoCombo))
        {
            _quickTodoCombo = HotkeyComboHelper.DefaultQuickTodo;
        }
    }

    public void Register()
    {
        Unregister();

        var helper = new WindowInteropHelper(_window);
        if (helper.Handle == IntPtr.Zero)
        {
            return;
        }

        _source = HwndSource.FromHwnd(helper.Handle);
        _source?.AddHook(WndProc);

        var toggleOk = TryRegister(helper.Handle, IdToggle, _showHideCombo, HotkeyComboHelper.DefaultShowHide);
        var todoOk = TryRegister(helper.Handle, IdQuickTodo, _quickTodoCombo, HotkeyComboHelper.DefaultQuickTodo);
        _registered = toggleOk || todoOk;
    }

    public void Unregister()
    {
        if (!_registered)
        {
            return;
        }

        var helper = new WindowInteropHelper(_window);
        if (helper.Handle != IntPtr.Zero)
        {
            UnregisterHotKey(helper.Handle, IdToggle);
            UnregisterHotKey(helper.Handle, IdQuickTodo);
        }

        _source?.RemoveHook(WndProc);
        _registered = false;
    }

    private static bool TryRegister(IntPtr handle, int id, string comboText, string fallbackText)
    {
        if (!HotkeyComboHelper.TryParse(comboText, out var combo) &&
            !HotkeyComboHelper.TryParse(fallbackText, out combo))
        {
            return false;
        }

        return RegisterHotKey(handle, id, combo.Modifiers, combo.VirtualKey);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WmHotkey)
        {
            return IntPtr.Zero;
        }

        switch (wParam.ToInt32())
        {
            case IdToggle:
                _onToggleWindow();
                handled = true;
                break;
            case IdQuickTodo:
                _onQuickTodo();
                handled = true;
                break;
        }

        return IntPtr.Zero;
    }

    public void Dispose() => Unregister();

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
