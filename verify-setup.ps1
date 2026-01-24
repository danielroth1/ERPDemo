# Verify Rancher Desktop k3s + nerdctl Setup
# Run this script to ensure everything is configured correctly

Write-Host "Verifying Rancher Desktop Setup..." -ForegroundColor Cyan
Write-Host ""

# Check nerdctl
Write-Host "1. Checking nerdctl..." -ForegroundColor Yellow
try {
    $nerdctlVersion = nerdctl --version 2>&1
    Write-Host "   OK - nerdctl found: $nerdctlVersion" -ForegroundColor Green
}
catch {
    Write-Host "   ERROR - nerdctl not found!" -ForegroundColor Red
    Write-Host "   Add Rancher Desktop to PATH:" -ForegroundColor Yellow
    Write-Host "   C:\Program Files\Rancher Desktop\resources\resources\win32\bin" -ForegroundColor White
    exit 1
}

# Check nerdctl compose
Write-Host ""
Write-Host "2. Checking nerdctl compose..." -ForegroundColor Yellow
try {
    $composeVersion = nerdctl compose version 2>&1
    Write-Host "   OK - nerdctl compose found: $composeVersion" -ForegroundColor Green
}
catch {
    Write-Host "   ERROR - nerdctl compose not found!" -ForegroundColor Red
    exit 1
}

# Check kubectl
Write-Host ""
Write-Host "3. Checking kubectl..." -ForegroundColor Yellow
try {
    $kubectlVersion = kubectl version --client --short 2>&1
    Write-Host "   OK - kubectl found: $kubectlVersion" -ForegroundColor Green
}
catch {
    Write-Host "   WARNING - kubectl not found (optional for Kubernetes testing)" -ForegroundColor Yellow
}

# Check k3s cluster
Write-Host ""
Write-Host "4. Checking k3s cluster..." -ForegroundColor Yellow
try {
    $nodes = kubectl get nodes 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   OK - k3s cluster is running" -ForegroundColor Green
        kubectl get nodes --no-headers 2>&1 | ForEach-Object {
            Write-Host "      $($_)" -ForegroundColor White
        }
    }
    else {
        Write-Host "   WARNING - k3s cluster not accessible" -ForegroundColor Yellow
        Write-Host "   Make sure Kubernetes is enabled in Rancher Desktop" -ForegroundColor White
    }
}
catch {
    Write-Host "   WARNING - Could not check k3s cluster" -ForegroundColor Yellow
}

# Test nerdctl connectivity
Write-Host ""
Write-Host "5. Testing nerdctl connectivity..." -ForegroundColor Yellow
try {
    $containers = nerdctl ps -a 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   OK - nerdctl can connect to containerd" -ForegroundColor Green
        $count = (nerdctl ps -a --format "{{.ID}}" | Measure-Object).Count
        Write-Host "   Running containers: $count" -ForegroundColor White
    }
    else {
        Write-Host "   ERROR - nerdctl cannot connect to containerd!" -ForegroundColor Red
        Write-Host "   Make sure Rancher Desktop is running" -ForegroundColor White
        exit 1
    }
}
catch {
    Write-Host "   ERROR - Error connecting to containerd" -ForegroundColor Red
    exit 1
}

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "SUCCESS - Rancher Desktop configured!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "1. Start infrastructure:" -ForegroundColor White
Write-Host "   Terminal > Run Task > dev-infrastructure" -ForegroundColor Yellow
Write-Host "   OR: nerdctl compose -f infrastructure/docker-compose.dev.yml up -d" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Run backend services:" -ForegroundColor White
Write-Host "   Terminal > Run Task > watch-all-services" -ForegroundColor Yellow
Write-Host ""
Write-Host "3. Run frontend:" -ForegroundColor White
Write-Host "   Terminal > Run Task > dev-frontend" -ForegroundColor Yellow
Write-Host ""
Write-Host "Documentation:" -ForegroundColor Cyan
Write-Host "- RANCHER_DESKTOP_SETUP.md - Complete setup guide" -ForegroundColor White
Write-Host "- .github/copilot-instructions.md - Contributing guide" -ForegroundColor White
Write-Host ""
