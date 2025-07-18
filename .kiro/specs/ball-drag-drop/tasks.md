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

- [ ] 11. Get tests into green status
  - check, if all tests are necessary
  - Fix any remaining issues in the tests
  - _Requirements: All_