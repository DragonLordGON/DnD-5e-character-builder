using System.Windows;
using System.Windows.Threading;
using System;
using System.IO;

namespace DndCharacterBuilder
{
    public partial class App : Application
    {
        public App()
        {
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"UNHANDLED EXCEPTION: {e.Exception.Message}\n\n{e.Exception.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
            // Optionally log to file
            File.AppendAllText("crash_log.txt", $"{DateTime.Now}: {e.Exception}\n");
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
             MessageBox.Show($"CRITICAL ERROR: {e.ExceptionObject}", "Crash", MessageBoxButton.OK, MessageBoxImage.Error);
             File.AppendAllText("crash_log.txt", $"{DateTime.Now}: {e.ExceptionObject}\n");
        }
    }
}
