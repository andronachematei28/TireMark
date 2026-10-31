namespace TireMark;

using System.IO;
using System.Windows;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        try
        {
            AppSettings.Load();
            AppSettings.ApplyCulture();
            if (args.Contains("--init-db", StringComparer.OrdinalIgnoreCase))
            {
                _ = new GarageDatabase(Path.Combine(AppContext.BaseDirectory, "TireMark.db"), AppSettings.Current.Country);
                return;
            }
            var app = new Application { ShutdownMode = ShutdownMode.OnMainWindowClose };
            app.DispatcherUnhandledException += (_, eventArgs) =>
            {
                eventArgs.Handled = true;
                ReportCrash(eventArgs.Exception);
                app.Shutdown(-1);
            };
            AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
            {
                if (eventArgs.ExceptionObject is Exception exception) ReportCrash(exception);
                else ReportCrash(new Exception(Convert.ToString(eventArgs.ExceptionObject) ?? "Unknown fatal error."));
            };
            var mainWindow = new MainWindow(new GarageDatabase(Path.Combine(AppContext.BaseDirectory, "TireMark.db"), AppSettings.Current.Country));
            app.MainWindow = mainWindow;
            WindowsThemeChrome.Attach(mainWindow);
            mainWindow.Start();
            app.Run(mainWindow);
        }
        catch (Exception exception)
        {
            ReportCrash(exception);
            Environment.ExitCode = 1;
        }
    }

    private static int _crashReported;

    private static void ReportCrash(Exception exception)
    {
        if (Interlocked.Exchange(ref _crashReported, 1) != 0) return;
        try
        {
            var logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TireMark");
            Directory.CreateDirectory(logDirectory);
            File.AppendAllText(Path.Combine(logDirectory, "crash.log"), $"[{DateTimeOffset.Now:O}] {exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch { }
        try
        {
            MessageBox.Show("An unexpected error has occurred.", "TireMark", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch { }
    }
}

