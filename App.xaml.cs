using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ColorPanel;

public partial class App : Application
{
    static Mutex? _mutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Aucune erreur inattendue ne doit fermer l'application
        DispatcherUnhandledException += (_, a) => { Log.Write(a.Exception); a.Handled = true; };
        AppDomain.CurrentDomain.UnhandledException += (_, a) => Log.Write(a.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, a) => { Log.Write(a.Exception); a.SetObserved(); };

        // Une seule instance : un second clic ramène la fenêtre existante au premier plan
        _mutex = new Mutex(true, @"Local\ColorPanel_SingleInstance", out bool first);
        if (!first)
        {
            Native.BringExistingToFront();
            Shutdown();
            return;
        }

        base.OnStartup(e);
        try
        {
            new MainWindow().Show();
        }
        catch (Exception ex)
        {
            Log.Write(ex);
            MessageBox.Show("Color Panel n'a pas pu démarrer.\n\nDétails enregistrés dans :\n" + Log.Path_,
                "Color Panel", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }
}
