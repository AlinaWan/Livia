param(
    [string]$Project = "Livia\Livia.csproj"
)

$ErrorActionPreference = "Stop"

$projectPath = Resolve-Path $Project

$majorMinor = dotnet msbuild $projectPath `
    -getProperty:LiviaMajorMinor

if ($LASTEXITCODE -ne 0) {
    throw "Failed to read LiviaMajorMinor from $Project."
}

if ($majorMinor -notmatch '^\d+\.\d+$') {
    throw "Invalid LiviaMajorMinor: $majorMinor"
}

$epoch = [DateTime]::new(
    2000,
    1,
    1,
    0,
    0,
    0,
    [DateTimeKind]::Utc
)

$now = [DateTime]::UtcNow

$build = ($now.Date - $epoch.Date).Days

$revision = [int][Math]::Floor(
    ($now - $now.Date).TotalSeconds / 2
)

$version = "$majorMinor.$build.$revision"

Write-Host "Livia version: $version"
Write-Host "Major.Minor: $majorMinor"
Write-Host "Build: $build"
Write-Host "Revision: $revision"

"version=$version" >> $env:GITHUB_OUTPUT
"build=$build" >> $env:GITHUB_OUTPUT
"revision=$revision" >> $env:GITHUB_OUTPUT