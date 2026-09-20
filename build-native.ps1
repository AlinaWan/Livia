param(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

$vsWhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"

if (-not (Test-Path $vsWhere)) {
    throw "Visual Studio Installer's vswhere.exe was not found."
}

$installationPath = & $vsWhere `
    -latest `
    -products * `
    -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 `
    -property installationPath

if (-not $installationPath) {
    throw "A Visual Studio installation with C++ x64 tools was not found."
}

$msbuild = Join-Path `
    $installationPath `
    "MSBuild\Current\Bin\MSBuild.exe"

if (-not (Test-Path $msbuild)) {
    throw "Visual Studio MSBuild was not found at '$msbuild'."
}

$project = Join-Path `
    $PSScriptRoot `
    "Livia\Native\ScreenCapture\DxgiFrameCapture.vcxproj"

& $msbuild `
    $project `
    /p:Configuration=$Configuration `
    /p:Platform=x64

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}