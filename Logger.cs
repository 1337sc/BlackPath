using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace tgBot
{
    /// <summary>
    /// A maintenance class for logging anything useful for debugging.
    /// </summary>
    public static class Logger
    {
        private const string LogPath = "./userdata/archived/";
        private const string LogFormat = ".txt";
        private static readonly string LogId = DateTime.Now
            .ToString()
            .Replace(' ', '_')
            .Replace(':', '_');
        private static readonly SemaphoreSlim LogSemaphore = new SemaphoreSlim(1, 1);
        public static async Task Log(string message)
        {
            await Log(message, $"{LogPath}{LogId}{LogFormat}");
        }
        public static async Task Log(string message, string path)
        {
            await LogSemaphore.WaitAsync();
            var pathInfo = new DirectoryInfo(LogPath);
            if (!pathInfo.Exists)
            {
                pathInfo.Create();
            }
            using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate))
            {
                _ = fs.Seek(0, SeekOrigin.End);
                await fs.WriteAsync(Encoding.UTF8.GetBytes(message + $"\t at {DateTime.Now}\n"));
            }
            LogSemaphore.Release();
        }
    }
}
