using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Pie.Services
{
    public class HotkeyService : IDisposable
    {
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 9000;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private HwndSource? _source;
        private IntPtr _windowHandle;
        private bool _isRegistered;

        public event EventHandler? HotkeyPressed;

        public bool Register(Window window, Key key, ModifierKeys modifiers)
        {
            var helper = new WindowInteropHelper(window);
            _windowHandle = helper.EnsureHandle();
            _source = HwndSource.FromHwnd(_windowHandle);
            _source?.AddHook(HwndHook);

            return RegisterHotkey(key, modifiers);
        }

        public bool RegisterHotkey(Key key, ModifierKeys modifiers)
        {
            if (_isRegistered)
            {
                UnregisterHotKey(_windowHandle, HOTKEY_ID);
                _isRegistered = false;
            }

            uint mod = 0;
            if (modifiers.HasFlag(ModifierKeys.Alt)) mod |= 0x0001;
            if (modifiers.HasFlag(ModifierKeys.Control)) mod |= 0x0002;
            if (modifiers.HasFlag(ModifierKeys.Shift)) mod |= 0x0004;
            if (modifiers.HasFlag(ModifierKeys.Windows)) mod |= 0x0008;

            uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);
            _isRegistered = RegisterHotKey(_windowHandle, HOTKEY_ID, mod, vk);
            return _isRegistered;
        }

        public void Unregister()
        {
            if (_isRegistered)
            {
                UnregisterHotKey(_windowHandle, HOTKEY_ID);
                _isRegistered = false;
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                HotkeyPressed?.Invoke(this, EventArgs.Empty);
                handled = true;
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            Unregister();
            _source?.RemoveHook(HwndHook);
            _source?.Dispose();
        }
    }
}
