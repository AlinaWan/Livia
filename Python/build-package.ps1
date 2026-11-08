
[CmdletBinding()]
param(
    [string]$Python = "python",

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+\.\d+$')]
    [string]$Version,

    [int]$Build = 0,

    [int]$Revision = 0
)

$ErrorActionPreference = "Stop"

# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------

$pythonDirectory = $PSScriptRoot
$repositoryDirectory = Split-Path $pythonDirectory -Parent

$liviaProject = Join-Path $repositoryDirectory "Livia\Livia.csproj"

$liviaOutput = Join-Path `
    $repositoryDirectory `
    "Livia\bin\Release\net10.0-windows10.0.22000.0"

$packageDirectory = Join-Path $pythonDirectory "livia"

$nativeOutput = Join-Path `
    $liviaOutput `
    "Native\ScreenCapture\Livia.Native.DxgiFrameCapture.dll"

$nativePackageDirectory = Join-Path `
    $packageDirectory `
    "Native\ScreenCapture"

$distDirectory = Join-Path $pythonDirectory "dist"

# ---------------------------------------------------------------------------
# Version
# ---------------------------------------------------------------------------

$requestedVersion = [version]::Parse($Version)
$liviaMajorMinor = "$($requestedVersion.Major).$($requestedVersion.Minor)"

Write-Host ""
Write-Host "Building Livia"
Write-Host "  Version:       $Version"
Write-Host "  Major.Minor:   $liviaMajorMinor"
Write-Host "  Build:         $Build"
Write-Host "  Revision:      $Revision"
Write-Host ""

# ---------------------------------------------------------------------------
# Validate paths
# ---------------------------------------------------------------------------

if (-not (Test-Path $liviaProject)) {
    throw "Livia.csproj was not found: $liviaProject"
}

if (-not (Test-Path (Join-Path $pythonDirectory "pyproject.toml"))) {
    throw "pyproject.toml was not found in $pythonDirectory."
}

if (-not (Test-Path $packageDirectory)) {
    throw "Python package directory was not found: $packageDirectory"
}

# ---------------------------------------------------------------------------
# Build Livia
# ---------------------------------------------------------------------------

Write-Host "[INFO] Building Livia..."

& dotnet build $liviaProject `
    --configuration Release `
    "-p:LiviaMajorMinor=$liviaMajorMinor" `
    "-p:LiviaBuild=$Build" `
    "-p:LiviaRevision=$Revision"

if ($LASTEXITCODE -ne 0) {
    throw "Livia build failed with exit code $LASTEXITCODE."
}

# ---------------------------------------------------------------------------
# Validate build outputs
# ---------------------------------------------------------------------------

$liviaDll = Join-Path $liviaOutput "Livia.dll"
$depsPath = Join-Path $liviaOutput "Livia.deps.json"

foreach ($file in @($liviaDll, $depsPath, $nativeOutput)) {
    if (-not (Test-Path $file)) {
        throw "Required build output was not found: $file"
    }
}

$assemblyVersion = [System.Reflection.AssemblyName]::GetAssemblyName(
    $liviaDll
).Version

Write-Host "[OK] Livia.dll ($assemblyVersion)"

# ---------------------------------------------------------------------------
# Prepare Python package
# ---------------------------------------------------------------------------

New-Item -ItemType Directory -Path $packageDirectory -Force |
    Out-Null

New-Item -ItemType Directory -Path $nativePackageDirectory -Force |
    Out-Null

# ---------------------------------------------------------------------------
# Copy Livia.dll and its dependency manifest
# ---------------------------------------------------------------------------

Copy-Item $liviaDll `
    -Destination $packageDirectory `
    -Force

Copy-Item $depsPath `
    -Destination $packageDirectory `
    -Force

Write-Host "[OK] Copied Livia.dll and Livia.deps.json"

# ---------------------------------------------------------------------------
# Locate Microsoft.Windows.SDK.NET.dll
# ---------------------------------------------------------------------------

Write-Host "[INFO] Resolving Microsoft.Windows.SDK.NET.dll..."

$deps = Get-Content $depsPath -Raw | ConvertFrom-Json

$runtimePack = $deps.libraries.PSObject.Properties |
    Where-Object {
        $_.Name -like "runtimepack.Microsoft.Windows.SDK.NET.Ref/*"
    } |
    Select-Object -First 1

if ($null -eq $runtimePack) {
    throw "Microsoft.Windows.SDK.NET.Ref was not found in Livia.deps.json."
}

$runtimePackVersion = ($runtimePack.Name -split "/", 2)[1]

$nugetPackageRoot = if ($env:NUGET_PACKAGES) {
    $env:NUGET_PACKAGES
}
else {
    Join-Path $env:USERPROFILE ".nuget\packages"
}

$runtimePackDirectory = Join-Path `
    $nugetPackageRoot `
    "microsoft.windows.sdk.net.ref\$runtimePackVersion"

if (-not (Test-Path $runtimePackDirectory)) {
    throw "Windows SDK runtime pack was not found: $runtimePackDirectory"
}

$sdkAssembly = Get-ChildItem `
    $runtimePackDirectory `
    -Filter "Microsoft.Windows.SDK.NET.dll" `
    -File `
    -Recurse |
    Select-Object -First 1

if ($null -eq $sdkAssembly) {
    throw "Microsoft.Windows.SDK.NET.dll was not found in $runtimePackDirectory."
}

# Find the expected assembly version in the dependency manifest.

$expectedSdkVersion = $null

