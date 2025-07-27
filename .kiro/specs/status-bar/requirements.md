# Requirements Document

## Introduction

This feature adds a status bar to the bottom of the main window in the BallDragDrop WPF application. The status bar will display real-time performance metrics (FPS and average FPS) on the right side, along with asset information and a status field on the left side. This enhancement will provide users with valuable feedback about application performance and current asset state.

## Requirements

### Requirement 1

**User Story:** As a user, I want to see the current FPS and average FPS displayed in the status bar, so that I can monitor the application's performance in real-time.

#### Acceptance Criteria

1. WHEN the application is running THEN the system SHALL display the current FPS on the right side of the status bar
2. WHEN the application is running THEN the system SHALL calculate and display the average FPS for the last 10 seconds next to the current FPS
3. WHEN the FPS values change THEN the system SHALL update the display in real-time without flickering
4. WHEN no frame data is available THEN the system SHALL display "FPS: --" and "Avg: --" as placeholder text

### Requirement 2

**User Story:** As a user, I want to see the name of the currently loaded asset in the status bar, so that I can quickly identify which visual content is active.

#### Acceptance Criteria

1. WHEN an asset is loaded THEN the system SHALL display the asset name on the leftmost position of the status bar
2. WHEN no asset is loaded THEN the system SHALL display "No Asset" as the default text
3. WHEN the asset changes THEN the system SHALL immediately update the displayed name
4. WHEN the asset name is too long THEN the system SHALL truncate it with ellipsis to fit the available space

### Requirement 3

**User Story:** As a user, I want to see a status field in the status bar, so that I can view general application state information.

#### Acceptance Criteria

1. WHEN the status bar is displayed THEN the system SHALL show a field with the static text "Status" positioned to the right of the asset name
2. WHEN the application state changes THEN the system SHALL maintain the "Status" text consistently
3. WHEN the status bar is resized THEN the system SHALL keep the "Status" field properly positioned

### Requirement 4

**User Story:** As a user, I want the status bar to be properly integrated into the main window layout, so that it doesn't interfere with the existing functionality.

#### Acceptance Criteria

1. WHEN the main window is displayed THEN the system SHALL position the status bar at the bottom of the window
2. WHEN the window is resized THEN the system SHALL maintain the status bar at the bottom with proper width adjustment
3. WHEN the status bar is added THEN the system SHALL ensure the main canvas area is not overlapped
4. WHEN the application starts THEN the system SHALL initialize the status bar with default values

### Requirement 5

**User Story:** As a developer, I want the status bar implementation to follow WPF best practices, so that it integrates seamlessly with the existing codebase.

#### Acceptance Criteria

1. WHEN implementing the status bar THEN the system SHALL use proper MVVM pattern with data binding
2. WHEN updating FPS values THEN the system SHALL use appropriate threading to avoid UI blocking
3. WHEN the status bar is created THEN the system SHALL follow the existing code style and architecture patterns
4. WHEN performance monitoring is active THEN the system SHALL minimize the overhead of FPS calculation on application performance