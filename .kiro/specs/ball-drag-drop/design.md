# Design Document: Ball Drag and Drop Application

## Overview

This document outlines the design for a desktop application that allows users to drag, drop, and throw a bitmap of a ball around the application window. The application will be built using .NET 9 with Windows Presentation Foundation (WPF) to create a responsive and visually appealing user experience.

The application will feature a clean interface with a ball graphic that users can interact with using mouse input. The core functionality includes dragging and dropping the ball, as well as throwing it with momentum-based physics for more dynamic interactions.

## Architecture

The application will follow the Model-View-ViewModel (MVVM) architectural pattern, which is well-suited for WPF applications. This pattern will help separate the UI logic from the business logic and data, making the code more maintainable and testable.

### Key Components

```mermaid
graph TD
    A[App.xaml] --> B[MainWindow.xaml]
    B --> C[BallViewModel]
    C --> D[BallModel]
    C --> E[PhysicsEngine]
    C --> F[ImageService]
    F --> G[AnimationEngine]
    F --> H[AsepriteLoader]
    B --> I[ResourceDictionary]
    I --> J[Styles/Templates]
    I --> K[Ball Visuals]
```

1. **App.xaml**: The entry point of the application.
2. **MainWindow.xaml**: The main window of the application containing the UI elements.
3. **BallViewModel**: Handles the presentation logic and user interactions.
4. **BallModel**: Represents the data and state of the ball.
5. **PhysicsEngine**: Manages the physics calculations for ball movement and momentum.
6. **ImageService**: Handles loading and managing static images and animations.
7. **AnimationEngine**: Manages animation playback, frame timing, and animation state.
8. **AsepriteLoader**: Specialized loader for Aseprite PNG+JSON exports.
9. **ResourceDictionary**: Contains styles, templates, and resources used in the application.

## Components and Interfaces

### MainWindow

The MainWindow will serve as the container for the application's UI elements. It will:
- Host the Canvas where the ball will be displayed and moved
- Handle window-related events (resize, close)
- Maintain the application's visual appearance

```xml
<Window x:Class="BallDragDrop.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Ball Drag and Drop" Height="450" Width="800">
    <Canvas x:Name="MainCanvas">
        <!-- Ball will be added here -->
    </Canvas>
</Window>
```

### BallViewModel

The BallViewModel will act as an intermediary between the View (MainWindow) and the Model (BallModel). It will:
- Expose properties for the ball's position, size, and state
- Handle user input events (mouse down, move, up)
- Implement commands for user interactions
- Manage the physics calculations through the PhysicsEngine
- Coordinate with ImageService for visual content management

```csharp
public class BallViewModel : INotifyPropertyChanged
{
    private BallModel _ball;
    private PhysicsEngine _physicsEngine;
    private ImageService _imageService;
    private DispatcherTimer _animationTimer;
    
    // Properties for binding
    public double X { get; set; }
    public double Y { get; set; }
    public bool IsDragging { get; set; }
    public ImageSource BallImage { get; set; }
    public bool IsAnimated { get; set; }
    
    // Commands
    public ICommand MouseDownCommand { get; }
    public ICommand MouseMoveCommand { get; }
    public ICommand MouseUpCommand { get; }
    public ICommand LoadImageCommand { get; }
    
    // Methods for handling drag, drop, and throw
    public async Task LoadBallVisualAsync(string filePath)
    private void OnAnimationTimerTick(object sender, EventArgs e)
    private void UpdateBallImage()
    
    // Implementation of INotifyPropertyChanged
}
```

### BallModel

The BallModel will represent the data and state of the ball. It will:
- Store the ball's position, velocity, and other physical properties
- Provide methods to update the ball's state

```csharp
public class BallModel
{
    public double X { get; set; }
    public double Y { get; set; }
    public double VelocityX { get; set; }
    public double VelocityY { get; set; }
    public double Radius { get; set; }
    public double Mass { get; set; }
    
    // Methods to update position, apply forces, etc.
}
```

### PhysicsEngine

The PhysicsEngine will handle the physics calculations for the ball's movement. It will:
- Calculate the ball's trajectory when thrown
- Apply friction to slow down the ball over time
- Handle collision detection with window boundaries
- Implement bouncing behavior

