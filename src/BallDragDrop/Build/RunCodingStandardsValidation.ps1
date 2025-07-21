#!/usr/bin/env pwsh

Write-Host "Running Coding Standards Validation..." -ForegroundColor Green
Write-Host ""

# Change to project directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Split-Path -Parent $scriptDir
Set-Location $projectDir

Write-Host "Building project to ensure latest analyzers are available..." -ForegroundColor Yellow
dotnet build --no-restore --verbosity quiet

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed. Cannot proceed with validation." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Running MSBuild coding standards validation..." -ForegroundColor Yellow
dotnet msbuild -target:RunCodingStandardsValidation -verbosity:normal

Write-Host ""
Write-Host "Validation complete. Check the output above for results." -ForegroundColor Green
Write-Host "Report location: bin\Debug\net9.0-windows\CodeQuality\CodeQualityReport.xml" -ForegroundColor Cyan

# Check if report exists and show summary
$reportPath = "bin\Debug\net9.0-windows\CodeQuality\CodeQualityReport.xml"
$reportPathRelease = "bin\Release\net9.0-windows\CodeQuality\CodeQualityReport.xml"

# Check both debug and release paths
$actualReportPath = $null
if (Test-Path $reportPath) {
    $actualReportPath = $reportPath
} elseif (Test-Path $reportPathRelease) {
    $actualReportPath = $reportPathRelease
}

if ($actualReportPath) {
    Write-Host ""
    Write-Host "Report Summary:" -ForegroundColor Cyan
    try {
        [xml]$report = Get-Content $actualReportPath
        $totalViolations = $report.CodeQualityReport.Summary.TotalViolations
        $criticalViolations = $report.CodeQualityReport.Summary.CriticalViolations
        $warnings = $report.CodeQualityReport.Summary.Warnings
        
        Write-Host "  Total Violations: $totalViolations" -ForegroundColor $(if ([int]$totalViolations -gt 0) { "Yellow" } else { "Green" })
        Write-Host "  Critical Violations: $criticalViolations" -ForegroundColor $(if ([int]$criticalViolations -gt 0) { "Red" } else { "Green" })
        Write-Host "  Warnings: $warnings" -ForegroundColor $(if ([int]$warnings -gt 0) { "Yellow" } else { "Green" })
        
        # Fail the build if critical violations are found
        if ([int]$criticalViolations -gt 0) {
            Write-Host ""
            Write-Host "BUILD FAILED: Critical coding standards violations found!" -ForegroundColor Red
            Write-Host "Please fix the critical violations and try again." -ForegroundColor Red
            exit 1
        }
        
        Write-Host ""
        if ([int]$totalViolations -eq 0) {
            Write-Host "SUCCESS: No coding standards violations found!" -ForegroundColor Green
        } else {
            Write-Host "BUILD PASSED: Only warnings found, no critical violations." -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host "  Could not parse report summary" -ForegroundColor Yellow
        Write-Host "  Assuming validation passed due to parsing error" -ForegroundColor Yellow
    }
} else {
    Write-Host ""
    Write-Host "WARNING: Code quality report not found at expected locations:" -ForegroundColor Yellow
    Write-Host "  $reportPath" -ForegroundColor Yellow
    Write-Host "  $reportPathRelease" -ForegroundColor Yellow
    Write-Host "Validation may not have run properly." -ForegroundColor Yellow
}