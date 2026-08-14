using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace YMusicGameBarWidget.Services
{
    public enum MediaCommand
    {
        PlayPause = 14,
        NextTrack = 11,
        PreviousTrack = 12
    }

    /// <summary>
    /// Fallback transport control: sends WM_APPCOMMAND to the Yandex Music window
    /// when the SMTC session does not respond.
    /// </summary>
    public static class YandexWindowControl
    {
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private const uint WM_APPCOMMAND = 0x0319;

        public static bool Send(MediaCommand command)
        {
            try
            {
                var windows = new List<IntPtr>();
                EnumWindows((hWnd, lParam) =>
                {
                    windows.Add(hWnd);
                    return true;
                }, IntPtr.Zero);

                var yandexPids = new HashSet<uint>(
                    Process.GetProcesses()
                        .Where(p =>
                        {
                            try
                            {
                                var name = p.ProcessName;
                                bool isYandex = name.IndexOf("yandex", StringComparison.OrdinalIgnoreCase) >= 0
                                            || name.IndexOf("яндекс", StringComparison.OrdinalIgnoreCase) >= 0;
                                bool isMusic = name.IndexOf("music", StringComparison.OrdinalIgnoreCase) >= 0
                                           || name.IndexOf("музыка", StringComparison.OrdinalIgnoreCase) >= 0;
                                return isYandex && isMusic;
                            }
                            catch { return false; }
                        })
                        .Select(p => (uint)p.Id));

                if (yandexPids.Count == 0) return false;

                foreach (var hWnd in windows)
                {
                    if (!IsWindowVisible(hWnd)) continue;
                    GetWindowThreadProcessId(hWnd, out uint pid);
                    if (!yandexPids.Contains(pid)) continue;
                    if (PostMessage(hWnd, WM_APPCOMMAND, IntPtr.Zero, new IntPtr((int)command)))
                        return true;
                }
            }
            catch { }
            return false;
        }
    }
}