```csharp
public class PhysicsEngine
{
    // Constants for physics calculations
    private const double Friction = 0.98;
    private const double Gravity = 0.5;
    private const double BounceRestitution = 0.8;
    
    // Methods for physics calculations
    public void ApplyVelocity(BallModel ball)
    public void ApplyFriction(BallModel ball)
    public void HandleBoundaryCollision(BallModel ball, double windowWidth, double windowHeight)
    public void CalculateThrowVelocity(BallModel ball, Point startPoint, Point endPoint, double timeInterval)
}
```

### ImageService

The ImageService will manage loading and providing visual content for the ball. It will:
- Detect file types and determine if content is static or animated
- Load static images (PNG, JPG, BMP)
- Load and manage animated content (GIF, Aseprite exports)
- Provide the current frame for animated content
- Handle fallback scenarios when content cannot be loaded

```csharp
public class ImageService
{
    private AnimationEngine _animationEngine;
    private AsepriteLoader _asepriteLoader;
    
    public ImageSource CurrentFrame { get; private set; }
    public bool IsAnimated { get; private set; }
    public TimeSpan FrameDuration { get; private set; }
    
    public async Task<bool> LoadBallVisualAsync(string filePath)
    public void StartAnimation()
    public void StopAnimation()
    public void UpdateFrame()
    public ImageSource GetFallbackImage()
}
```

### AnimationEngine

The AnimationEngine will handle animation playback and timing. It will:
- Manage frame sequences and timing
- Control animation playback state
- Provide smooth frame transitions
- Handle different animation formats

```csharp
public class AnimationEngine
{
    public List<AnimationFrame> Frames { get; private set; }
    public int CurrentFrameIndex { get; private set; }
    public bool IsPlaying { get; private set; }
    public bool IsLooping { get; set; } = true;
    
    public void LoadFrames(List<AnimationFrame> frames)
    public void Play()
    public void Pause()
    public void Stop()
    public void NextFrame()
    public AnimationFrame GetCurrentFrame()
    public void Update(TimeSpan deltaTime)
}

public class AnimationFrame
{
    public ImageSource Image { get; set; }
    public TimeSpan Duration { get; set; }
    public Rectangle SourceRect { get; set; }
}
```

### AsepriteLoader

The AsepriteLoader will handle Aseprite-specific file formats. It will:
- Parse JSON metadata files
- Extract frames from PNG sprite sheets
- Convert Aseprite data to internal animation format
- Handle multiple animation tags/sequences

```csharp
public class AsepriteLoader
{
    public async Task<AsepriteData> LoadAsepriteAsync(string pngPath, string jsonPath)
    public List<AnimationFrame> ConvertToAnimationFrames(AsepriteData data)
    public ImageSource ExtractFrame(ImageSource spriteSheet, Rectangle sourceRect)
}

public class AsepriteData
{
    public List<AsepriteFrame> Frames { get; set; }
    public List<AsepriteTag> Tags { get; set; }
    public AsepriteMetadata Meta { get; set; }
}

public class AsepriteFrame
{
    public Rectangle Frame { get; set; }
    public int Duration { get; set; }
}

public class AsepriteTag
{
    public string Name { get; set; }
    public int From { get; set; }
    public int To { get; set; }
    public string Direction { get; set; }
}
```

## Data Models

### Ball Properties

The ball will have the following properties:

- **Position (X, Y)**: The current coordinates of the ball on the canvas
- **Velocity (VelocityX, VelocityY)**: The speed and direction of the ball's movement
- **Radius**: The size of the ball (for collision detection)
- **Mass**: Used in physics calculations
- **IsDragging**: Boolean flag indicating whether the ball is being dragged

### Mouse Interaction Data

To handle mouse interactions, we'll track:

- **MouseStartPosition**: The position where the user first clicked the ball
- **BallStartPosition**: The ball's position when the user started dragging
- **LastMousePosition**: The previous mouse position (for calculating velocity)
- **MouseVelocity**: The speed and direction of the mouse movement
- **LastUpdateTime**: Timestamp for calculating time intervals

