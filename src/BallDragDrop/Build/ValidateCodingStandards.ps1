param(
    [Parameter(Mandatory=$true)]
    [string]$ProjectPath,
    
    [Parameter(Mandatory=$true)]
    [string]$ConfigPath,
    
    [Parameter(Mandatory=$true)]
    [string]$ReportPath,
    
    [Parameter(Mandatory=$false)]
    [bool]$FailOnCritical = $true
)

# Function to read JSON configuration
function Read-CodingStandardsConfig {
    param([string]$ConfigPath)
    
    if (-not (Test-Path $ConfigPath)) {
        Write-Warning "Coding standards configuration file not found: $ConfigPath"
        return $null
    }
    
    try {
        $config = Get-Content $ConfigPath -Raw | ConvertFrom-Json
        return $config
    }
    catch {
        Write-Error "Failed to parse coding standards configuration: $_"
        return $null
    }
}

# Function to validate folder structure
function Test-FolderStructure {
    param(
        [string]$ProjectPath,
        [object]$Config
    )
    
    $violations = @()
    
    if ($Config.folderStructure) {
        $requiredFolders = $Config.folderStructure.requiredFolders
        $fileMapping = $Config.folderStructure.fileTypeToFolderMapping
        
        # Check if required folders exist
        foreach ($folder in $requiredFolders) {
            $folderPath = Join-Path $ProjectPath $folder
            if (-not (Test-Path $folderPath)) {
                $violations += @{
                    Type = "MissingFolder"
                    Severity = $Config.folderStructure.enforcementLevel
                    Message = "Required folder '$folder' is missing"
                    File = $folder
                }
            }
        }
        
        # Check file placement
        $csFiles = Get-ChildItem -Path $ProjectPath -Filter "*.cs" -Recurse | Where-Object { 
            $_.FullName -notmatch "\\bin\\" -and $_.FullName -notmatch "\\obj\\" 
        }
        
        foreach ($file in $csFiles) {
            $content = Get-Content $file.FullName -Raw
            $relativePath = $file.FullName.Substring($ProjectPath.Length + 1)
            
            # Check interfaces
            if ($content -match "interface\s+\w+") {
                if ($relativePath -notmatch "Contracts") {
                    $violations += @{
                        Type = "InterfacePlacement"
                        Severity = $Config.folderStructure.enforcementLevel
                        Message = "Interface file should be in Contracts folder"
                        File = $relativePath
                    }
                }
            }
            
            # Check bootstrappers
            if ($file.Name -match "Bootstrapper") {
                if ($relativePath -notmatch "Bootstrapper") {
                    $violations += @{
                        Type = "BootstrapperPlacement"
                        Severity = $Config.folderStructure.enforcementLevel
                        Message = "Bootstrapper file should be in Bootstrapper folder"
                        File = $relativePath
                    }
                }
            }
        }
    }
    
    return $violations
}

# Function to validate method regions
function Test-MethodRegions {
    param(
        [string]$ProjectPath,
        [object]$Config
    )
    
    $violations = @()
    
    if ($Config.methodRegions -and $Config.methodRegions.enforceRegions) {
        $csFiles = Get-ChildItem -Path $ProjectPath -Filter "*.cs" -Recurse | Where-Object { 
            $_.FullName -notmatch "\\bin\\" -and $_.FullName -notmatch "\\obj\\" -and
            $_.Name -notmatch "\.Designer\.cs$" -and $_.Name -notmatch "\.g\.cs$"
        }
        
        foreach ($file in $csFiles) {
            $content = Get-Content $file.FullName -Raw
            $relativePath = $file.FullName.Substring($ProjectPath.Length + 1)
            
            # Check for methods without regions
            $methodMatches = [regex]::Matches($content, "(?:public|private|protected|internal)\s+(?:static\s+)?(?:virtual\s+)?(?:override\s+)?(?:\w+\s+)*\w+\s*\([^)]*\)\s*{")
            
            foreach ($match in $methodMatches) {
                $methodStart = $match.Index
                $beforeMethod = $content.Substring(0, $methodStart)
                $lastRegionIndex = $beforeMethod.LastIndexOf("#region")
                $lastEndRegionIndex = $beforeMethod.LastIndexOf("#endregion")
                
                # If there's no region or the last endregion is after the last region, method is not in a region
                if ($lastRegionIndex -eq -1 -or ($lastEndRegionIndex -ne -1 -and $lastEndRegionIndex -gt $lastRegionIndex)) {
                    $violations += @{
                        Type = "MethodRegion"
                        Severity = $Config.methodRegions.enforcementLevel
                        Message = "Method should be enclosed in a region"
                        File = $relativePath
                    }
                    break # Only report once per file
                }
            }
        }
    }
    
    return $violations
}

