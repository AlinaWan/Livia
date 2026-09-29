param(
    [string]$Python = "python",

    [Parameter(Mandatory = $true)]
    [string]$Version,

    [Parameter(Mandatory = $true)]
    [int]$Build,

    [Parameter(Mandatory = $true)]
    [int]$Revision
)

$ErrorActionPreference = "Stop"

if ($Version -notmatch '^\d+\.\d+\.\d+\.\d+$') {
    throw "Invalid version: $Version"
}

$pythonCommand = Get-Command $Python -ErrorAction Stop
$Python = $pythonCommand.Source

$pythonDirectory = $PSScriptRoot
$repositoryDirectory = Split-Path $pythonDirectory -Parent

$liviaProject = Join-Path `
    $repositoryDirectory `
    "Livia\Livia.csproj"

$liviaOutput = Join-Path `
    $repositoryDirectory `
    "Livia\bin\Release\net10.0-windows10.0.22000.0"

$packageDirectory = Join-Path $pythonDirectory "livia"
$nativeDirectory = Join-Path `
    $packageDirectory `
    "Native\ScreenCapture"

Write-Host "Livia version: $Version"

Write-Host "Preparing Python package..."

New-Item `
    -ItemType Directory `
    -Force `
    -Path $packageDirectory |
    Out-Null

New-Item `
    -ItemType Directory `
    -Force `
    -Path $nativeDirectory |
    Out-Null

@"
__version__ = "$Version"
"@ | Set-Content `
    (Join-Path $packageDirectory "_version.py") `
    -Encoding utf8

Write-Host "Building Livia..."

dotnet build `
    $liviaProject `
    --configuration Release `
    /p:LiviaBuild=$Build `
    /p:LiviaRevision=$Revision

if ($LASTEXITCODE -ne 0) {
    throw "Livia build failed."
}

Copy-Item `
    (Join-Path $liviaOutput "Livia.dll") `
    (Join-Path $packageDirectory "Livia.dll") `
    -Force

Copy-Item `
    (Join-Path `
        $liviaOutput `
        "Native\ScreenCapture\Livia.Native.DxgiFrameCapture.dll") `
    (Join-Path `
        $nativeDirectory `
        "Livia.Native.DxgiFrameCapture.dll") `
    -Force

@"
{
  "runtimeOptions": {
    "tfm": "net10.0",
    "frameworks": [
      {
        "name": "Microsoft.NETCore.App",
        "version": "10.0.0"
      },
      {
        "name": "Microsoft.WindowsDesktop.App",
        "version": "10.0.0"
      }
    ]
  }
}
"@ | Set-Content `
    (Join-Path $packageDirectory "runtimeconfig.json") `
    -Encoding utf8

Write-Host "Building Python wheel..."

Push-Location $pythonDirectory

try {
    & $Python -m build . `
        --wheel `
        --config-setting=--build-option=--plat-name=win_amd64

    if ($LASTEXITCODE -ne 0) {
        throw "Python wheel build failed."
    }
}
finally {
    Pop-Location
}

Write-Host "Python package built successfully."