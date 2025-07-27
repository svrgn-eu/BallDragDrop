# Implementation Plan

- [x] 0. Full implementation without questions asked





    - [x] 1. Create core status bar infrastructure


    - Create StatusBarViewModel class with INotifyPropertyChanged implementation
    - Implement basic properties for CurrentFps, AverageFps, AssetName, and StatusText
    - Add constructor with dependency injection for required services
    - _Requirements: 5.1, 5.3_

    - [x] 2. Implement FPS calculation utilities


    - Create FpsReading struct to store FPS data with timestamps
    - Implement FpsCalculator class with 10-second rolling average calculation
    - Add methods for adding FPS readings and calculating averages
    - Include validation for invalid FPS values and edge cases
    - _Requirements: 1.1, 1.2, 1.4_

    - [x] 3. Extend PerformanceMonitor service for status bar integration


    - Add CurrentFps property to PerformanceMonitor class
    - Implement FpsUpdated event for real-time FPS notifications
    - Modify existing metrics calculation to support status bar requirements
    - Ensure thread-safe access to FPS data
    - _Requirements: 1.1, 1.3, 5.4_

    - [x] 4. Add status bar UI to MainWindow


    - Modify MainWindow.xaml to include StatusBar control in DockPanel
    - Create StatusBarItem elements for asset name, status text, and FPS display
    - Implement proper data binding to StatusBarViewModel properties
    - Ensure status bar positioning at bottom of window
    - _Requirements: 4.1, 4.2, 4.4_

    - [x] 5. Integrate StatusBarViewModel with MainWindow


    - Add StatusBarViewModel property to MainWindow code-behind
    - Initialize StatusBarViewModel with required dependencies in MainWindow constructor
    - Set up data context binding for status bar
    - Implement proper disposal of StatusBarViewModel resources
    - _Requirements: 4.4, 5.1, 5.3_

    - [x] 6. Connect PerformanceMonitor to StatusBarViewModel


    - Subscribe to PerformanceMonitor FpsUpdated events in StatusBarViewModel
    - Implement FPS data processing and property updates
    - Add FpsCalculator integration for 10-second average calculation
    - Ensure UI thread marshaling for property change notifications
    - _Requirements: 1.1, 1.2, 1.3, 5.2, 5.4_

    - [x] 7. Add asset name functionality to BallViewModel


    - Add AssetName property to BallViewModel class
    - Implement asset name extraction from file paths in LoadBallVisualAsync method
    - Add property change notification for AssetName updates
    - Handle default "No Asset" state when no asset is loaded
    - _Requirements: 2.1, 2.2, 2.3_

    - [x] 8. Connect BallViewModel asset data to StatusBarViewModel


    - Add BallViewModel dependency to StatusBarViewModel constructor
    - Subscribe to BallViewModel PropertyChanged events for AssetName updates
    - Implement asset name processing with truncation for long names
    - Handle null or empty asset names with appropriate defaults
    - _Requirements: 2.1, 2.2, 2.3, 2.4_

- [x] 1. Error handling and tests





    - [x] 9. Implement error handling and edge cases


    - Add validation for FPS values in FpsCalculator (filter invalid values)
    - Implement fallback display values ("--") when data is unavailable
    - Add exception handling for timer failures and threading issues
    - Ensure graceful degradation when services are unavailable
    - _Requirements: 1.4, 2.2, 2.4_

    - [x] 10. Add comprehensive unit tests for StatusBarViewModel


    - Create test class for StatusBarViewModel with mock dependencies
    - Test property change notifications for all properties
    - Verify FPS value formatting and display logic
    - Test asset name handling including truncation and defaults
    - _Requirements: 5.1, 5.3_

    - [x] 11. Add unit tests for FpsCalculator


    - Create test class for FpsCalculator functionality
    - Test 10-second rolling average calculation with various data sets
    - Verify edge cases like empty data and single readings
    - Test time-based data expiration and cleanup
    - _Requirements: 1.1, 1.2, 1.4_

    - [ ] 12. Add integration tests for PerformanceMonitor extensions
    - Create tests for new FpsUpdated event functionality
    - Verify thread safety of FPS data access
    - Test integration with existing PerformanceMonitor metrics
    - Validate performance impact of status bar integration
    - _Requirements: 1.1, 1.3, 5.4_

    - [ ] 13. Create UI integration tests for status bar
    - Test status bar visibility and positioning in MainWindow
    - Verify data binding functionality with live data
    - Test window resize behavior and status bar responsiveness
    - Validate real-time FPS updates in UI
    - _Requirements: 4.1, 4.2, 4.3, 4.4_

    - [ ] 14. Implement performance optimization and validation
    - Add FPS update throttling to 10 Hz for optimal performance
    - Implement circular buffer for efficient FPS history management
    - Add memory usage validation and cleanup
    - Verify no performance impact on existing ball animation
    - _Requirements: 1.3, 5.4_

    - [ ] 15. Add final integration and polish
    - Test complete status bar functionality with all features enabled
    - Verify proper initialization and cleanup during application lifecycle
    - Add logging for debugging and monitoring status bar operations
    - Ensure consistent behavior across different usage scenarios
    - _Requirements: 4.4, 5.1, 5.2, 5.3_