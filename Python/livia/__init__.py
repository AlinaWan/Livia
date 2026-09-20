import importlib
from pathlib import Path
from ._version import __version__

from pythonnet import load


_PACKAGE_DIRECTORY = Path(__file__).resolve().parent
_ASSEMBLY_PATH = _PACKAGE_DIRECTORY / "Livia.dll"
_RUNTIME_CONFIG_PATH = _PACKAGE_DIRECTORY / "runtimeconfig.json"

if not _ASSEMBLY_PATH.exists():
    raise FileNotFoundError(
        f"Could not find Livia.dll at {_ASSEMBLY_PATH}"
    )

if not _RUNTIME_CONFIG_PATH.exists():
    raise FileNotFoundError(
        f"Could not find runtimeconfig.json at {_RUNTIME_CONFIG_PATH}"
    )

load(
    "coreclr",
    runtime_config=str(_RUNTIME_CONFIG_PATH),
)

import clr


_assembly = clr.AddReference(str(_ASSEMBLY_PATH))

for type_ in _assembly.GetTypes():
    if not type_.IsPublic or type_.IsNested:
        continue

    namespace = type_.Namespace

    if namespace is None:
        continue

    module = importlib.import_module(namespace)

    if hasattr(module, type_.Name):
        globals()[type_.Name] = getattr(module, type_.Name)