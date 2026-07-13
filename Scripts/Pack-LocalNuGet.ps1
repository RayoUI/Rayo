# .\Scripts\Pack-LocalNuGet.ps1 -Version 0.1.10
# .\Scripts\Pack-LocalNuGet.ps1 -Version 0.1.10 -OutputDirectory D:\feeds\rayo
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^\d+\.\d+\.\d+(?:-[0-9A-Za-z][0-9A-Za-z.-]*)?(?:\+[0-9A-Za-z.-]+)?$')]
    [string]$Version,

    [string]$OutputDirectory = 'C:\nuget-local'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-DotNet
{
    param([string[]]$Arguments)

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0)
    {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$nuGetConfigPath = Join-Path $repositoryRoot 'NuGet.Config'
$projectListPath = Join-Path $repositoryRoot 'eng\nuget-pack-projects.txt'

if (-not (Test-Path -LiteralPath $nuGetConfigPath))
{
    throw "NuGet configuration was not found: $nuGetConfigPath"
}

if (-not (Test-Path -LiteralPath $projectListPath))
{
    throw "NuGet project list was not found: $projectListPath"
}

$projects = Get-Content -LiteralPath $projectListPath |
    ForEach-Object { $_.Trim() } |
    Where-Object { $_ -and -not $_.StartsWith('#') } |
    ForEach-Object { Join-Path $repositoryRoot $_ }

if ($projects.Count -eq 0)
{
    throw 'The NuGet project list does not contain any projects.'
}

foreach ($projectPath in $projects)
{
    if (-not (Test-Path -LiteralPath $projectPath))
    {
        throw "NuGet project was not found: $projectPath"
    }
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

foreach ($projectPath in $projects)
{
    Write-Host "Restoring $projectPath..."
    Invoke-DotNet @('restore', $projectPath, '--configfile', $nuGetConfigPath, '--nologo')

    Write-Host "Building $projectPath..."
    Invoke-DotNet @(
        'build', $projectPath, '-c', 'Release', '--no-restore', '--nologo',
        "-p:PackageVersion=$Version"
    )

    Write-Host "Packing $projectPath..."
    Invoke-DotNet @(
        'pack', $projectPath, '-c', 'Release', '--no-build', '--no-restore', '--nologo',
        '-o', $OutputDirectory, "-p:PackageVersion=$Version"
    )
}

Write-Host "Created local NuGet packages version $Version in $OutputDirectory."
