
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
```

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