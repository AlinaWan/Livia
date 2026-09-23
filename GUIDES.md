# Livia Guides

Practical examples demonstrating how Livia's APIs can be used
to implement common functionality in an application.

These examples are intended as reference implementations, and
applications may implement the same functionality differently.

## User Interface

*User interface guides coming soon.*

---

## Hotkeys & Input Simulation

### Registering Global Hotkeys

#### WPF Applications

For WPF applications, pass the application window to
`HotkeyService`. Hotkeys are registered automatically when the
window's underlying HWND is initialized, and callbacks execute
on the WPF UI thread.

```csharp
private readonly HotkeyService _hotkeys;

public MainWindow()
    : base(CreateWindowOptions())
{
    Body = BuildContent();

    StatusBar.SetStatus(
        "Status: Suspended (Press F6 to toggle)",
        Color.FromRgb(50, 50, 50));

    _hotkeys = new HotkeyService(this);

    _hotkeys.Register(
        VirtualKeys.F6,
        ModifierKeys.ModNoRepeat,
        ToggleMacro);
}
````

#### Non-WPF Applications

For applications that do not use WPF, `HotkeyService` can be
created without a window. In this mode, the service uses its own
background thread and must be started explicitly.

```csharp
using var hotkeys = new HotkeyService();

hotkeys.Register(
    VirtualKeys.F6,
    ModifierKeys.ModNoRepeat,
    ToggleMacro);

hotkeys.Start();
```

When using the non-WPF form, hotkey callbacks execute on the
service's background thread.

---

## Automation & Lifecycle

### Detecting a Roblox Disconnect

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

### Implementing Auto-Rejoin

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

---

### Automatically Retrieving a Private Server Link

Applications can automatically fetch a private server join link by retrieving the user's `.ROBLOSECURITY` authentication token from a Chromium-based browser and interacting with the game's running instances interface.

First, extract the token using `BrowserUtils`. Note that this requires **elevated privileges** and the MSBuild property `LiviaEnableBrowserCookieDecryption` to be set to `true` to successfully access the browser's protected local state and files:

```csharp
string? robloxSecurityToken = BrowserUtils.GetCookieValue(
    "Google\\Chrome", 
    ".roblox.com", 
    ".ROBLOSECURITY");
```

Once the token is available, pass it into `RobloxServerUtils` along with the game URL and target server name (or pass `null` to automatically select the first available configurable server):

```csharp
string gameUrl = "https://www.roblox.com/games/123456789";
string serverName = "JaneDoe's server"; // Optional: pass null for the first available

string? joinLink = await RobloxServerUtils.GetPrivateServerJoinLinkAsync(
    gameUrl, 
    serverName, 
    robloxSecurityToken);
```

*(Note: If the private server link has never been generated before on Roblox, the returned join link will be `null`.)*

---

## Screen Capture

### Capturing a Screen Region

`DxgiFrameProvider` captures screen regions using DXGI Desktop Duplication
and provides direct access to the mapped pixel buffer without copying the
pixels into a managed buffer.

Create a frame provider and specify the region to capture:

```csharp
using var provider = new DxgiFrameProvider();

var region = new CaptureRegion(
    Left: 0,
    Top: 0,
    Right: 1920,
    Bottom: 1080);

using Frame? frame = provider.Grab(region);

if (frame == null)
{
    return;
}
````

The captured frame provides its dimensions, row pitch, and BGRA8 pixel data:

```csharp
ReadOnlySpan<byte> pixels = frame.Pixels;

int width = frame.Width;
int height = frame.Height;
int rowPitch = frame.RowPitch;
```

The frame must remain undisposed while its pixel data is being used.
Disposing the frame releases the underlying DXGI mapping.
