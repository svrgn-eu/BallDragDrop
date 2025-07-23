# Implementation Plan

- [x] 1. Set up project structure
  - Create a new WPF application using .NET 9 in the 'src' subdirectory
  - Set up the basic folder structure for MVVM pattern
  - Configure project properties and references
  - Move the Mainwindow view to the "Views" folder
  - _Requirements: 6.1, 6.2_

- [x] 2. Create the application window
  - [x] 2.1 Implement MainWindow XAML layout
    - Create the main window with appropriate title and dimensions
    - Add a Canvas element as the main container
    - Set up basic window properties and event handlers
    - _Requirements: 1.1, 1.2, 1.3_
  
  - [x] 2.2 Implement window resize handling


    - Add event handlers for window resize events
    - Implement logic to maintain ball position when window is resized
    - Write unit tests for resize functionality
    - _Requirements: 1.3_

- [x] 3. Implement ball model and view model
  - [x] 3.1 Create BallModel class
    - Define properties for position, velocity, size, and other physical attributes
    - Implement methods for updating ball state
    - Write unit tests for the model
    - _Requirements: 2.1, 2.2, 4.2_
  
  - [x] 3.2 Create BallViewModel class
    - Implement INotifyPropertyChanged interface
    - Create properties for binding to the view
    - Set up commands for mouse interactions
    - Write unit tests for the view model
    - _Requirements: 3.1, 3.2, 3.3_

- [x] 4. Implement ball rendering

  - [x] 4.1 Use existing ball image resource
    - Use the existing ball image from ./Resources/Ball/Ball01.png
    - Implement fallback rendering for when image can't be loaded
    - Write unit tests for image loading
    - _Requirements: 2.1, 2.2, 2.3_
  
  - [x] 4.2 Create ball UI element
    - Create an Image control bound to the view model
    - Set up proper sizing and positioning
    - Implement high-quality rendering
    - _Requirements: 2.2, 2.4_

- [x] 5. Implement drag and drop functionality
  - [x] 5.1 Add mouse event handlers
    - Implement MouseDown, MouseMove, and MouseUp event handlers
    - Connect events to view model commands
    - Write unit tests for event handling
    - _Requirements: 3.1, 3.2, 3.3_
  
  - [x] 5.2 Implement cursor feedback
    - Change cursor when hovering over the ball
    - Change cursor when dragging the ball
    - Write unit tests for cursor changes
    - _Requirements: 3.5, 3.6_
  
  - [x] 5.3 Implement boundary constraints

    - Add logic to keep the ball within the window boundaries
    - Handle edge cases for window resizing
    - Write unit tests for boundary constraints
    - _Requirements: 3.7_

- [x] 6. Implement physics engine

  - [x] 6.1 Create PhysicsEngine class
    - Implement basic physics calculations (velocity, friction)
    - Add methods for applying forces and updating position
    - Write unit tests for physics calculations
    - _Requirements: 4.2, 4.3_
  
  - [x] 6.2 Implement collision detection
    - Add boundary collision detection
    - Implement bouncing behavior
    - Write unit tests for collision detection
    - _Requirements: 4.4_

- [x] 7. Implement ball throwing functionality


  - [x] 7.1 Add velocity calculation on release
    - Calculate velocity based on mouse movement speed and direction
    - Implement logic to determine when a movement is a throw vs. a drop
    - Write unit tests for velocity calculation
    - _Requirements: 4.1, 4.2_
  
  - [x] 7.2 Implement animation loop
    - Create a CompositionTarget.Rendering-based animation loop
    - Update ball position based on physics in each frame
    - Apply friction to slow down the ball over time
    - Write unit tests for animation behavior
    - _Requirements: 4.3, 5.1, 5.2, 5.3_
  
  - [x] 7.3 Implement ball grabbing during motion
    - Add logic to allow grabbing the ball while it's in motion
    - Handle transition from physics-based to user-controlled movement
    - Write unit tests for interrupting ball motion
    - _Requirements: 4.5_

- [x] 8. Optimize performance
  - [x] 8.1 Implement rendering optimizations
    - Use hardware acceleration where appropriate
    - Optimize animation frame rate
    - Write performance tests
    - _Requirements: 5.1, 5.2, 5.3, 5.4_
  
  - [x] 8.2 Implement event throttling
    - Add logic to limit event processing frequency if needed
    - Ensure smooth interaction even under heavy load
    - Write performance tests for event handling
    - _Requirements: 5.1, 5.4_

- [x] 9. Implement application lifecycle
  - Properly handle application startup
  - Implement clean shutdown
  - Write integration tests for application lifecycle
  - _Requirements: 1.4_

