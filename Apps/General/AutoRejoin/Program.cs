using System.Windows;

namespace AutoRejoin;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var application = new Application();

        application.Run(new MainWindow());
    }
}