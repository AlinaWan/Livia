import importlib
import sys
from pathlib import Path

from ._version import __version__
from pythonnet import load


_PACKAGE_DIRECTORY = Path(__file__).resolve().parent
_ASSEMBLY_PATH = _PACKAGE_DIRECTORY / "Livia.dll"
_RUNTIME_CONFIG_PATH = _PACKAGE_DIRECTORY / "runtimeconfig.json"
_WINDOWS_SDK_ASSEMBLY_PATH = (
    _PACKAGE_DIRECTORY / "Microsoft.Windows.SDK.NET.dll"
)
_WINRT_RUNTIME_ASSEMBLY_PATH = (
    _PACKAGE_DIRECTORY / "WinRT.Runtime.dll"
)

if not _ASSEMBLY_PATH.exists():
    raise FileNotFoundError(
        f"Could not find Livia.dll at {_ASSEMBLY_PATH}"
    )

if not _RUNTIME_CONFIG_PATH.exists():
    raise FileNotFoundError(
        f"Could not find runtimeconfig.json at {_RUNTIME_CONFIG_PATH}"
    )

if not _WINDOWS_SDK_ASSEMBLY_PATH.exists():
    raise FileNotFoundError(
        f"Could not find Microsoft.Windows.SDK.NET.dll at "
        f"{_WINDOWS_SDK_ASSEMBLY_PATH}"
    )

if not _WINRT_RUNTIME_ASSEMBLY_PATH.exists():
    raise FileNotFoundError(
        f"Could not find WinRT.Runtime.dll at "
        f"{_WINRT_RUNTIME_ASSEMBLY_PATH}"
    )

# Make the package directory available for managed assembly resolution.
sys.path.insert(0, str(_PACKAGE_DIRECTORY))

load(
    "coreclr",
    runtime_config=str(_RUNTIME_CONFIG_PATH),
)

import clr

# Load the Windows SDK projection before Livia.
# Load runtime dependencies.
clr.AddReference("PresentationFramework")
clr.AddReference("PresentationCore")

clr.AddReference(str(_WINRT_RUNTIME_ASSEMBLY_PATH))
clr.AddReference(str(_WINDOWS_SDK_ASSEMBLY_PATH))

# Load Livia.
_assembly = clr.AddReference(str(_ASSEMBLY_PATH))

import System

try:
    types = _assembly.GetTypes()

except System.Reflection.ReflectionTypeLoadException as exc:
    # print("\n[LIVIA] Failed to load assembly types:")

    # for index, error in enumerate(exc.LoaderExceptions):
        # if error is not None:
            # print(f"\n[{index}] {type(error).__name__}")
            # print(error)

    raise

for type_ in types:
    if not type_.IsPublic or type_.IsNested:
        continue

    namespace = type_.Namespace

    if namespace is None:
        continue

    module = importlib.import_module(namespace)

    if hasattr(module, type_.Name):
        globals()[type_.Name] = getattr(module, type_.Name)
