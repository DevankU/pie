using System;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace Pie.Services
{
    public class MouseHookService : IDisposable
    {
        private const int WH_MOUSE_LL = 14;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        private LowLevelMouseProc? _proc;
        private IntPtr _hookId = IntPtr.Zero;
        private DispatcherTimer? _longPressTimer;
        private DateTime _rightButtonDownTime;
        private bool _isRightButtonDown;
        private bool _longPressTriggered;

        public double LongPressThresholdMs { get; set; } = 2000; // 2 seconds
        public bool IsEnabled { get; set; } = true;

        public event EventHandler? LongRightClickDetected;
        public event EventHandler? RightClickReleased;

        public void Start()
        {
            _proc = HookCallback;
            using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            if (curModule?.ModuleName != null)
            {
                _hookId = SetWindowsHookEx(WH_MOUSE_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
            }

            _longPressTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            _longPressTimer.Tick += LongPressTimer_Tick;
        }

        private void LongPressTimer_Tick(object? sender, EventArgs e)
        {
            if (!_isRightButtonDown || _longPressTriggered) return;

            var elapsed = (DateTime.Now - _rightButtonDownTime).TotalMilliseconds;
            if (elapsed >= LongPressThresholdMs)
            {
                _longPressTriggered = true;
                _longPressTimer?.Stop();
                LongRightClickDetected?.Invoke(this, EventArgs.Empty);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && IsEnabled)
            {
                int msg = wParam.ToInt32();

                if (msg == WM_RBUTTONDOWN)
                {
                    _isRightButtonDown = true;
                    _longPressTriggered = false;
                    _rightButtonDownTime = DateTime.Now;
                    _longPressTimer?.Start();
                }
                else if (msg == WM_RBUTTONUP)
                {
                    _isRightButtonDown = false;
                    _longPressTimer?.Stop();

                    if (_longPressTriggered)
                    {
                        RightClickReleased?.Invoke(this, EventArgs.Empty);
                        // Consume the right click if we triggered long press
                        return (IntPtr)1;
                    }
                }
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        public void Stop()
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
            _longPressTimer?.Stop();
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
