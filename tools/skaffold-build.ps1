[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Context,

    [Parameter(Mandatory = $true)]
    [string]$Dockerfile,

    [string]$BuildConfiguration = "Release",

    [string]$Namespace = "k8s.io",
    [string]$Platform = "linux/amd64"
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($env:IMAGE)) {
    throw "Skaffold did not provide IMAGE env var."
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

function Resolve-RepoPath([string]$pathValue) {
    if ([System.IO.Path]::IsPathRooted($pathValue)) {
        return (Resolve-Path $pathValue).Path
    }

    return (Resolve-Path (Join-Path $repoRoot $pathValue)).Path
}

$resolvedContext = Resolve-RepoPath $Context
$resolvedDockerfile = Resolve-RepoPath $Dockerfile

Write-Host "Building $($env:IMAGE)" -ForegroundColor Cyan
Write-Host "  context:    $resolvedContext"
Write-Host "  dockerfile: $resolvedDockerfile"
Write-Host "  platform:   $Platform"
Write-Host "  namespace:  $Namespace"

$nerdctlArgs = @(
    "--namespace", $Namespace,
    "build",
    "--platform", $Platform,
    "--tag", $env:IMAGE,
    "--build-arg", "BUILD_CONFIGURATION=$BuildConfiguration",
    "--file", $resolvedDockerfile,
    $resolvedContext
)

& nerdctl @nerdctlArgs
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

# If Skaffold ever sets PUSH_IMAGE=true for this artifact, allow pushing.
if ($env:PUSH_IMAGE -and $env:PUSH_IMAGE.ToLowerInvariant() -eq 'true') {
    Write-Host "Pushing $($env:IMAGE)" -ForegroundColor Cyan
    & nerdctl --namespace $Namespace push $env:IMAGE
    exit $LASTEXITCODE
}
