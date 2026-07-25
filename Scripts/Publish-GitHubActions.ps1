# .\Scripts\Publish-GitHubActions.ps1 -Version 0.1.13

[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^\d+\.\d+\.\d+(?:-[0-9A-Za-z][0-9A-Za-z.-]*)?(?:\+[0-9A-Za-z.-]+)?$')]
    [string]$Version,

    [string]$Remote = 'origin',

    [string]$Commit = 'HEAD'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-Git
{
    param([string[]]$Arguments)

    $previousErrorActionPreference = $ErrorActionPreference
    try
    {
        $ErrorActionPreference = 'Continue'
        $output = & git @Arguments 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally
    {
        $ErrorActionPreference = $previousErrorActionPreference
    }

    if ($exitCode -ne 0)
    {
        throw "git $($Arguments -join ' ') failed:`n$($output -join [Environment]::NewLine)"
    }

    return @($output | Where-Object { $_ -isnot [System.Management.Automation.ErrorRecord] })
}

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$currentRepositoryRoot = (Invoke-Git @('rev-parse', '--show-toplevel')).Trim()
if (-not [string]::Equals(
        [IO.Path]::GetFullPath($currentRepositoryRoot),
        [IO.Path]::GetFullPath($repositoryRoot),
        [StringComparison]::OrdinalIgnoreCase))
{
    throw "Run this script from the Rayo repository. Expected: $repositoryRoot"
}

$workingTreeChanges = @(Invoke-Git @('status', '--porcelain'))
if ($workingTreeChanges.Count -gt 0)
{
    throw 'The working tree is not clean. Commit or stash all changes before creating a release tag.'
}

$commitSha = (Invoke-Git @('rev-parse', '--verify', "$Commit^{commit}")).Trim()
$tag = "v$Version"

$localTag = & git rev-parse -q --verify "refs/tags/$tag" 2>$null
if ($LASTEXITCODE -eq 0)
{
    throw "The local tag '$tag' already exists."
}

if (-not $PSCmdlet.ShouldProcess(
        "$tag at $commitSha",
        "create and push release tag to '$Remote'"))
{
    return
}

$remoteTag = @(Invoke-Git @('ls-remote', '--tags', '--refs', $Remote, "refs/tags/$tag"))
if ($remoteTag.Count -gt 0)
{
    throw "The remote tag '$tag' already exists on '$Remote'."
}

Invoke-Git @('tag', '-a', $tag, $commitSha, '-m', "Release $Version") | Out-Null

try
{
    Invoke-Git @('push', $Remote, $tag) | Out-Null
}
catch
{
    throw "The tag '$tag' was created locally but could not be pushed. Delete it with 'git tag -d $tag' after resolving the issue, or push it manually."
}

Write-Host "Pushed $tag. GitHub Actions will run publish-nuget.yml and publish the Rayo NuGet packages at version $Version."
