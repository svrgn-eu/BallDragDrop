@echo off
echo Running Coding Standards Validation...
echo.

cd /d "%~dp0.."

echo Building project to ensure latest analyzers are available...
dotnet build --no-restore --verbosity quiet

echo.
echo Running MSBuild coding standards validation...
dotnet msbuild -target:RunCodingStandardsValidation -verbosity:normal

echo.
echo Validation complete. Check the output above for results.
echo Report location: bin\Debug\net9.0-windows\CodeQuality\CodeQualityReport.xml

pause