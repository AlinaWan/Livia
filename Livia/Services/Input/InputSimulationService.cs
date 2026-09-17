using System;
using System.Runtime.InteropServices;
using Livia.Native;

namespace Livia.Services.Input;

public class InputSimulationService
{
    public MouseDevice Mouse { get; } = new();
    public KeyboardDevice Keyboard { get; } = new();
}
