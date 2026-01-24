[CmdletBinding()]
param(
    [string]$Namespace = "k8s.io",
    [string]$Platform = "linux/amd64"
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

$images = @(
    @{ Image = "erp/user-management"; Context = "services/user-management"; Dockerfile = "services/user-management/Dockerfile" },
    @{ Image = "erp/inventory"; Context = "services/inventory"; Dockerfile = "services/inventory/Dockerfile" },
    @{ Image = "erp/sales"; Context = "services/sales"; Dockerfile = "services/sales/Dockerfile" },
    @{ Image = "erp/financial"; Context = "services/financial"; Dockerfile = "services/financial/Dockerfile" },
    @{ Image = "erp/dashboard"; Context = "services/dashboard"; Dockerfile = "services/dashboard/Dockerfile" },
    @{ Image = "erp/gateway"; Context = "services/gateway"; Dockerfile = "services/gateway/Dockerfile" },
    @{ Image = "erp/frontend"; Context = "frontend"; Dockerfile = "frontend/Dockerfile" }
)

foreach ($item in $images) {
    $contextPath = (Resolve-Path (Join-Path $repoRoot $item.Context)).Path
    $dockerfilePath = (Resolve-Path (Join-Path $repoRoot $item.Dockerfile)).Path

    Write-Host "Building $($item.Image)" -ForegroundColor Cyan
    Write-Host "  context:    $contextPath"
    Write-Host "  dockerfile: $dockerfilePath"
    Write-Host "  platform:   $Platform"
    Write-Host "  namespace:  $Namespace"

    & nerdctl --namespace $Namespace build --platform $Platform -t $item.Image -f $dockerfilePath $contextPath
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
