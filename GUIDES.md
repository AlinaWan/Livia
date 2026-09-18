# Livia Guides

Practical examples demonstrating how Livia's APIs can be used
to implement common functionality in an application.

These examples are intended as reference implementations, and
applications may implement the same functionality differently.

## Detecting a Roblox Disconnect

`RobloxLogMonitor` can monitor Roblox log files and invoke a callback when a configured regular expression matches a log entry.

For example, the following pattern detects Roblox's network disconnect message:

```csharp
private readonly RobloxLogMonitor _logMonitor = new();

private void StartLogMonitoring()
{
    _logMonitor.Start(new Dictionary<string, Action<Match>>
    {
        [@"\[FLog::Network\] Time to disconnect replication data: ([\d.]+)"]
            = OnRobloxDisconnected
    });
}
```

The callback receives the `Match` produced by the regular expression, allowing captured values to be accessed if they are needed:

```csharp
private void OnRobloxDisconnected(Match match)
{
    string value = match.Groups[1].Value;

    // Handle the disconnect.
}
```

For a disconnect detector where the captured value is not needed, the callback can simply ignore the `Match`:

```csharp
private void OnRobloxDisconnected(Match _)
{
    // Handle the disconnect.
}
```

The application should decide what a disconnect means for its own macro. It may stop the macro, attempt to rejoin, notify the user, or perform some other action.

---

## Implementing Auto-Rejoin

Auto-rejoin can be implemented by combining `RobloxLogMonitor` with the application's own macro lifecycle and Roblox process management.

A typical implementation consists of three steps:

1. Detect the disconnect with `RobloxLogMonitor`.
2. Cancel the current macro operation.
3. Open a Roblox private-server URL and wait for a new Roblox process.

### Detect the Disconnect

The disconnect callback can trigger the application's rejoin logic:

```csharp
private bool _isRejoining;

private void OnRobloxDisconnected(Match _)
{
    if (_isRejoining)
        return;

    _isRejoining = true;

    // Cancel the currently running macro.
    _cancellationTokenSource?.Cancel();

    _cancellationTokenSource = new CancellationTokenSource();

    _ = RejoinAsync(_cancellationTokenSource.Token);
}
```

The exact lifecycle handling will depend on the application. For example, a WPF application may need to dispatch the callback to the UI thread before modifying UI state.

### Open the Private Server

Use a Roblox private-server URL for the experience that the application is automating.

For example:

```csharp
private const string RejoinUrl =
    "https://www.roblox.com/share?code=YOUR_CODE&type=Server";
```

The URL can be opened using the operating system's default browser/URL handler:

```csharp
Process.Start(new ProcessStartInfo
{
    FileName = RejoinUrl,
    UseShellExecute = true
});
```

### Wait for the New Roblox Process

After opening the rejoin URL, the application needs to wait for Roblox to launch the new session.

One way to do this is to record the PID of the existing Roblox process, then periodically check for a Roblox process with a different PID and an active main window.

```csharp
Process? newProcess = null;

while (newProcess == null)
{
    token.ThrowIfCancellationRequested();

    await Task.Delay(1000, token);

    Process? process = Process
        .GetProcessesByName("RobloxPlayerBeta")
        .FirstOrDefault();

    if (process != null && process.Id != oldPid)
    {
        process.Refresh();

        if (process.MainWindowHandle != IntPtr.Zero)
            newProcess = process;
    }
}
```

Once `newProcess` is assigned, the application has found the new Roblox process and can continue with whatever initialization or monitoring it requires.
