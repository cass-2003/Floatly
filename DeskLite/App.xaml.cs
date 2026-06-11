namespace DeskLite;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        if (e.Args.Any(arg => string.Equals(arg, "--settings", StringComparison.OrdinalIgnoreCase)))
        {
            ShutdownMode = System.Windows.ShutdownMode.OnMainWindowClose;
            var settingsWindow = new SettingsWindow(Services.JsonStore.LoadSettings());
            MainWindow = settingsWindow;
            settingsWindow.Show();
            return;
        }

        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        mainWindow.Show();
    }
}
