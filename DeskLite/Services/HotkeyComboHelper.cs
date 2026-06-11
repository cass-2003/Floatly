using System.Windows.Input;

namespace DeskLite.Services;

public readonly record struct HotkeyCombo(uint Modifiers, uint VirtualKey, string Display);

public static class HotkeyComboHelper
{
    public const string DefaultShowHide = "Ctrl+Shift+D";
    public const string DefaultQuickTodo = "Ctrl+Shift+N";

    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModWin = 0x0008;

    private static readonly HashSet<Key> IgnoredKeys =
    [
        Key.LeftCtrl, Key.RightCtrl,
        Key.LeftAlt, Key.RightAlt,
        Key.LeftShift, Key.RightShift,
        Key.LWin, Key.RWin,
        Key.System, Key.ImeProcessed
    ];

    public static string Sanitize(string? text, string fallback)
    {
        return TryParse(text, out var combo) ? combo.Display : Sanitize(fallback, DefaultShowHide);
    }

    public static bool TryParse(string? text, out HotkeyCombo combo)
    {
        combo = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var parts = text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        uint mods = 0;
        var keyIndex = parts.Length - 1;
        for (var i = 0; i < keyIndex; i++)
        {
            if (!TryParseModifier(parts[i], out var mod))
            {
                return false;
            }

            mods |= mod;
        }

        if (mods == 0)
        {
            return false;
        }

        if (!TryParseKeyToken(parts[keyIndex], out var key))
        {
            return false;
        }

        if (IgnoredKeys.Contains(key))
        {
            return false;
        }

        var vk = (uint)KeyInterop.VirtualKeyFromKey(key);
        if (vk == 0)
        {
            return false;
        }

        combo = new HotkeyCombo(mods, vk, Format(key, ToModifierKeys(mods)));
        return true;
    }

    public static string Format(Key key, ModifierKeys modifiers)
    {
        var parts = new List<string>();
        if (modifiers.HasFlag(ModifierKeys.Control))
        {
            parts.Add("Ctrl");
        }

        if (modifiers.HasFlag(ModifierKeys.Alt))
        {
            parts.Add("Alt");
        }

        if (modifiers.HasFlag(ModifierKeys.Shift))
        {
            parts.Add("Shift");
        }

        if (modifiers.HasFlag(ModifierKeys.Windows))
        {
            parts.Add("Win");
        }

        parts.Add(NormalizeKeyName(key));
        return string.Join('+', parts);
    }

    public static bool Conflicts(string? left, string? right)
    {
        if (!TryParse(left, out var a) || !TryParse(right, out var b))
        {
            return false;
        }

        return a.Modifiers == b.Modifiers && a.VirtualKey == b.VirtualKey;
    }

    public static bool TryFormatFromInput(Key key, ModifierKeys modifiers, out string display)
    {
        display = string.Empty;
        if (IgnoredKeys.Contains(key))
        {
            return false;
        }

        var effectiveMods = modifiers & (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift | ModifierKeys.Windows);
        if (effectiveMods == ModifierKeys.None)
        {
            return false;
        }

        display = Format(key, effectiveMods);
        return TryParse(display, out _);
    }

    private static bool TryParseModifier(string token, out uint modifier)
    {
        modifier = token.Trim().ToLowerInvariant() switch
        {
            "ctrl" or "control" => ModControl,
            "alt" => ModAlt,
            "shift" => ModShift,
            "win" or "windows" => ModWin,
            _ => 0
        };

        return modifier != 0;
    }

    private static bool TryParseKeyToken(string token, out Key key)
    {
        if (Enum.TryParse<Key>(token, ignoreCase: true, out key))
        {
            return true;
        }

        return token.ToUpperInvariant() switch
        {
            "0" => Assign(Key.D0, out key),
            "1" => Assign(Key.D1, out key),
            "2" => Assign(Key.D2, out key),
            "3" => Assign(Key.D3, out key),
            "4" => Assign(Key.D4, out key),
            "5" => Assign(Key.D5, out key),
            "6" => Assign(Key.D6, out key),
            "7" => Assign(Key.D7, out key),
            "8" => Assign(Key.D8, out key),
            "9" => Assign(Key.D9, out key),
            _ => false
        };
    }

    private static bool Assign(Key value, out Key key)
    {
        key = value;
        return true;
    }

    private static ModifierKeys ToModifierKeys(uint mods)
    {
        var result = ModifierKeys.None;
        if ((mods & ModControl) != 0)
        {
            result |= ModifierKeys.Control;
        }

        if ((mods & ModAlt) != 0)
        {
            result |= ModifierKeys.Alt;
        }

        if ((mods & ModShift) != 0)
        {
            result |= ModifierKeys.Shift;
        }

        if ((mods & ModWin) != 0)
        {
            result |= ModifierKeys.Windows;
        }

        return result;
    }

    private static string NormalizeKeyName(Key key) => key switch
    {
        Key.OemPlus => "+",
        Key.OemMinus => "-",
        Key.OemComma => ",",
        Key.OemPeriod => ".",
        >= Key.D0 and <= Key.D9 => ((int)key - (int)Key.D0).ToString(),
        >= Key.A and <= Key.Z => key.ToString(),
        >= Key.F1 and <= Key.F24 => key.ToString(),
        _ => key.ToString()
    };
}
