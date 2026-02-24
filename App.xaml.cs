using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DndCharacterBuilder
{
    public partial class App : Application
    {
        private readonly string _logDir;

        public App()
        {
            _logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            CleanOldLogs();

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        public override void Initialize() => AvaloniaXamlLoader.Load(this);

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
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

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            LogCrash(e.Exception, "TaskScheduler");
            e.SetObserved();
        }

        private void CurrentDomain_UnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
             LogCrash(e.ExceptionObject, "AppDomain");
        }
    }
}
