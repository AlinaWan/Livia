using Livia.Services;

namespace SingletonMutexCloser;

// A thin console application to start the
// RobloxSingletonMutexClosingService from the Livia API.
public static class Program
{
    public static void Main()
    {
        using var service = new RobloxSingletonMutexClosingService();
        using var wait = new ManualResetEventSlim();

        service.SingletonHandlesClosed += (_, e) =>
        {
            Console.WriteLine(
                $"[{DateTime.Now:T}] " +
                $"PID {e.ProcessId}: closed " +
                $"{e.MutexesClosed} mutex handle(s), " +
                $"{e.EventsClosed} event handle(s).");
        };

        service.Start();

        Console.WriteLine(
            $"[{DateTime.Now:T}] Service started.");

        Console.WriteLine(
            "Press Ctrl+C to stop.");

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            wait.Set();
        };

        wait.Wait();
        service.Stop();
    }
}