foreach ($target in $deps.targets.PSObject.Properties) {
    $sdkTarget = $target.Value.PSObject.Properties |
        Where-Object {
            $_.Name -like "runtimepack.Microsoft.Windows.SDK.NET.Ref/*"
        } |
        Select-Object -First 1

    if ($null -ne $sdkTarget) {
        $expectedSdkVersion = $sdkTarget.Value.runtime.
            'Microsoft.Windows.SDK.NET.dll'.assemblyVersion

        if ($expectedSdkVersion) {
            break
        }
    }
}

if (-not $expectedSdkVersion) {
    throw "Could not determine the expected Windows SDK assembly version from Livia.deps.json."
}

$actualSdkVersion = [System.Reflection.AssemblyName]::GetAssemblyName(
    $sdkAssembly.FullName
).Version

if ($actualSdkVersion -ne [version]$expectedSdkVersion) {
    throw (
        "Microsoft.Windows.SDK.NET assembly version mismatch. " +
        "Expected $expectedSdkVersion, found $actualSdkVersion."
    )
}

Copy-Item $sdkAssembly.FullName `
    -Destination $packageDirectory `
    -Force

Write-Host "[OK] Microsoft.Windows.SDK.NET.dll ($actualSdkVersion)"

# ---------------------------------------------------------------------------
# Locate WinRT.Runtime.dll
# ---------------------------------------------------------------------------

$winRtAssembly = Get-ChildItem `
    $runtimePackDirectory `
    -Filter "WinRT.Runtime.dll" `
    -File `
    -Recurse |
    Select-Object -First 1

if ($null -eq $winRtAssembly) {
    throw "WinRT.Runtime.dll was not found in $runtimePackDirectory."
}

# Find the expected assembly version in the dependency manifest.

$expectedWinRtVersion = $null

foreach ($target in $deps.targets.PSObject.Properties) {
    $sdkTarget = $target.Value.PSObject.Properties |
        Where-Object {
            $_.Name -like "runtimepack.Microsoft.Windows.SDK.NET.Ref/*"
        } |
        Select-Object -First 1

    if ($null -ne $sdkTarget) {
        $expectedWinRtVersion = $sdkTarget.Value.runtime.
            'WinRT.Runtime.dll'.assemblyVersion

        if ($expectedWinRtVersion) {
            break
        }
    }
}

if (-not $expectedWinRtVersion) {
    throw "Could not determine the expected WinRT.Runtime assembly version from Livia.deps.json."
}

$actualWinRtVersion = [System.Reflection.AssemblyName]::GetAssemblyName(
    $winRtAssembly.FullName
).Version

if ($actualWinRtVersion -ne [version]$expectedWinRtVersion) {
    throw (
        "WinRT.Runtime assembly version mismatch. " +
        "Expected $expectedWinRtVersion, found $actualWinRtVersion."
    )
}

Copy-Item $winRtAssembly.FullName `
    -Destination $packageDirectory `
    -Force

Write-Host "[OK] WinRT.Runtime.dll ($actualWinRtVersion)"

# ---------------------------------------------------------------------------
# Copy native DXGI capture library
# ---------------------------------------------------------------------------

Copy-Item $nativeOutput `
    -Destination $nativePackageDirectory `
    -Force

# Also place the native DLL beside Livia.dll for P/Invoke resolution.
Copy-Item `
    -LiteralPath (Join-Path $nativePackageDirectory "Livia.Native.DxgiFrameCapture.dll") `
    -Destination $packageDirectory `
    -Force

Write-Host "[OK] Copied Livia.Native.DxgiFrameCapture.dll"

# ---------------------------------------------------------------------------
# Generate runtimeconfig.json
# ---------------------------------------------------------------------------

$runtimeConfig = @{
    runtimeOptions = @{
        tfm = "net10.0"
        frameworks = @(
            @{
                name = "Microsoft.NETCore.App"
                version = "10.0.0"
            },
            @{
                name = "Microsoft.WindowsDesktop.App"
                version = "10.0.0"
            }
        )
    }
}

$runtimeConfigPath = Join-Path $packageDirectory "runtimeconfig.json"

$runtimeConfig |
    ConvertTo-Json -Depth 10 |
    Set-Content -Path $runtimeConfigPath -Encoding utf8

Write-Host "[OK] Generated runtimeconfig.json"

# ---------------------------------------------------------------------------
# Generate Python version information
# ---------------------------------------------------------------------------

$versionFile = Join-Path $packageDirectory "_version.py"

@"
__version__ = "$assemblyVersion"
"@ | Set-Content -Path $versionFile -Encoding utf8

Write-Host "[OK] Generated _version.py ($assemblyVersion)"

# ---------------------------------------------------------------------------
# Build Python wheel
# ---------------------------------------------------------------------------

Write-Host ""
Write-Host "[INFO] Building Python wheel..."

Push-Location $pythonDirectory

try {
    # Remove old wheels so they cannot be mistaken for the new build.
    if (Test-Path $distDirectory) {
        Get-ChildItem $distDirectory -Filter "*.whl" -File |
            Remove-Item -Force
    }

    & $Python -m build . --wheel

    if ($LASTEXITCODE -ne 0) {
        throw "Python wheel build failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}

# ---------------------------------------------------------------------------
# Verify resulting wheel
# ---------------------------------------------------------------------------

$wheel = Get-ChildItem $distDirectory -Filter "*.whl" -File |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if ($null -eq $wheel) {
    throw "Python wheel was not generated."
}

Write-Host ""
Write-Host "[SUCCESS] Livia Python package built successfully."
Write-Host "  Assembly: $assemblyVersion"
Write-Host "  Wheel:    $($wheel.FullName)"
