# Livia for Python

**A New Frontier in Macro Development**

Official Python bindings for the [Livia](https://github.com/AlinaWan/Livia) .NET library, an avant-garde open-source C# automation framework and engine that brings reusable, extensible software architecture to Roblox macros, challenging the status quo of building automation as isolated, one-off scripts.

*"Magnus ab integro saeclorum nascitur ordo" —Virgil, Eclogue IV (c. 40 BCE)*

## Requirements

- Python 3.11 or later
- [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- Windows 11 x86-64

## Installation

Livia can be added to an existing Python project by adding the package:
```powershell
pip install livia-python # -> import livia
```

## API Usage

The public Livia APIs are exposed directly through the `livia` package.

```python
import livia

print(livia.WindowUtils)
````

Livia APIs that accept .NET-specific types require the corresponding Python.NET type.
For example, `WindowUtils.ForceFocusWindow` accepts a `System.IntPtr` because an
HWND is represented by `IntPtr` in the .NET API.

```python
import livia

from System import IntPtr

hwnd = IntPtr(12345678)

if livia.WindowUtils.ForceFocusWindow(hwnd):
    print("Window focused")
```

The Python package uses [Python.NET](https://pythonnet.github.io/) to expose the
underlying .NET APIs.
