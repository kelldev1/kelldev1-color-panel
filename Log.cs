using System.IO;

namespace ColorPanel;

/// <summary>Journal d'erreurs discret : l'application ne doit jamais s'arrêter à cause d'un problème secondaire.</summary>
public static class Log
{
    public static string Path_ =>
        System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ColorPanel", "error.log");

    public static void Write(Exception? ex)
    {
        try
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path_)!);
            File.AppendAllText(Path_, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}\r\n\r\n");
        }
        catch { /* on ne plante jamais à cause du journal */ }
    }
}
