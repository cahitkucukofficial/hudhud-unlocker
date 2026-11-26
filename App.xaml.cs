using System.Windows;

namespace HudhudUnlocker;

public partial class App : System.Windows.Application
{
    public static string StartPath = "";

    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += (s, args) =>
        {
            System.Windows.MessageBox.Show(args.Exception.ToString(), "Hudhud Unlocker", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
        base.OnStartup(e);
        if (e.Args.Length > 0) StartPath = e.Args[0];
        try
        {
            new MainWindow().Show();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.ToString(), "Hudhud Unlocker", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }
}
