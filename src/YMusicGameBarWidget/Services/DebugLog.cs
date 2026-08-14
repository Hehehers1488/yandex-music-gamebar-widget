using System;
using System.IO;
using Windows.Storage;

namespace YMusicGameBarWidget.Services
{
    /// <summary>Writes diagnostics to the app's LocalState folder (debug.log).</summary>
    public static class DebugLog
    {
        private static readonly object Lock = new object();

        public static void Write(string message)
        {
            try
            {
                var file = Path.Combine(ApplicationData.Current.LocalFolder.Path, "debug.log");
                lock (Lock)
                {
                    File.AppendAllText(file, $"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
                }
            }
            catch { }
        }
    }
}
