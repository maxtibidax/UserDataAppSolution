using System;
using System.IO;
using UserDataLibrary.Services; // Для FileConstants

namespace WpfUserDataApp.Utils
{
    public static class ErrorLogger
    {
        private static readonly string _logFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            FileConstants.LogFileName);
        private static readonly object _lock = new object(); // Для потокобезопасности

        public static void LogError(Exception ex, string context = "")
        {
            lock (_lock)
            {
                try
                {
                    using (StreamWriter writer = File.AppendText(_logFilePath))
                    {
                        writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {(string.IsNullOrEmpty(context) ? "" : $"Context: {context} ")}Error: {ex.GetType().Name}");
                        writer.WriteLine($"Message: {ex.Message}");
                        writer.WriteLine($"StackTrace: {ex.StackTrace}");
                        if (ex.InnerException != null)
                        {
                            writer.WriteLine($"--- Inner Exception ---");
                            writer.WriteLine($"Type: {ex.InnerException.GetType().Name}");
                            writer.WriteLine($"Message: {ex.InnerException.Message}");
                            writer.WriteLine($"StackTrace: {ex.InnerException.StackTrace}");
                            writer.WriteLine($"--- End Inner Exception ---");
                        }
                        writer.WriteLine(new string('-', 40));
                    }
                }
                catch (Exception logEx)
                {
                    // Если запись в лог не удалась, выводим в консоль
                    Console.WriteLine($"FATAL: Failed to write to log file {_logFilePath}. Log Error: {logEx.Message}. Original Error: {ex.Message}");
                }
            }
        }
    }
}