# Function to validate class regions
function Test-ClassRegions {
    param(
        [string]$ProjectPath,
        [object]$Config
    )
    
    $violations = @()
    
    if ($Config.classRegions -and $Config.classRegions.enforceClassRegions) {
        $requiredRegions = $Config.classRegions.requiredRegions
        
        $csFiles = Get-ChildItem -Path $ProjectPath -Filter "*.cs" -Recurse | Where-Object { 
            $_.FullName -notmatch "\\bin\\" -and $_.FullName -notmatch "\\obj\\" -and
            $_.Name -notmatch "\.Designer\.cs$" -and $_.Name -notmatch "\.g\.cs$"
        }
        
        foreach ($file in $csFiles) {
            $content = Get-Content $file.FullName -Raw
            $relativePath = $file.FullName.Substring($ProjectPath.Length + 1)
            
            # Check if file contains a class
            if ($content -match "class\s+\w+") {
                $missingRegions = @()
                
                foreach ($region in $requiredRegions) {
                    if ($content -notmatch "#region\s+$region") {
                        $missingRegions += $region
                    }
                }
                
                if ($missingRegions.Count -gt 0) {
                    $violations += @{
                        Type = "ClassRegions"
                        Severity = $Config.classRegions.enforcementLevel
                        Message = "Missing required regions: $($missingRegions -join ', ')"
                        File = $relativePath
                    }
                }
            }
        }
    }
    
    return $violations
}

# Function to validate XML documentation
function Test-XmlDocumentation {
    param(
        [string]$ProjectPath,
        [object]$Config
    )
    
    $violations = @()
    
    if ($Config.xmlDocumentation -and $Config.xmlDocumentation.enforceDocumentation) {
        $csFiles = Get-ChildItem -Path $ProjectPath -Filter "*.cs" -Recurse | Where-Object { 
            $_.FullName -notmatch "\\bin\\" -and $_.FullName -notmatch "\\obj\\" -and
            $_.Name -notmatch "\.Designer\.cs$" -and $_.Name -notmatch "\.g\.cs$"
        }
        
        foreach ($file in $csFiles) {
            $content = Get-Content $file.FullName -Raw
            $relativePath = $file.FullName.Substring($ProjectPath.Length + 1)
            
            # Find public methods
            $publicMethodMatches = [regex]::Matches($content, "public\s+(?:static\s+)?(?:virtual\s+)?(?:override\s+)?(?:\w+\s+)*\w+\s*\([^)]*\)\s*{")
            
            foreach ($match in $publicMethodMatches) {
                $methodStart = $match.Index
                $beforeMethod = $content.Substring(0, $methodStart)
                
                # Check for XML documentation before the method
                if ($beforeMethod -notmatch "///\s*<summary>" -or $beforeMethod.LastIndexOf("///") -eq -1) {
                    $violations += @{
                        Type = "XmlDocumentation"
                        Severity = $Config.xmlDocumentation.enforcementLevel
                        Message = "Public method missing XML documentation"
                        File = $relativePath
                    }
                    break # Only report once per file
                }
            }
        }
    }
    
    return $violations
}

