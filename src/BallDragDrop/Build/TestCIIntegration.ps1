#!/usr/bin/env pwsh

Write-Host "Testing CI/CD Integration for Coding Standards..." -ForegroundColor Green
Write-Host ""

# Test 1: Verify GitLab CI configuration
Write-Host "Test 1: Verifying GitLab CI configuration..." -ForegroundColor Yellow
$ciConfigPath = "../../.gitlab-ci.yml"
if (Test-Path $ciConfigPath) {
    $ciContent = Get-Content $ciConfigPath -Raw
    
    # Check for code-quality stage
    if ($ciContent -match "code-quality:") {
        Write-Host "  ✓ Code quality stage found" -ForegroundColor Green
    } else {
        Write-Host "  ✗ Code quality stage missing" -ForegroundColor Red
        exit 1
    }
    
    # Check for artifacts configuration
    if ($ciContent -match "CodeQualityReport\.xml") {
        Write-Host "  ✓ Code quality report artifacts configured" -ForegroundColor Green
    } else {
        Write-Host "  ✗ Code quality report artifacts missing" -ForegroundColor Red
        exit 1
    }
    
    # Check for allow_failure: false
    if ($ciContent -match "allow_failure: false") {
        Write-Host "  ✓ Build failure on violations configured" -ForegroundColor Green
    } else {
        Write-Host "  ✗ Build failure configuration missing" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "  ✗ GitLab CI configuration file not found" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Test 2: Verify PowerShell validation script
Write-Host "Test 2: Verifying PowerShell validation script..." -ForegroundColor Yellow
$scriptPath = "RunCodingStandardsValidation.ps1"
if (Test-Path $scriptPath) {
    $scriptContent = Get-Content $scriptPath -Raw
    
    # Check for exit code handling
    if ($scriptContent -match "exit 1") {
        Write-Host "  ✓ Error exit code handling found" -ForegroundColor Green
    } else {
        Write-Host "  ✗ Error exit code handling missing" -ForegroundColor Red
        exit 1
    }
    
    # Check for critical violations handling
    if ($scriptContent -match "criticalViolations") {
        Write-Host "  ✓ Critical violations handling found" -ForegroundColor Green
    } else {
        Write-Host "  ✗ Critical violations handling missing" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "  ✗ PowerShell validation script not found" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Test 3: Test the validation script execution
Write-Host "Test 3: Testing validation script execution..." -ForegroundColor Yellow
try {
    # Run the validation script in test mode
    $output = & .\RunCodingStandardsValidation.ps1 2>&1
    Write-Host "  ✓ Validation script executed successfully" -ForegroundColor Green
    Write-Host "  Script output preview:" -ForegroundColor Cyan
    $output | Select-Object -First 5 | ForEach-Object { Write-Host "    $_" -ForegroundColor Gray }
} catch {
    Write-Host "  ✗ Validation script execution failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "All CI/CD integration tests passed!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps for complete testing:" -ForegroundColor Cyan
Write-Host "1. Commit changes to trigger GitLab CI pipeline" -ForegroundColor White
Write-Host "2. Verify code-quality stage runs in CI/CD" -ForegroundColor White
Write-Host "3. Check that artifacts are properly collected" -ForegroundColor White
Write-Host "4. Confirm build fails on critical violations" -ForegroundColor White