# Implementation Plan

- [x] 1. Add Stateless NuGet package dependency
  - Add Stateless package reference to BallDragDrop.csproj
  - Verify package installation and compatibility with .NET 9.0
  - _Requirements: 1.1, 2.1_

- [x] 2. Create core state machine enumerations and data models





  - [x] 2.1 Create BallState enumeration


    - Define Idle, Held, and Thrown states
    - Add XML documentation for each state
    - _Requirements: 1.1, 1.2, 1.3, 1.4_
  - [x] 2.2 Create BallTrigger enumeration


    - Define MouseDown, Release, and VelocityBelowThreshold triggers
    - Add XML documentation for each trigger
    - _Requirements: 1.2, 1.3, 1.4_
  - [x] 2.3 Create BallStateChangedEventArgs class
    - Implement event args with PreviousState, NewState, Trigger, and Timestamp properties
    - Add constructor and proper XML documentation
    - _Requirements: 3.1_
- [x] 3. Create state machine interfaces and configuration
  - [x] 3.1 Create IBallStateMachine interface
    - Define CurrentState property, Fire method, CanFire method, StateChanged event
    - Add Subscribe/Unsubscribe methods for observers
    - _Requirements: 1.1, 2.1, 3.1_
  - [x] 3.2 Create IBallStateObserver interface
    - Define OnStateChanged method signature
    - Add XML documentation for observer pattern implementation
    - _Requirements: 3.1, 3.2, 3.3, 3.4_
  - [x] 3.3 Create BallStateConfiguration class
    - Implement configuration properties for velocity threshold, transition delays, logging, and visual feedback
    - Add default values and validation
    - _Requirements: 5.3_

- [x] 4. Implement core BallStateMachine service
  - [x] 4.1 Create BallStateMachine class with Stateless integration
    - Implement IBallStateMachine interface using Stateless library
    - Configure state machine with three states and valid transitions
    - Add logging for all state transitions
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 2.1, 2.2, 2.3_
  - [x] 4.2 Implement observer pattern in BallStateMachine
    - Add observer subscription/unsubscription methods
    - Implement state change notification to all registered observers
    - Add thread-safe observer list management
    - _Requirements: 3.1, 3.2, 3.3, 3.4_
  - [x] 4.3 Add error handling and validation to BallStateMachine
    - Implement invalid transition rejection with logging
    - Add state consistency validation methods
    - Implement recovery mechanisms for error scenarios
    - _Requirements: 2.1, 2.2, 2.3_

- [x] 5. Integrate state machine with BallViewModel
  - [x] 5.1 Update BallViewModel to implement IBallStateObserver
    - Add IBallStateObserver implementation to BallViewModel
    - Inject IBallStateMachine dependency through constructor
    - Add state-related properties (CurrentState, IsInIdleState, etc.)
    - _Requirements: 1.1, 3.1, 3.4_
  - [x] 5.2 Modify mouse event handlers to use state machine
    - Update OnMouseDown to fire MouseDown trigger when in Idle state
    - Update OnMouseUp to fire Release trigger when in Held state
    - Add state validation before processing mouse events
    - _Requirements: 1.2, 1.3, 2.3_
  - [x] 5.3 Implement state-dependent drag behavior
    - Modify IsDragging property to reflect Held state
    - Update drag logic to only allow dragging when in Held state
    - Add state change handling in OnStateChanged method
    - _Requirements: 1.5, 3.2_
- [x] 6. Enhance PhysicsEngine with state awareness
  - [x] 6.1 Update PhysicsEngine.UpdateBall method signature
    - Add BallState parameter to UpdateBall method
    - Modify method to accept current state for physics calculations
    - Update all existing calls to include state parameter
    - _Requirements: 1.5, 1.6, 5.1, 5.2_
  - [x] 6.2 Implement state-dependent physics behavior
    - Skip physics calculations when ball is in Held state
    - Apply normal physics when ball is in Thrown state
    - Add velocity threshold checking for Thrown to Idle transition
    - _Requirements: 1.5, 1.6, 5.1, 5.2, 5.3_
  - [x] 6.3 Add velocity threshold monitoring
    - Implement velocity magnitude calculation in physics update
    - Add automatic state machine trigger when velocity drops below threshold
    - Configure threshold value from BallStateConfiguration
    - _Requirements: 1.4, 5.3_