- [x] 10. Final integration and testing
  - Integrate all components
  - Perform end-to-end testing
  - Fix any remaining issues
  - _Requirements: All_

- [-] 11. Implement ImageService for visual content management
  - [x] 11.1 Create ImageService class
    - Implement file type detection for static images, GIFs, and Aseprite exports
    - Add methods for loading different image formats (PNG, JPG, BMP)
    - Implement fallback image generation when content cannot be loaded
    - Write unit tests for file type detection and loading
    - _Requirements: 6.1, 6.2, 6.7, 6.10_
  
  - [x] 11.2 Integrate ImageService with BallViewModel

    - Update BallViewModel to use ImageService instead of direct image loading
    - Add properties for tracking animation state
    - Implement LoadBallVisualAsync method
    - Write unit tests for ImageService integration
    - _Requirements: 6.6, 6.9_

- [-] 12. Implement animation support
  - [x] 12.1 Create AnimationEngine class
    - Implement frame management and timing control
    - Add playback state management (play, pause, stop, loop)
    - Implement frame update logic with proper timing
    - Write unit tests for animation playback
    - _Requirements: 6.4, 6.5_
  
  - [x] 12.2 Add GIF animation support
    - Implement GIF decoder to extract frames and timing
    - Convert GIF frames to internal animation format
    - Handle GIF-specific timing and loop behavior
    - Write unit tests for GIF loading and playback
    - _Requirements: 6.3, 6.4_
  
  - [x] 12.3 Implement animation timer integration
    - Add DispatcherTimer to BallViewModel for animation updates
    - Coordinate animation timing with physics updates
    - Ensure animation continues during drag operations
    - Write unit tests for animation timing
    - _Requirements: 6.5, 6.6_



- [-] 13. Implement Aseprite support

  - [x] 13.1 Create AsepriteLoader class
    - Implement JSON metadata parsing for Aseprite exports
    - Create data structures for Aseprite frames, tags, and metadata
    - Add PNG sprite sheet frame extraction
    - Write unit tests for JSON parsing and frame extraction
    - _Requirements: 6.4, 6.5_
  
  - [x] 13.2 Integrate Aseprite loading with ImageService
    - Add Aseprite file detection (PNG + JSON pair)
    - Convert Aseprite data to internal animation format
    - Handle multiple animation sequences (use default/first)
    - Write unit tests for Aseprite integration
    - _Requirements: 6.4, 6.5, 6.6_
  
  - [x] 13.3 Implement error handling for Aseprite files
    - Handle missing JSON metadata files
    - Provide fallback behavior for invalid JSON
    - Add appropriate error messages for malformed data
    - Write unit tests for error scenarios
    - _Requirements: 6.10_

- [-] 14. Update ball rendering for animations
  - [x] 14.1 Modify ball UI element for animation support
    - Update Image control to handle animated content
    - Ensure proper frame updates without flickering
    - Maintain visual quality during animation playback
    - Write unit tests for animated rendering
    - _Requirements: 2.4, 2.5, 2.8_
  
  - [x] 14.2 Implement visual content switching
    - Add ability to change ball visual without restart
    - Handle transitions between static and animated content
    - Maintain drag functionality during visual changes
    - Write unit tests for content switching
    - _Requirements: 6.6, 6.7, 6.9_

- [-] 15. Optimize animation performance
  - [x] 15.1 Implement animation memory management
    - Add frame caching for efficient memory usage
    - Implement resource disposal for unused animations
    - Pre-load animation frames to prevent stuttering
    - Write performance tests for memory usage
    - _Requirements: 6.8_
  
  - [ ] 15.2 Optimize dual timer system
    - Separate physics updates (60 FPS) from animation frame updates
    - Respect source animation frame rates while maintaining physics smoothness
    - Ensure animation performance doesn't impact drag responsiveness
    - Write performance tests for timer coordination
    - _Requirements: 6.8_

- [ ] 16. Comprehensive testing for animation features
  - [ ] 16.1 Create integration tests for animation system
    - Test ImageService with AnimationEngine integration
    - Test animation playback during drag operations
    - Test visual switching between different content types
    - _Requirements: All animation requirements_
  
  - [ ] 16.2 Add file format testing
    - Test loading of PNG, JPG, BMP static images
    - Test GIF animation loading and playback
    - Test Aseprite PNG+JSON combinations
    - Test error handling for corrupted or missing files
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.10_

- [ ] 17. Get tests into green status
  - Check if all tests are necessary
  - Fix any remaining issues in the tests
  - Ensure all new animation tests pass
  - _Requirements: All_