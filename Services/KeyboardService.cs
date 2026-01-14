using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace Pie.Services
{
    public class KeyboardService
    {
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern short VkKeyScan(char ch);

        private const uint KEYEVENTF_KEYDOWN = 0x0000;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        private const byte VK_CONTROL = 0x11;
        private const byte VK_SHIFT = 0x10;
        private const byte VK_ALT = 0x12;
        private const byte VK_LWIN = 0x5B;

        public void SendKeyboardShortcut(string shortcut)
        {
            if (string.IsNullOrEmpty(shortcut)) return;

            var parts = shortcut.Split('+').Select(p => p.Trim()).ToList();
            var modifiers = new List<byte>();
            byte? mainKey = null;

            foreach (var part in parts)
            {
                var upperPart = part.ToUpperInvariant();
                switch (upperPart)
                {
                    case "CTRL":
                    case "CONTROL":
                        modifiers.Add(VK_CONTROL);
                        break;
                    case "SHIFT":
                        modifiers.Add(VK_SHIFT);
                        break;
                    case "ALT":
                        modifiers.Add(VK_ALT);
                        break;
                    case "WIN":
                    case "WINDOWS":
                        modifiers.Add(VK_LWIN);
                        break;
                    default:
                        mainKey = GetVirtualKeyCode(upperPart);
                        break;
                }
            }

            if (mainKey == null) return;

            // Press modifiers
            foreach (var mod in modifiers)
            {
                keybd_event(mod, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
            }

            // Press and release main key
            keybd_event(mainKey.Value, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
            keybd_event(mainKey.Value, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

            // Release modifiers in reverse order
            modifiers.Reverse();
            foreach (var mod in modifiers)
            {
                keybd_event(mod, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            }
        }

        private byte? GetVirtualKeyCode(string keyName)
        {
            // Function keys
            if (keyName.StartsWith("F") && int.TryParse(keyName.Substring(1), out int fNum) && fNum >= 1 && fNum <= 24)
            {
                return (byte)(0x70 + fNum - 1); // VK_F1 = 0x70
            }

            // Single character
            if (keyName.Length == 1)
            {
                char c = keyName[0];
                if (char.IsLetterOrDigit(c))
                {
                    return (byte)char.ToUpperInvariant(c);
                }
            }

            // Special keys
            return keyName switch
            {
                "ENTER" or "RETURN" => 0x0D,
                "TAB" => 0x09,
                "SPACE" => 0x20,
                "BACKSPACE" or "BACK" => 0x08,
                "ESCAPE" or "ESC" => 0x1B,
                "DELETE" or "DEL" => 0x2E,
                "INSERT" or "INS" => 0x2D,
                "HOME" => 0x24,
                "END" => 0x23,
                "PAGEUP" or "PGUP" => 0x21,
                "PAGEDOWN" or "PGDN" => 0x22,
                "UP" => 0x26,
                "DOWN" => 0x28,
                "LEFT" => 0x25,
                "RIGHT" => 0x27,
                "PRINTSCREEN" or "PRTSC" => 0x2C,
                "SCROLLLOCK" => 0x91,
                "PAUSE" => 0x13,
                "NUMLOCK" => 0x90,
                "CAPSLOCK" => 0x14,
                _ => null
            };
        }

        public static (Key key, ModifierKeys modifiers) ParseHotkey(string hotkeyString)
        {
            var parts = hotkeyString.Split('+').Select(p => p.Trim()).ToList();
            ModifierKeys modifiers = ModifierKeys.None;
            Key key = Key.None;

            foreach (var part in parts)
            {
                var upperPart = part.ToUpperInvariant();
                switch (upperPart)
                {
                    case "CTRL":
                    case "CONTROL":
                        modifiers |= ModifierKeys.Control;
                        break;
                    case "SHIFT":
                        modifiers |= ModifierKeys.Shift;
                        break;
                    case "ALT":
                        modifiers |= ModifierKeys.Alt;
                        break;
                    case "WIN":
                    case "WINDOWS":
                        modifiers |= ModifierKeys.Windows;
                        break;
                    default:
                        if (Enum.TryParse<Key>(part, true, out var parsedKey))
                        {
                            key = parsedKey;
                        }
                        break;
                }
            }

            return (key, modifiers);
        }
    }
}
