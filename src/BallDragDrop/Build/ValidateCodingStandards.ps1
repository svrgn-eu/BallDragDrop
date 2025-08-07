param(
    [Parameter(Mandatory=$true)]
    [string]$ProjectPath,
    
    [Parameter(Mandatory=$true)]
    [string]$ConfigPath,
    
    [Parameter(Mandatory=$true)]
    [string]$ReportPath,
    
    [Parameter(Mandatory=$false)]
    [switch]$FailOnCritical,
    
    [Parameter(Mandatory=$false)]
    [switch]$EnforceEnhancedStandards
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

# Function to validate "this" qualifier usage
function Test-ThisQualifier {
    param(
        [string]$ProjectPath,
        [object]$Config,
        [bool]$EnforceEnhancedStandards = $true
    )
    
    $violations = @()
    
    if ($Config.thisQualifier -and $Config.thisQualifier.enforceThisQualifier) {
        $csFiles = Get-ChildItem -Path $ProjectPath -Filter "*.cs" -Recurse | Where-Object { 
            $_.FullName -notmatch "\\bin\\" -and $_.FullName -notmatch "\\obj\\" -and
            $_.Name -notmatch "\.Designer\.cs$" -and $_.Name -notmatch "\.g\.cs$"
        }
        
        foreach ($file in $csFiles) {
            $content = Get-Content $file.FullName -Raw
            $relativePath = $file.FullName.Substring($ProjectPath.Length + 1)
            
            # Look for instance member access without "this."
            # This is a simplified check - the actual analyzer would be more sophisticated
            $lines = $content -split "`n"
            for ($i = 0; $i -lt $lines.Count; $i++) {
                $line = $lines[$i].Trim()
                
                # Skip comments, strings, and static contexts
                if ($line -match "^\s*//|^\s*/\*|^\s*\*|static\s+") {
                    continue
                }
                
                # Look for property/method access patterns that might need "this."
                if ($line -match "\b[A-Z][a-zA-Z0-9]*\s*[\(\.]" -and $line -notmatch "this\." -and $line -notmatch "base\." -and $line -notmatch "new\s+" -and $line -notmatch "typeof\s*\(" -and $line -notmatch "nameof\s*\(") {
                    # This is a basic heuristic - actual implementation would need more sophisticated parsing
                    $violations += @{
                        Type = "ThisQualifier"
                        Severity = $Config.thisQualifier.enforcementLevel
                        Message = "Instance member access should use 'this.' qualifier (Line $($i + 1))"
                        File = $relativePath
                    }
                    break # Only report once per file to avoid spam
                }
            }
        }
    }
    
    return $violations
}

# Function to validate class file organization
function Test-ClassFileOrganization {
    param(
        [string]$ProjectPath,
        [object]$Config,
        [bool]$EnforceEnhancedStandards = $true
    )
    
    $violations = @()
    
    if ($Config.classFileOrganization) {
        $csFiles = Get-ChildItem -Path $ProjectPath -Filter "*.cs" -Recurse | Where-Object { 
            $_.FullName -notmatch "\\bin\\" -and $_.FullName -notmatch "\\obj\\" -and
            $_.Name -notmatch "\.Designer\.cs$" -and $_.Name -notmatch "\.g\.cs$"
        }
        
        foreach ($file in $csFiles) {
            $content = Get-Content $file.FullName -Raw
            $relativePath = $file.FullName.Substring($ProjectPath.Length + 1)
            $fileName = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
            
            # Check for multiple classes in one file
            if ($Config.classFileOrganization.enforceOneClassPerFile) {
                # More precise regex to avoid false positives from comments
                $classMatches = [regex]::Matches($content, "^\s*(?:public|internal|private|protected)?\s*(?:static\s+)?(?:partial\s+)?class\s+(\w+)", [System.Text.RegularExpressions.RegexOptions]::Multiline)
                
                if ($classMatches.Count -gt 1) {
                    $classNames = $classMatches | ForEach-Object { $_.Groups[1].Value }
                    $violations += @{
                        Type = "MultipleClasses"
                        Severity = $Config.classFileOrganization.enforcementLevel
                        Message = "File contains multiple classes: $($classNames -join ', '). Each class should be in its own file."
                        File = $relativePath
                    }
                }
            }
            
            # Check filename matches class name
            if ($Config.classFileOrganization.enforceFilenameMatchesClassName) {
                # Skip XAML code-behind files if configured to allow them
                $isXamlCodeBehind = $false
                if ($Config.classFileOrganization.allowWpfXamlCodeBehind -and $Config.classFileOrganization.wpfCodeBehindExtensions) {
                    foreach ($extension in $Config.classFileOrganization.wpfCodeBehindExtensions) {
                        if ($file.Name.EndsWith($extension, [StringComparison]::OrdinalIgnoreCase)) {
                            $isXamlCodeBehind = $true
                            break
                        }
                    }
                }
                
                # Also check for .xaml.cs pattern specifically
                if ($file.Name -match "\.xaml\.cs$") {
                    $isXamlCodeBehind = $true
                }
                
                if (-not $isXamlCodeBehind) {
                    # More precise regex to find the primary class
                    $primaryClassMatch = [regex]::Match($content, "^\s*(?:public|internal)?\s*(?:static\s+)?(?:partial\s+)?class\s+(\w+)", [System.Text.RegularExpressions.RegexOptions]::Multiline)
                    
                    if ($primaryClassMatch.Success) {
                        $className = $primaryClassMatch.Groups[1].Value
                        if ($fileName -ne $className) {
                            $violations += @{
                                Type = "FilenameClassMismatch"
                                Severity = $Config.classFileOrganization.enforcementLevel
                                Message = "Filename '$fileName.cs' does not match class name '$className'"
                                File = $relativePath
                            }
                        }
                    }
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
Write-Host "Enhanced Standards Enforcement: $($EnforceEnhancedStandards.IsPresent)" -ForegroundColor $(if ($EnforceEnhancedStandards.IsPresent) { "Green" } else { "Yellow" })

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

Write-Host "Validating 'this' qualifier usage..." -ForegroundColor Yellow
$thisQualifierViolations = Test-ThisQualifier -ProjectPath $ProjectPath -Config $config -EnforceEnhancedStandards $EnforceEnhancedStandards.IsPresent
$allViolations += $thisQualifierViolations

Write-Host "Validating class file organization..." -ForegroundColor Yellow
$classFileOrgViolations = Test-ClassFileOrganization -ProjectPath $ProjectPath -Config $config -EnforceEnhancedStandards $EnforceEnhancedStandards.IsPresent
$allViolations += $classFileOrgViolations

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
if ($summary.CriticalViolations -gt 0 -and $FailOnCritical.IsPresent) {
    Write-Host "`nBuild should fail due to critical violations!" -ForegroundColor Red
    exit 1
} else {
    exit 0
}