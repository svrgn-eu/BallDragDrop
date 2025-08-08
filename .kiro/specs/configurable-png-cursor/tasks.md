# Implementation Plan
- [-] 0. do it all at once



  - [ ] 1. Create core hand state machine and cursor infrastructure


    - [ ] 1.1 Define hand state machine interfaces and enums
      - Create IHandStateMachine interface with state management methods
      - Define HandState enum (Default, Hover, Grabbing, Releasing)
      - Define HandTrigger enum for state transitions
      - Create HandStateChangedEventArgs for state change events
      - _Requirements: 3.1, 3.2, 3.3, 3.4_

    - [ ] 1.2 Create cursor service interface and configuration model
      - Define ICursorService interface with hand state-based cursor management
      - Create CursorConfiguration model class with PNG file paths for each hand state
      - Add cursor configuration section to appsettings.json structure
      - _Requirements: 2.1, 2.2_

  - [ ] 2. Implement PNG image loading and cursor conversion
    - [ ] 2.1 Create CursorImageLoader class for PNG processing
      - Implement LoadPngAsCursor method to load PNG files and convert to WPF Cursor objects
      - Add ResizeImage method to standardize all cursor images to 30x30 pixels
      - Include ConvertBitmapToCursor method for bitmap to cursor conversion
      - _Requirements: 1.3, 5.1, 5.2_

    - [ ] 2.2 Add error handling for image loading failures
      - Implement try-catch blocks for file loading operations
      - Add logging for PNG loading errors and fallback behavior
      - Create fallback mechanisms when PNG files are corrupted or missing
      - _Requirements: 5.1, 5.2, 5.3_

  - [ ] 3. Implement hand state machine
    - [ ] 3.1 Create HandStateMachine class implementing IHandStateMachine
      - Implement state machine using Stateless library with HandState and HandTrigger
      - Add Fire method for triggering hand state transitions
      - Create CanFire method for validating state transitions
      - Implement Reset method to return to Default hand state
      - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

    - [ ] 3.2 Add ball state observer integration to hand state machine
      - Implement IBallStateObserver in HandStateMachine class
      - Add OnStateChanged method to respond to ball state changes
      - Create logic to trigger appropriate hand state transitions based on ball states
      - _Requirements: 3.1, 3.5_

    - [ ] 3.3 Implement mouse event handling for hand state transitions
      - Add methods to handle mouse over/leave ball events (MouseOverBall/MouseLeaveBall triggers)
      - Implement drag start/stop event handling (StartGrabbing/StopGrabbing triggers)
      - Create releasing state timer for brief Releasing state duration
      - _Requirements: 3.2, 3.3, 3.4_

  - [ ] 4. Implement cursor management service
    - [ ] 4.1 Create CursorManager class implementing ICursorService
      - Implement SetCursorForHandState method to apply cursors based on hand state
      - Create cursor caching system using Dictionary<HandState, Cursor> for performance
      - Add GetCurrentCursorState method for debugging and status reporting
      - _Requirements: 1.1, 1.2, 4.3_

    - [ ] 4.2 Add configuration reload and cursor cache management
      - Implement ReloadConfigurationAsync method for dynamic configuration updates
      - Add cache invalidation when configuration changes
      - Include proper disposal of cached cursor resources
      - _Requirements: 2.2, 4.3_

    - [ ] 4.3 Implement cursor update debouncing for performance
      - Add EventThrottler class to prevent cursor flickering during rapid hand state changes
      - Implement 16ms debounce timing to maintain 60fps performance
      - Create thread-safe cursor update operations
      - _Requirements: 4.1, 4.2_

  - [ ] 5. Integrate hand state machine with cursor service
    - [ ] 5.1 Connect hand state machine to cursor service
      - Register hand state machine as observer of its own state changes
      - Implement automatic cursor updates when hand state changes occur
      - Add hand state change event handling to trigger cursor updates
      - _Requirements: 1.1, 1.2, 3.1, 3.2, 3.3, 3.4, 3.5_

    - [ ] 5.2 Add hand state machine to ball state machine observer list
      - Register HandStateMachine as IBallStateObserver with ball state machine
      - Ensure hand state machine receives ball state change notifications
      - Test integration between ball state changes and hand state transitions
      - _Requirements: 3.1, 3.5_

  - [ ] 6. Enhance MainWindow for hand state machine integration
    - [ ] 6.1 Add hand state machine integration to MainWindow
      - Inject IHandStateMachine into MainWindow constructor
      - Initialize hand state machine in MainWindow initialization
      - Add mouse enter/leave event handlers for canvas hover detection
      - _Requirements: 3.2_

    - [ ] 6.2 Implement mouse event routing to hand state machine
      - Add IsMouseOverBall method to detect when mouse is over ball elements
      - Implement OnCanvasMouseEnter and OnCanvasMouseLeave event handlers
      - Route mouse events to hand state machine via appropriate triggers
      - _Requirements: 3.2_

  - [ ] 7. Update BallViewModel for hand state machine interactions
    - [ ] 7.1 Add hand state machine integration to BallViewModel
      - Inject IHandStateMachine into BallViewModel constructor
      - Add mouse enter/leave handlers for ball-specific hover detection
      - Route ball interaction events to hand state machine
      - _Requirements: 3.2, 3.3, 3.4_

    - [ ] 7.2 Implement drag operation integration with hand state machine
      - Trigger StartGrabbing when ball drag begins
      - Trigger StopGrabbing when ball drag ends
      - Ensure proper hand state transitions during ball interactions
      - _Requirements: 3.3, 3.4_

  - [ ] 8. Configure dependency injection and services
    - [ ] 8.1 Register hand state machine and cursor services in ServiceBootstrapper
      - Add IHandStateMachine and HandStateMachine registration to DI container
      - Add ICursorService and CursorManager registration to DI container
      - Register CursorImageLoader service
      - _Requirements: 2.1, 2.2_

    - [ ] 8.2 Update configuration service for cursor settings
      - Enhance existing ConfigurationService to load cursor configuration
      - Add cursor configuration validation and default value handling
      - Configure CursorConfiguration binding from appsettings.json
      - _Requirements: 2.1, 2.2, 2.3, 2.4_

  - [ ] 9. Create cursor resource structure and default images
    - [ ] 9.1 Create cursor resource directory structure and project configuration
      - Create src/BallDragDrop/Resources/Cursors directory for PNG cursor files
      - Add placeholder PNG files for default, hover, grabbing, and releasing cursors (30x30 pixels)
      - Update BallDragDrop.csproj to include cursor resources with CopyToOutputDirectory=PreserveNewest
      - Ensure cursor files are copied to output directory maintaining Resources/Cursors/ structure
      - _Requirements: 1.3, 2.1_

    - [ ] 9.2 Update appsettings.json with hand state cursor configuration
      - Add cursorConfiguration section to appsettings.json
      - Include relative paths to cursor PNG files (Resources/Cursors/filename.png) for each hand state
      - Set enableCustomCursors, debounceTimeMs, and releasingDurationMs default values
      - Ensure paths are relative to application output directory
      - _Requirements: 2.1, 2.2, 4.1_

  - [ ] 10. Implement comprehensive error handling
    - [ ] 10.1 Add cursor loading error handling
      - Implement LoadCursorWithFallback method for graceful PNG loading failures
      - Add HandleCursorApplicationError method for cursor application failures
      - Create logging for all cursor-related errors and warnings
      - _Requirements: 5.1, 5.2, 5.3, 5.4_

    - [ ] 10.2 Add hand state machine error handling
      - Implement error handling for invalid hand state transitions
      - Add automatic recovery to Default hand state on errors
      - Create logging for hand state machine errors and recovery actions
      - _Requirements: 5.5_

    - [ ] 10.3 Add configuration validation and error recovery
      - Implement cursor configuration validation with error logging
      - Add fallback to system cursors when configuration is invalid
      - Create error recovery mechanisms for cursor system failures
      - _Requirements: 5.3, 5.4_

  - [ ] 11. Create comprehensive unit tests
    - [ ] 11.1 Create HandStateMachine unit tests
      - Write tests for hand state transitions and trigger validation
      - Add tests for ball state observer integration
      - Create tests for mouse event handling and state transitions
      - Test error handling and recovery mechanisms
      - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 5.5_

    - [ ] 11.2 Create CursorManager unit tests
      - Write tests for cursor loading, caching, and hand state-based cursor application
      - Add tests for configuration reload and error handling scenarios
      - Create performance tests to validate 16ms cursor update timing
      - _Requirements: 1.1, 1.2, 1.3, 1.4, 4.1, 4.2_

    - [ ] 11.3 Create CursorImageLoader unit tests
      - Write tests for PNG loading, image resizing to 30x30 pixels, and cursor conversion
      - Add tests for corrupted PNG file handling and error scenarios
      - Create tests for memory management and resource disposal
      - _Requirements: 1.3, 5.1, 5.2_

    - [ ] 11.4 Create integration tests for hand state machine and cursor system
      - Write tests for cursor changes during hand state transitions
      - Add tests for ball state integration with hand state changes
      - Create tests for mouse interaction scenarios and cursor updates
      - Test configuration reload and dynamic cursor updates
      - _Requirements: 1.1, 1.2, 3.1, 3.2, 3.3, 3.4, 3.5, 2.2_