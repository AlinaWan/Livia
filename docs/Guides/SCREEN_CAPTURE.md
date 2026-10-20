## Screen Capture

### Getting Screen Dimensions

`SystemUtils.GetSystemMetric` retrieves system metrics, including the dimensions of the primary display monitor.

For example, retrieve the screen dimensions to define a capture region:

```csharp
int screenWidth = SystemUtils.GetSystemMetric(SystemMetric.ScreenWidth);
int screenHeight = SystemUtils.GetSystemMetric(SystemMetric.ScreenHeight);
```

To retrieve the dimensions and origin of the virtual screen, which encompasses all display monitors, use `VirtualScreenWidth`, `VirtualScreenHeight`, `VirtualScreenLeft`, and `VirtualScreenTop`:

```csharp
int virtualScreenWidth = SystemUtils.GetSystemMetric(SystemMetric.VirtualScreenWidth);
int virtualScreenHeight = SystemUtils.GetSystemMetric(SystemMetric.VirtualScreenHeight);
int virtualScreenLeft = SystemUtils.GetSystemMetric(SystemMetric.VirtualScreenLeft);
int virtualScreenTop = SystemUtils.GetSystemMetric(SystemMetric.VirtualScreenTop);
```

The virtual screen's origin may have negative coordinates when a monitor is positioned to the left of or above the primary monitor.

To retrieve the number of visible display monitors:

```csharp
int monitorCount = SystemUtils.GetSystemMetric(SystemMetric.MonitorCount);
```

All dimensions are returned in pixels. `ScreenWidth` and `ScreenHeight` describe the primary display monitor, whereas the virtual screen metrics describe the bounding rectangle of all display monitors.

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
