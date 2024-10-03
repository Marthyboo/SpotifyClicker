using System;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Autoclicker.clicker;

namespace Autoclicker.hooks
{
    public class MouseHook
    {
        #region WinAPI
        [DllImport("user32", EntryPoint = "SetWindowsHookExA")]
        private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, ref Msllhookstruct lParam);

        [DllImport("user32")]
        private static extern bool UnhookWindowsHookEx(IntPtr hHook);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private delegate IntPtr HookProc(int nCode, IntPtr wParam, ref Msllhookstruct lParam);
        private IntPtr _mouseHook;
        public const int HcAction = 0;
        private const int WhMouseLl = 14;

        [StructLayout(LayoutKind.Sequential)]
        public struct Msllhookstruct
        {
            public Point pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private enum MouseMessages
        {
            WmLbuttondown = 0x0201,
            WmLbuttonup = 0x0202,
            WmMousemove = 0x0200,
            WmRbuttondown = 0x0204,
            WmRbuttonup = 0x0205,
        }
        #endregion

        #region Mouse Events
        private HookProc _mouseHookProcedure;
        public delegate void MouseMoveEventHandler(Point ptLocat);
        public delegate void MouseLeftDownEventHandler(Point ptLocat);
        public delegate void MouseLeftUpEventHandler(Point ptLocat);
        public delegate void MouseRightDownEventHandler(Point ptLocat);
        public delegate void MouseRightUpEventHandler(Point ptLocat);

        public event MouseMoveEventHandler MouseMove;
        public event MouseLeftDownEventHandler MouseLeftDown;
        public event MouseLeftUpEventHandler MouseLeftUp;
        public event MouseRightDownEventHandler MouseRightDown;
        public event MouseRightUpEventHandler MouseRightUp;
        #endregion

        #region LowLevelKeyboardProc callback function
        public static class HookStructData
        {
            public static Msllhookstruct Msllhookstruct;
        }

        public static uint Flag;

        private IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, ref Msllhookstruct lParam)
        {
            if (nCode < HcAction)
                return CallNextHookEx(_mouseHook, nCode, wParam, ref lParam);

            Msllhookstruct hookStruct = lParam;
            Flag = hookStruct.flags;

            bool isMouseStatusEnabled = Clicker.MainWindow.Dispatcher.Invoke(() =>
                Clicker.MainWindow.ToggleMouseStatus.IsChecked != null && Clicker.MainWindow.ToggleMouseStatus.IsChecked.Value);

            if (isMouseStatusEnabled)
            {
                Clicker.MainWindow.Dispatcher.Invoke(() =>
                {
                    Clicker.MainWindow.ScreenXAxisText.Content = hookStruct.pt.X;
                    Clicker.MainWindow.ScreenYAxisText.Content = hookStruct.pt.Y;
                    Clicker.MainWindow.SimulatedClickStatusText.Content = hookStruct.flags == 1 ? "TRUE" : "FALSE";
                });
            }

            switch ((MouseMessages)wParam.ToInt32())
            {
                case MouseMessages.WmMousemove:
                    MouseMove?.Invoke(lParam.pt);
                    break;
                case MouseMessages.WmLbuttondown:
                    MouseLeftDown?.Invoke(lParam.pt);
                    break;
                case MouseMessages.WmLbuttonup:
                    MouseLeftUp?.Invoke(lParam.pt);
                    break;
                case MouseMessages.WmRbuttondown:
                    MouseRightDown?.Invoke(lParam.pt);
                    break;
                case MouseMessages.WmRbuttonup:
                    MouseRightUp?.Invoke(lParam.pt);
                    break;
            }

            return CallNextHookEx(_mouseHook, nCode, wParam, ref lParam);
        }

        public static async Task DisplayScreenCoordsAndMouseFlag(Msllhookstruct hookStruct)
        {
            await Task.Run(() =>
            {
                Clicker.MainWindow.Dispatcher.Invoke(() =>
                {
                    Clicker.MainWindow.ScreenXAxisText.Content = hookStruct.pt.X;
                    Clicker.MainWindow.ScreenYAxisText.Content = hookStruct.pt.Y;
                    Clicker.MainWindow.SimulatedClickStatusText.Content = hookStruct.flags == 1 ? "TRUE" : "FALSE";
                });
            });
        }

        #endregion

        public void InstallMouseHook()
        {
            _mouseHookProcedure = LowLevelKeyboardProc;
            IntPtr hInstance = GetModuleHandle(Assembly.GetExecutingAssembly().GetModules()[0].Name);
            _mouseHook = SetWindowsHookEx(WhMouseLl, _mouseHookProcedure, hInstance, 0);
        }

        public void UninstallMouseHook()
        {
            UnhookWindowsHookEx(_mouseHook);
        }
    }
}
