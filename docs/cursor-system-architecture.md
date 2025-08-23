# Cursor System Architecture

## Overview
The Ball Drag Drop application implements a sophisticated custom cursor system that dynamically changes cursors based on user interaction states. The system converts PNG images to Windows cursor format (.cur) at runtime and applies them based on hand state transitions.

## Architecture Components

### 1. Hand State Machine (`HandStateMachine`)
- **Purpose**: Manages interaction states and triggers cursor changes
- **States**: Default, Hover, Grabbing, Releasing
- **Triggers**: MouseOverBall, MouseLeaveBall, StartGrabbing, StopGrabbing, ReleaseComplete
- **Location**: `src/BallDragDrop/Services/HandStateMachine.cs`

### 2. Cursor Manager (`CursorManager`)
- **Purpose**: Orchestrates cursor loading, caching, and application
- **Responsibilities**:
  - Load cursor configuration
  - Manage cursor cache for performance
  - Apply cursors to the main window
  - Handle throttling to prevent excessive updates
- **Location**: `src/BallDragDrop/Services/CursorManager.cs`

### 3. Cursor Image Loader (`CursorImageLoader`)
- **Purpose**: Converts PNG images to Windows cursor format
- **Process**:
  1. Load PNG from file system
  2. Resize to standard cursor size (32x32)
  3. Convert to Windows .cur format with proper headers
  4. Create WPF Cursor object
- **Location**: `src/BallDragDrop/Services/CursorImageLoader.cs`

## Data Flow

```
Mouse Event → HandStateMachine → CursorManager → CursorImageLoader → Window.Cursor
     ↓              ↓                ↓               ↓                    ↓
MouseEnter    Fire(Trigger)   SetCursorForHandState  LoadPngAsCursor   ApplyCursor
MouseLeave    StateChanged    UpdateCursorNow        PNG→CUR           UI Update
MouseDown     OnMouseOver     GetCursorForHandState  ResizeImage
MouseUp       OnDragStart     LoadCursorFromConfig   CreateCursorData
```

## Configuration

### Cursor Paths (`appsettings.json`)
```json
{
  "CursorSettings": {
    "EnableCustomCursors": true,
    "DefaultCursorPath": "Resources/Cursors/default.png",
    "HoverCursorPath": "Resources/Cursors/hover.png",
    "GrabbingCursorPath": "Resources/Cursors/grabbing.png",
    "ReleasingCursorPath": "Resources/Cursors/releasing.png",
    "DebounceTimeMs": 16,
    "ReleasingDurationMs": 200
  }
}
```

### File Structure
```
Resources/
└── Cursors/
    ├── default.png     # Default cursor state
    ├── hover.png       # Mouse over ball
    ├── grabbing.png    # Dragging ball
    └── releasing.png   # Ball released
```

## State Transitions

### Hand States
1. **Default**: Normal cursor, mouse not over ball
2. **Hover**: Mouse over ball, ready to interact
3. **Grabbing**: Actively dragging the ball
4. **Releasing**: Brief state after ball release

### Transition Flow
```
Default ←→ Hover ←→ Grabbing → Releasing → Default
   ↑                              ↓
   └──────── Reset ←──────────────┘
```

## Technical Implementation

### PNG to Cursor Conversion Process

1. **File Loading**
   ```csharp
   using var bitmap = new Bitmap(pngPath);
   ```

2. **Image Resizing**
   ```csharp
   var resizedBitmap = ResizeImage(bitmap, 32, 32);
   ```

3. **Cursor Data Creation**
   ```csharp
   // Create .cur file structure in memory
   var cursorData = CreateCursorData(resizedBitmap, hotspotX, hotspotY);
   ```

4. **WPF Cursor Creation**
   ```csharp
   using var stream = new MemoryStream(cursorData);
   return new Cursor(stream);
   ```

### Cursor File Format (.cur)
The system creates Windows cursor files with this structure:
- **Header**: File type, image count
- **Directory Entry**: Image dimensions, color info, data offset
- **Bitmap Info Header**: Image format details
- **Color Data**: 32-bit RGBA pixel data
- **AND Mask**: Transparency mask for cursor

## Performance Optimizations

### Caching Strategy
- Cursors are loaded once and cached in memory
- Cache key: HandState enum value
- Prevents repeated PNG conversion operations

### Throttling
- Cursor updates are throttled to prevent excessive calls
- Default throttle: 16ms (60 FPS)
- Uses EventThrottler for smooth performance

### Error Handling
- Comprehensive fallback to system cursors
- Graceful degradation when PNG files are missing
- Detailed logging for debugging

## Integration Points

### MainWindow Event Handlers
```csharp
private void BallImage_MouseEnter(object sender, MouseEventArgs e)
{
    _handStateMachine.OnMouseOverBall();
}

private void BallImage_MouseLeave(object sender, MouseEventArgs e)
{
    _handStateMachine.OnMouseLeaveBall();
}
```

### Dependency Injection
All cursor services are registered in `ServiceBootstrapper`:
```csharp
services.AddSingleton<ICursorService, CursorManager>();
services.AddSingleton<CursorImageLoader>();
services.AddSingleton<IHandStateMachine, HandStateMachine>();
```

## Debugging and Logging

### Log Categories
- **CURSOR DEBUG**: High-level cursor operations
- **CURSOR LOADING**: Detailed PNG conversion process
- **Hand state transitions**: State machine operations

### Common Log Messages
- `SetCursorForHandState called with {HandState}`
- `CURSOR LOADING: Starting PNG cursor load for path: {Path}`
- `Successfully applied cursor for hand state {HandState}`

## Troubleshooting

### Common Issues
1. **Cursors not visible**: PNG files may be too small or transparent
2. **Performance issues**: Check throttling configuration
3. **File not found**: Verify PNG file paths in configuration
4. **Memory leaks**: Ensure proper cursor disposal

### Diagnostic Steps
1. Check log for "CURSOR LOADING" messages
2. Verify PNG files exist and are valid
3. Test with larger, more visible cursor images
4. Validate configuration settings