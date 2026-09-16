using System.Windows;

namespace AutoOrder;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var application = new Application();

        application.Run(new MainWindow());
    }
}