- [x] 7. Integrate state machine with StatusBarViewModel
  - [x] 7.1 Update StatusBarViewModel to implement IBallStateObserver
    - Add IBallStateObserver implementation to StatusBarViewModel
    - Inject IBallStateMachine dependency through constructor
    - Subscribe to state machine notifications in constructor
    - _Requirements: 4.5, 4.6, 4.7, 4.8_
  - [x] 7.2 Implement state display formatting in StatusBarViewModel
    - Create FormatBallStateForDisplay method to convert states to display strings
    - Update Status property to show formatted ball state
    - Implement OnStateChanged to update status display
    - _Requirements: 4.5, 4.6, 4.7, 4.8_
- [x] 8. Update dependency injection configuration
  - [x] 8.1 Register state machine services in DI container
    - Register IBallStateMachine as singleton in Program.cs or DI configuration
    - Register BallStateConfiguration as singleton
    - Configure default state machine settings
    - _Requirements: 1.1, 3.1_
  - [x] 8.2 Update existing service registrations
    - Ensure BallViewModel and StatusBarViewModel receive state machine dependencies
    - Update MainWindowViewModel if needed for state machine integration
    - Verify all dependencies are properly resolved
    - _Requirements: 3.1, 3.4_
- [x] 9. Create comprehensive unit tests for state machine





  - [x] 9.1 Create BallStateMachineTests class


    - Test initial state is Idle
    - Test valid state transitions (Idle→Held→Thrown→Idle)
    - Test invalid transition rejection
    - Test observer notification on state changes
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 2.1, 2.2, 2.3, 3.1_
  - [x] 9.2 Create state machine integration tests


    - Test BallViewModel state integration
    - Test PhysicsEngine state-aware behavior
    - Test StatusBarViewModel state display
    - Test complete state lifecycle scenarios
    - _Requirements: 1.5, 1.6, 3.2, 3.3, 3.4, 4.5, 4.6, 4.7, 4.8_
  - [x] 9.3 Create error handling and edge case tests
    - Test concurrent state transitions
    - Test error recovery mechanisms
    - Test state consistency validation
    - Test observer subscription/unsubscription
    - _Requirements: 2.1, 2.2, 2.3_

- [x] 10. Add visual feedback for ball states
  - [x] 10.1 Implement state-dependent visual styling
    - Add visual properties to BallViewModel for different states
    - Create state-specific styling or visual indicators
    - Update XAML bindings to reflect state changes
    - _Requirements: 4.1, 4.2, 4.3, 4.4_
  - [x] 10.2 Test visual feedback responsiveness
    - Verify visual updates occur within 100ms of state changes
    - Test visual feedback across all state transitions
    - Ensure visual consistency with state machine state
    - _Requirements: 4.4_
- [x] 11. Implement reset functionality


  - [x] 11.1 Add Reset trigger to state machine configuration


    - Update BallTrigger enumeration to include Reset trigger
    - Configure state machine to allow Reset trigger from any state to Idle
    - Add state machine transition logging for reset operations
    - _Requirements: 6.1_
  - [x] 11.2 Implement Reset_Click event handler in MainWindow
    - Create Reset_Click method in MainWindow.xaml.cs
    - Implement ball position reset to canvas center
    - Stop any ongoing physics simulation and clear ball velocity
    - Release mouse capture if ball is currently being dragged
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5_
  - [x] 11.3 Add reset method to BallViewModel
    - Create ResetBall method in BallViewModel to handle reset logic
    - Coordinate with state machine to trigger Reset transition
    - Ensure proper cleanup of drag state and mouse tracking
    - _Requirements: 6.1, 6.2, 6.3, 6.5_
- [-] 12. Integration testing and validation
  - [x] 12.1 Test complete ball interaction workflow
    - Test mouse down → drag → release → physics → idle cycle
    - Verify state transitions occur at correct times
    - Validate physics behavior matches state requirements
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6_
  - [x] 12.2 Test status bar integration
    - Verify status bar updates immediately on state changes
    - Test status display formatting for all states
    - Validate status bar shows correct state throughout interaction
    - _Requirements: 4.5, 4.6, 4.7, 4.8_
  - [x] 12.3 Test reset functionality
    - Test reset from each ball state (Idle, Held, Thrown)
    - Verify ball returns to center position and Idle state
    - Test reset during active drag operations
    - Validate status bar updates correctly after reset
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6_
  - [-] 12.4 Performance and stability testing
    - Test rapid state transitions for performance
    - Verify memory usage and object disposal
    - Test state machine behavior under stress conditions
    - _Requirements: 2.1, 3.1_