# Function to generate report
function New-QualityReport {
    param(
        [array]$AllViolations,
        [string]$ReportPath,
        [string]$ProjectName
    )
    
    $reportDir = Split-Path $ReportPath -Parent
    if (-not (Test-Path $reportDir)) {
        New-Item -ItemType Directory -Path $reportDir -Force | Out-Null
    }
    
    $report = @"
<?xml version="1.0" encoding="utf-8"?>
<CodeQualityReport>
  <ProjectName>$ProjectName</ProjectName>
  <Timestamp>$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')</Timestamp>
  <Violations>
"@
    
    $groupedViolations = $AllViolations | Group-Object Type
    
    foreach ($group in $groupedViolations) {
        $report += "`n    <ViolationType name=`"$($group.Name)`">"
        foreach ($violation in $group.Group) {
            $report += "`n      <Violation severity=`"$($violation.Severity)`">"
            $report += "`n        <File>$($violation.File)</File>"
            $report += "`n        <Message>$($violation.Message)</Message>"
            $report += "`n      </Violation>"
        }
        $report += "`n    </ViolationType>"
    }
    
    $criticalCount = ($AllViolations | Where-Object { $_.Severity -eq "error" }).Count
    $warningCount = ($AllViolations | Where-Object { $_.Severity -eq "warning" }).Count
    
    $report += @"

  </Violations>
  <Summary>
    <TotalViolations>$($AllViolations.Count)</TotalViolations>
    <CriticalViolations>$criticalCount</CriticalViolations>
    <Warnings>$warningCount</Warnings>
    <BuildStatus>$(if ($criticalCount -gt 0 -and $FailOnCritical) { "Failed" } else { "Success" })</BuildStatus>
  </Summary>
</CodeQualityReport>
"@
    
    $report | Out-File -FilePath $ReportPath -Encoding UTF8
    
    return @{
        TotalViolations = $AllViolations.Count
        CriticalViolations = $criticalCount
        Warnings = $warningCount
    }
}

# Main execution
Write-Host "Starting Coding Standards Validation..." -ForegroundColor Green
Write-Host "Project Path: $ProjectPath"
Write-Host "Config Path: $ConfigPath"
Write-Host "Report Path: $ReportPath"

# Read configuration
$config = Read-CodingStandardsConfig -ConfigPath $ConfigPath
if (-not $config) {
    Write-Error "Failed to load coding standards configuration"
    exit 1
}

# Run validations
$allViolations = @()

Write-Host "Validating folder structure..." -ForegroundColor Yellow
$folderViolations = Test-FolderStructure -ProjectPath $ProjectPath -Config $config
$allViolations += $folderViolations

Write-Host "Validating method regions..." -ForegroundColor Yellow
$methodRegionViolations = Test-MethodRegions -ProjectPath $ProjectPath -Config $config
$allViolations += $methodRegionViolations

Write-Host "Validating class regions..." -ForegroundColor Yellow
$classRegionViolations = Test-ClassRegions -ProjectPath $ProjectPath -Config $config
$allViolations += $classRegionViolations

Write-Host "Validating XML documentation..." -ForegroundColor Yellow
$xmlDocViolations = Test-XmlDocumentation -ProjectPath $ProjectPath -Config $config
$allViolations += $xmlDocViolations

# Generate report
$projectName = Split-Path $ProjectPath -Leaf
$summary = New-QualityReport -AllViolations $allViolations -ReportPath $ReportPath -ProjectName $projectName

# Output results
Write-Host "`nCoding Standards Validation Complete!" -ForegroundColor Green
Write-Host "Total Violations: $($summary.TotalViolations)" -ForegroundColor $(if ($summary.TotalViolations -gt 0) { "Yellow" } else { "Green" })
Write-Host "Critical Violations: $($summary.CriticalViolations)" -ForegroundColor $(if ($summary.CriticalViolations -gt 0) { "Red" } else { "Green" })
Write-Host "Warnings: $($summary.Warnings)" -ForegroundColor $(if ($summary.Warnings -gt 0) { "Yellow" } else { "Green" })
Write-Host "Report generated: $ReportPath" -ForegroundColor Cyan

# Display violations
if ($allViolations.Count -gt 0) {
    Write-Host "`nViolations found:" -ForegroundColor Yellow
    foreach ($violation in $allViolations) {
        $color = if ($violation.Severity -eq "error") { "Red" } else { "Yellow" }
        Write-Host "  [$($violation.Severity.ToUpper())] $($violation.File): $($violation.Message)" -ForegroundColor $color
    }
}

# Exit with appropriate code
if ($summary.CriticalViolations -gt 0 -and $FailOnCritical) {
    Write-Host "`nBuild should fail due to critical violations!" -ForegroundColor Red
    exit 1
} else {
    exit 0
}