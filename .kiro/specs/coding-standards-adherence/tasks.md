# Implementation Plan

- [x] 1. Set up basic configuration infrastructure

  - Create .editorconfig file with C# formatting rules for the project root
  - Create coding-standards.json configuration file for custom rules
  - Add standard Roslyn analyzer packages to BallDragDrop.csproj
  - _Requirements: 1.1, 1.2, 7.1, 7.3_

- [x] 2. Create project structure folders and reorganize existing files

  - Create Contracts subfolder in BallDragDrop project
  - Create Bootstrapper subfolder in BallDragDrop project  
  - Move ServiceBootstrapper.cs from Services to Bootstrapper folder
  - Update namespace and references for moved ServiceBootstrapper file
  - _Requirements: 3.1, 3.2, 3.3_

- [x] 3. Set up custom analyzer project infrastructure

  - Create BallDragDrop.CodeAnalysis class library project
  - Add Microsoft.CodeAnalysis.Analyzers and Microsoft.CodeAnalysis.CSharp packages
  - Create base analyzer infrastructure and diagnostic descriptor definitions
  - _Requirements: 3.1, 4.1, 5.1, 6.1_

- [x] 4. Implement folder structure validation analyzer
  - Create FolderStructureAnalyzer class that validates file placement
  - Implement logic to check interfaces and abstract classes are in Contracts folder
  - Implement logic to check bootstrapper files are in Bootstrapper folder
  - Add diagnostic reporting for folder structure violations
  - Write unit tests for FolderStructureAnalyzer
  - _Requirements: 3.1, 3.2, 3.3, 3.4_

- [x] 5. Implement method region enforcement analyzer
  - Create MethodRegionAnalyzer class that validates method regions
  - Implement logic to detect methods not enclosed in regions
  - Implement validation of region naming format "#region MethodName"
  - Add diagnostic reporting for region violations
  - Write unit tests for MethodRegionAnalyzer
  - _Requirements: 4.1, 4.2, 4.3, 4.4_

- [x] 6. Implement XML documentation validation analyzer
  - Create XmlDocumentationAnalyzer class for documentation validation
  - Implement logic to check for missing XML documentation on public methods
  - Implement validation of exception documentation completeness
  - Implement checks for missing summary, parameters, and return value documentation
  - Add diagnostic reporting for documentation violations
  - Write unit tests for XmlDocumentationAnalyzer
  - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_

- [x] 7. Create code fix providers for automatic violation resolution
  - Implement CodeFixProvider for folder structure violations
  - Implement CodeFixProvider for method region violations
  - Implement CodeFixProvider for XML documentation violations
  - Write unit tests for all code fix providers
  - _Requirements: 2.5, 3.3, 4.2, 5.4_

- [x] 8. Integrate analyzers with main project
  - Add project reference from BallDragDrop to BallDragDrop.CodeAnalysis
  - Configure analyzer assembly loading in BallDragDrop.csproj
  - Test analyzer integration in Visual Studio IDE
  - Verify real-time diagnostic feedback is working
  - _Requirements: 1.2, 6.1, 6.2_

- [x] 9. Implement MSBuild integration for build-time validation
  - Create custom MSBuild targets for pre-build standards validation
  - Configure build failure conditions for critical violations
  - Implement code quality report generation during build
  - Add MSBuild integration to BallDragDrop.csproj
  - _Requirements: 1.3, 8.2, 8.3_

- [x] 10. Update CI/CD pipeline for standards enforcement
  - Modify .gitlab-ci.yml to include coding standards validation
  - Configure build failure on critical standards violations
  - Add code quality report artifacts to CI/CD pipeline
  - Test complete CI/CD integration with standards enforcement
  - _Requirements: 8.1, 8.2, 8.4_
- [x] 11. Apply standards to existing codebase
  - Run analyzers on existing code to identify violations
  - Create regions around all existing methods in the codebase
  - Add comprehensive XML documentation to all public methods
  - Move any interfaces to Contracts folder if they exist
  - Fix all identified naming convention violations
  - _Requirements: 1.1, 2.1, 2.2, 2.3, 2.4, 4.1, 5.1_
- [x] 12. Organize existing codebase with proper regions
  - Add "Properties", "Construction", and "Methods" regions to all class files
  - Ensure all regions have named beginning and end tags (e.g., "#region Properties" and "#endregion Properties")
  - Move existing properties, constructors, and methods into their respective regions
  - Apply region organization to ServiceBootstrapper, BallViewModel, and all other classes
  - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5_
- [x] 13. Create comprehensive test suite for standards enforcement
  - Write integration tests for complete standards validation workflow
  - Create test projects with various violation scenarios
  - Test analyzer performance with large codebases
  - Validate error reporting and fix suggestions work correctly
  - _Requirements: 1.4, 2.5, 6.4, 6.5_

- [-] 20250801 Enhancements
  - [x] 14. Enhance ThisQualifierAnalyzer for mandatory enforcement
    - Update ThisQualifierAnalyzer to report violations as errors instead of warnings
    - Implement build-breaking behavior when "this" qualifier is missing
    - Add comprehensive detection for all instance member access patterns
    - Create code fix provider for automatic "this." qualifier insertion
    - Write unit tests for mandatory "this" qualifier enforcement
    - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.6, 10.7_

  - [x] 15. Implement ClassFileOrganizationAnalyzer for one class per file
    - Create ClassFileOrganizationAnalyzer class for file organization validation
    - Implement logic to detect multiple classes in a single file

    - Implement filename-to-classname matching validation
    - Add support for nested classes and partial classes exceptions
    - Add diagnostic reporting for file organization violations
    - Write unit tests for ClassFileOrganizationAnalyzer
    - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5, 11.6_

  - [x] 16. Create code fix providers for class file organization
    - Implement CodeFixProvider for splitting multiple classes into separate files
    - Implement CodeFixProvider for renaming files to match class names
    - Add automatic file creation and class extraction functionality
    - Write unit tests for class file organization code fix providers
    - _Requirements: 11.7_

  - [x] 17. Apply enhanced standards to existing codebase
    - Audit existing codebase for missing "this" qualifiers and add them
    - Identify any files with multiple classes and split them into separate files
    - Ensure all filenames match their contained class names exactly
    - Verify all instance member access uses mandatory "this." qualifier
    - _Requirements: 10.1, 10.2, 10.3, 11.1, 11.3_

  - [x] 18. Update configuration and integration for enhanced standards
    - Update coding-standards.json to include new mandatory enforcement settings
    - Configure MSBuild integration to treat new violations as build-breaking errors
    - Update CI/CD pipeline to enforce the enhanced standards
    - Test complete integration with enhanced error-level enforcement
    - _Requirements: 10.7, 11.2, 11.7_

  - [x] 19. Implement WPF XAML code-behind file exception handling
    - Update ClassFileOrganizationAnalyzer to detect WPF XAML code-behind files
    - Implement logic to allow ".xaml.cs" files to contain classes matching the base filename
    - Add validation to check for corresponding ".xaml" file existence
    - Update filename matching rules to exempt WPF code-behind files from strict matching
    - Write unit tests for WPF XAML code-behind file handling
    - _Requirements: 11.7, 11.8_