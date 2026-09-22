<div align="center">
  <h1>Livia</h1>
  <p><b>A New Frontier in Macro Development</b></p>

[![License](https://img.shields.io/github/license/AlinaWan/Livia)](LICENSE)
[![C#](https://custom-icon-badges.demolab.com/badge/C%23-%23239120.svg?logo=cshrp&logoColor=white)](#)
[![.NET](https://img.shields.io/badge/.NET-512BD4?logo=dotnet&logoColor=fff)](#)
[![Windows](https://custom-icon-badges.demolab.com/badge/For%20Windows%2011-0078D6?logo=windows11&logoColor=white)](#)

An avant-garde open-source C# automation framework and engine that brings reusable, extensible software architecture to Roblox macros, challenging the status quo of building automation as isolated, one-off scripts.

<sub>*"Magnus ab integro saeclorum nascitur ordo" —Virgil, Eclogue IV (c. 40 BCE)*</sub>

<img src="assets/preview.webp" alt="Preview" width="100%">

</div>

## Prerequisites

* [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
* Windows 11 or later

## Build Your Own Macro

Livia can be added to an existing console application project by adding the package from NuGet:
```powershell
dotnet add package Livia
```

or install the official Python bindings to an existing Python project:
```powershell
pip install livia-python # -> import livia
```

## API

Livia provides an easy-to-use API for creating automation macros. The following list is not exhaustive, but it covers the most commonly used APIs.

See [Livia Guides](GUIDES.md) for practical examples demonstrating how Livia's APIs can be used to implement common functionality in an application, including detecting Roblox disconnects and implementing auto-rejoin.

### UI

<details>
  <summary>Click to expand</summary>

| API                                    | Description                                        |
| :------------------------------------- | -------------------------------------------------- |
| `CommonTab.Create(...)`                | Creates a new tab in the UI.                       |
| `CommonIntegerInput.CreateRow(...)`    | Creates a new row with an integer input box.       |
| `CommonStringInput.CreateRow(...)`     | Creates a new row with a string input box.         |
| `CommonToggle.CreateRow(...)`          | Creates a new row with a toggle switch.            |
| `CommonSegmentedToggle.CreateRow(...)` | Creates a new row with a segmented toggle switch.  |
| `CommonHelpStep.Create(...)`           | Creates a new help step with a number and content. |

</details>

### Input Simulation

<details>
  <summary>Click to expand</summary>

| API                                              | Description                                                               |
| :----------------------------------------------- | ------------------------------------------------------------------------- |
| `HotkeyService`                                  | Registers hotkeys with optional modifiers.                                |
| `Mouse.MoveMouseToPositionOnVirtualDesktop(...)` | Moves the mouse to a specific position on the virtual desktop.            |
| `Mouse.MoveMouseBy(...)`                         | Moves the mouse by a specified offset.                                    |
| `Mouse.VerticalScroll(...)`                      | Scrolls the mouse wheel vertically by a multiple of wheel delta.          |
| `Mouse.LeftClick(...)`                           | Simulates a left mouse click.                                             |
| `Mouse.RightClick(...)`                          | Simulates a right mouse click.                                            |
| `Keyboard.KeyDown(...)`                          | Simulates a key press down event.                                         |
| `Keyboard.KeyUp(...)`                            | Simulates a key release event.                                            |
| `Keyboard.KeyPress(...)`                         | Simulates a key press and release event.                                  |
| `VirtualKeys`                                    | An enumeration of key names and their corresponding virtual key codes.    |
| `ModifierKeys`                                   | An enumeration of modifier key names and their corresponding flag values. |

</details>

### Services

<details>
  <summary>Click to expand</summary>

| API                     | Description                                                                   |
| :---------------------- | ----------------------------------------------------------------------------- |
| `DxgiFrameProvider`     | Captures a screen region directly from a DXGI GPU staging buffer.             |
| `WindowFocusMonitor`    | Monitors a process by executable name and raises an event on focus change.    |
| `RobloxLogMonitor`      | Monitors a Roblox log file by a regex pattern and invokes callbacks on match. |
| `WindowsOcrService`     | Provides optical character recognition using the native Windows OCR engine.   |
| `DiscordWebhookService` | Sends payloads to a Discord channel via webhooks.                             |
| `SmsService`            | Sends SMS text messages to a specified phone number.                          |

</details>

### Utilities

<details>
  <summary>Click to expand</summary>

| API                                       | Description                                                                      |
| :---------------------------------------- | -------------------------------------------------------------------------------- |
| `WindowUtils.ForceFocusWindow(...)`       | Forces the target application window to the foreground and brings it into focus. |
| `SystemUtils.InitiateSystemShutdown(...)` | Requests a system shutdown with a specified timeout and display message.         |
| `SystemUtils.AbortSystemShutdown(...)`    | Aborts a previously scheduled system shutdown request.                           |
| `IpUtils.GetIpInfoAsync(...)`             | Retrieves information for a specified internet protocol address.                 |

</details>

## Livia Reference Macros

The official Livia macros can be used as references for building your own macros. You can find them in the `Apps` folder of this repository.

Run any official Livia macro project directly using the .NET CLI:

```powershell
dotnet run --project Apps/<Experience>/<Macro>
```

### Available Reference Macros

<div align="center">

| Example Experience                                      | Available Reference Macros              |
| :------------------------------------------------------ | :-------------------------------------- |
| [My Cafe](https://www.roblox.com/games/133345376331809) | [Auto Order](Apps/MyCafe/AutoOrder/)    |
| General                                                 | [Auto Rejoin](Apps/General/AutoRejoin/) |
| more coming soon™                                       |                                         |

</div>

---

<div align="center">
Made with ❤️ in Visual Studio
</div>