## User Interface Design

The user interface will be minimalistic, focusing on the ball interaction:

1. **Main Window**: A clean window with a title and a canvas area
2. **Ball Representation**: A bitmap image of a ball displayed on the canvas
3. **Cursor Feedback**: Cursor changes to indicate when the ball can be grabbed and when it's being moved

### Cursor States

- **Default**: Standard cursor when not interacting with the ball
- **Hand**: When hovering over the ball to indicate it can be grabbed
- **Closed Hand/Grabbing**: When dragging the ball

## Error Handling

The application will implement the following error handling strategies:

1. **Image Loading Errors**: If the ball image cannot be loaded, a fallback shape (e.g., a colored circle) will be displayed instead.
2. **Out-of-Bounds Handling**: Logic to prevent the ball from leaving the visible area of the window.
3. **Exception Handling**: Try-catch blocks around critical operations with appropriate user feedback.
4. **Logging**: Basic logging of errors for troubleshooting.

## Visual Content Management

### Image and Animation Loading

The application will support multiple visual content types:

1. **Static Images**: PNG, JPG, and BMP files loaded directly as ImageSource
2. **GIF Animations**: Animated GIF files with automatic frame extraction
3. **Aseprite Exports**: PNG sprite sheets with JSON metadata for frame timing and sequences

### Animation System Architecture

```mermaid
graph LR
    A[File Input] --> B{File Type Detection}
    B -->|Static| C[Direct Load]
    B -->|GIF| D[GIF Decoder]
    B -->|PNG+JSON| E[Aseprite Loader]
    C --> F[ImageService]
    D --> G[Animation Engine]
    E --> G
    G --> F
    F --> H[BallViewModel]
    H --> I[UI Display]
```

### Animation Playback

The animation system will handle:

1. **Frame Management**: Store and manage sequences of animation frames
2. **Timing Control**: Respect frame durations from source files
3. **Playback State**: Play, pause, stop, and loop controls
4. **Performance**: Efficient frame updates without impacting drag responsiveness

## Animation and Physics

### Ball Movement

The ball's movement will be handled through two mechanisms:

1. **Direct Manipulation**: When dragging, the ball follows the mouse position directly.
2. **Physics-Based Animation**: When thrown, the ball moves according to physics calculations.

### Visual Animation

The ball's visual representation will support:

1. **Static Display**: Single frame images displayed continuously
2. **Continuous Animation**: Frame-based animations playing in loops
3. **Animation During Interaction**: Animations continue playing while dragging or throwing

### Physics Calculations

The physics system will implement:

1. **Momentum**: Calculate velocity based on the speed and direction of mouse movement before release.
2. **Friction**: Gradually reduce velocity over time to simulate friction.
3. **Collision**: Detect and respond to collisions with window boundaries.
4. **Bouncing**: Implement realistic bouncing behavior when the ball hits the edges.

## Performance Considerations

To ensure smooth performance:

1. **Rendering Optimization**: Use hardware acceleration for rendering both static and animated content.
2. **Animation Frame Rate**: Target 60 FPS for smooth physics animations while respecting source animation frame rates.
3. **Event Throttling**: Limit the frequency of mouse move event handling if necessary.
4. **Efficient Collision Detection**: Implement simple but efficient collision detection algorithms.
5. **Animation Memory Management**: Cache animation frames efficiently and dispose of unused resources.
6. **Dual Timer System**: Separate timers for physics updates (60 FPS) and animation frame updates (variable based on source).
7. **Frame Pre-loading**: Pre-load animation frames to avoid stuttering during playback.
8. **Aseprite Optimization**: Cache parsed JSON metadata and extracted frames to avoid repeated processing.

## Testing Strategy

The testing strategy will include:

1. **Unit Testing**:
   - Test physics calculations
   - Test boundary collision detection
   - Test velocity and position updates

2. **Integration Testing**:
   - Test the interaction between the ViewModel and the Model
   - Test the physics engine integration

3. **UI Testing**:
   - Test mouse interaction with the ball
   - Test window resizing behavior
   - Test animation smoothness

4. **Performance Testing**:
   - Measure frame rates during animations
   - Test responsiveness of drag operations