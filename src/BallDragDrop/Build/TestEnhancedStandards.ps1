#!/usr/bin/env pwsh

# Simple test script to validate enhanced standards configuration
Write-Host "Testing Enhanced Standards Configuration..." -ForegroundColor Green

# Test 1: Verify coding-standards.json has enhanced settings
$configPath = "../../coding-standards.json"
if (Test-Path $configPath) {
    Write-Host "✓ Configuration file found" -ForegroundColor Green
    
    try {
        $config = Get-Content $configPath -Raw | ConvertFrom-Json
        
        # Check thisQualifier settings
        if ($config.thisQualifier.mandatoryEnforcement -eq $true -and $config.thisQualifier.buildBreaking -eq $true) {
            Write-Host "✓ thisQualifier enhanced settings configured correctly" -ForegroundColor Green
        } else {
            Write-Host "✗ thisQualifier enhanced settings missing or incorrect" -ForegroundColor Red
        }
        
        # Check classFileOrganization settings
        if ($config.classFileOrganization.mandatoryEnforcement -eq $true -and $config.classFileOrganization.buildBreaking -eq $true) {
            Write-Host "✓ classFileOrganization enhanced settings configured correctly" -ForegroundColor Green
        } else {
            Write-Host "✗ classFileOrganization enhanced settings missing or incorrect" -ForegroundColor Red
        }
        
        # Check buildIntegration settings
        if ($config.buildIntegration.enhancedStandardsEnabled -eq $true -and $config.buildIntegration.mandatoryEnforcementEnabled -eq $true) {
            Write-Host "✓ buildIntegration enhanced settings configured correctly" -ForegroundColor Green
        } else {
            Write-Host "✗ buildIntegration enhanced settings missing or incorrect" -ForegroundColor Red
        }
        
    } catch {
        Write-Host "✗ Failed to parse configuration file: $_" -ForegroundColor Red
    }
} else {
    Write-Host "✗ Configuration file not found at $configPath" -ForegroundColor Red
}

# Test 2: Verify MSBuild project has enhanced settings
$projectPath = "BallDragDrop.csproj"
if (Test-Path $projectPath) {
    Write-Host "✓ Project file found" -ForegroundColor Green
    
    $projectContent = Get-Content $projectPath -Raw
    
    if ($projectContent -match "EnforceEnhancedStandards.*true") {
        Write-Host "✓ EnforceEnhancedStandards property configured" -ForegroundColor Green
    } else {
        Write-Host "✗ EnforceEnhancedStandards property missing or incorrect" -ForegroundColor Red
    }
    
    if ($projectContent -match "TreatThisQualifierViolationsAsErrors.*true") {
        Write-Host "✓ TreatThisQualifierViolationsAsErrors property configured" -ForegroundColor Green
    } else {
        Write-Host "✗ TreatThisQualifierViolationsAsErrors property missing or incorrect" -ForegroundColor Red
    }
    
    if ($projectContent -match "TreatClassFileOrganizationViolationsAsErrors.*true") {
        Write-Host "✓ TreatClassFileOrganizationViolationsAsErrors property configured" -ForegroundColor Green
    } else {
        Write-Host "✗ TreatClassFileOrganizationViolationsAsErrors property missing or incorrect" -ForegroundColor Red
    }
} else {
    Write-Host "✗ Project file not found at $projectPath" -ForegroundColor Red
}

# Test 3: Verify MSBuild targets file has enhanced settings
$targetsPath = "Build/CodingStandards.targets"
if (Test-Path $targetsPath) {
    Write-Host "✓ Targets file found" -ForegroundColor Green
    
    $targetsContent = Get-Content $targetsPath -Raw
    
    if ($targetsContent -match "EnforceEnhancedStandards") {
        Write-Host "✓ EnforceEnhancedStandards parameter configured in targets" -ForegroundColor Green
    } else {
        Write-Host "✗ EnforceEnhancedStandards parameter missing from targets" -ForegroundColor Red
    }
    
    if ($targetsContent -match "TreatThisQualifierViolationsAsErrors") {
        Write-Host "✓ TreatThisQualifierViolationsAsErrors property configured in targets" -ForegroundColor Green
    } else {
        Write-Host "✗ TreatThisQualifierViolationsAsErrors property missing from targets" -ForegroundColor Red
    }
} else {
    Write-Host "✗ Targets file not found at $targetsPath" -ForegroundColor Red
}

# Test 4: Verify CI/CD pipeline has enhanced messaging
$ciPath = "../../../.gitlab-ci.yml"
if (Test-Path $ciPath) {
    Write-Host "✓ CI/CD pipeline file found" -ForegroundColor Green
    
    $ciContent = Get-Content $ciPath -Raw
    
    if ($ciContent -match "BUILD-BREAKING errors") {
        Write-Host "✓ CI/CD pipeline has enhanced standards messaging" -ForegroundColor Green
    } else {
        Write-Host "✗ CI/CD pipeline missing enhanced standards messaging" -ForegroundColor Red
    }
} else {
    Write-Host "✗ CI/CD pipeline file not found at $ciPath" -ForegroundColor Red
}

Write-Host "`nEnhanced Standards Configuration Test Complete!" -ForegroundColor Green
Write-Host "If all tests passed, the enhanced standards are properly configured." -ForegroundColor Cyan