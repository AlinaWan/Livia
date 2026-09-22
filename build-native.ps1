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

$vcToolsPath = Join-Path `
    $installationPath `
    "VC\Tools\MSVC"

if (-not (Test-Path $vcToolsPath)) {
    throw "MSVC tools were not found at '$vcToolsPath'."
}

$toolsets = Get-ChildItem $vcToolsPath -Directory |
    Where-Object {
        $_.Name -match '^14\.(\d+)\.'
    } |
    Sort-Object {
        [version]$_.Name
    } -Descending

$platformToolset = $null

foreach ($toolset in $toolsets) {
    $minor = [int]$toolset.Name.Split('.')[1]

    if ($minor -ge 50) {
        $platformToolset = "v145"
        break
    }

    if ($minor -ge 30) {
        $platformToolset = "v143"
        break
    }
}

if (-not $platformToolset) {
    throw "MSVC v143 or newer is required. Install Visual Studio 2022 or newer with the C++ x64 tools."
}

Write-Host "Visual Studio: $installationPath"
Write-Host "MSBuild: $msbuild"
Write-Host "MSVC toolset: $platformToolset"

$project = Join-Path `
    $PSScriptRoot `
    "Livia\Native\ScreenCapture\DxgiFrameCapture.vcxproj"

if (-not (Test-Path $project)) {
    throw "Native project was not found at '$project'."
}

& $msbuild `
    $project `
    /p:Configuration=$Configuration `
    /p:Platform=x64 `
    /p:LiviaPlatformToolset=$platformToolset

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
