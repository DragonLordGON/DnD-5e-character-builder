using System.Windows;
using System.Windows.Threading;
using System;
using System.IO;

namespace DndCharacterBuilder
{
    public partial class App : Application
    {
        private string _logDir;

        public App()
        {
            _logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            CleanOldLogs();

            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void CleanOldLogs()
        {
            try
            {
                if (!Directory.Exists(_logDir)) Directory.CreateDirectory(_logDir);
                else
                {
                    // Clean previous logs on startup
                    var logFiles = Directory.GetFiles(_logDir, "*.txt");
                    foreach (var file in logFiles)
                    {
                        try { File.Delete(file); } catch { /* Ignore locked files */ }
                    }
                }
            }
            catch { /* Silent fail */ }
        }
        
        private void LogCrash(object exceptionObj, string source)
        {
            try 
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string logFile = Path.Combine(_logDir, $"crash_{timestamp}.txt");
                string message = (exceptionObj as Exception)?.ToString() ?? exceptionObj?.ToString() ?? "Unknown error object";
                File.WriteAllText(logFile, $"[{DateTime.Now}] CRITICAL ERROR ({source}):\n{message}");
            }
            catch { }
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogCrash(e.Exception, "Dispatcher");
            MessageBox.Show($"UNHANDLED EXCEPTION: {e.Exception.Message}\nCheck logs folder for details.", "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
            Current.Shutdown();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
             LogCrash(e.ExceptionObject, "AppDomain");
             MessageBox.Show($"CRITICAL ERROR: {e.ExceptionObject}", "Crash", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
