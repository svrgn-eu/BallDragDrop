# Ball Bounding Box Fix and Debug Feature

## Problem Identified
The ball's bounding box for mouse interaction had an offset issue where clicks were not registering correctly. The problem was:

1. **Mouse Position Calculation**: The mouse down event was using `e.GetPosition(null)` which gets the position relative to the entire window, but the ball's coordinate system is relative to the canvas.

2. **Coordinate System Mismatch**: 
   - Ball position (X, Y) represents the CENTER of the ball
   - Image element positioning uses Canvas.Left and Canvas.Top which represent the TOP-LEFT corner
   - The BallViewModel correctly calculates Left = X - Radius and Top = Y - Radius
   - But mouse click detection was using window coordinates instead of canvas coordinates

## Fixes Implemented

### 1. Mouse Position Fix
**File**: `src/BallDragDrop/Views/MainWindow.xaml.cs`

Changed mouse position calculation from:
```csharp
var position = e.GetPosition(null);  // Window-relative
```

To:
```csharp
var position = e.GetPosition(MainCanvas);  // Canvas-relative
```

This ensures the mouse coordinates match the ball's coordinate system.

### 2. Configuration Option for Bounding Box Display
**Files**: 
- `src/BallDragDrop/Contracts/IConfigurationService.cs`
- `src/BallDragDrop/Services/ConfigurationService.cs`

Added new configuration property:
```csharp
[Option(DefaultValue = false)]
bool ShowBoundingBox { get; set; }
```

Added methods to get/set the bounding box display setting:
```csharp
bool GetShowBoundingBox();
void SetShowBoundingBox(bool show);
```

### 3. Visual Bounding Box Display
**File**: `src/BallDragDrop/Views/MainWindow.xaml`

Added visual elements to show the bounding box when enabled:

1. **Bounding Box Rectangle**: Red dashed rectangle showing the exact clickable area
2. **Center Point Indicator**: Blue dot showing the ball's center point (X, Y coordinates)

Both elements are only visible when `ShowBoundingBox` is true.

### 4. Menu Toggle Option
**File**: `src/BallDragDrop/Views/MainWindow.xaml`

Added menu item in Visual menu:
```xml
<MenuItem Header="Show _Bounding Box" IsCheckable="True" IsChecked="{Binding ShowBoundingBox}" Click="ToggleBoundingBox_Click" />
```

### 5. ViewModel Integration
**Files**:
- `src/BallDragDrop/ViewModels/BallViewModel.cs`
- `src/BallDragDrop/ViewModels/MainWindowViewModel.cs`

Added `ShowBoundingBox` property to BallViewModel that:
- Reads initial value from configuration
- Provides a toggle method
- Notifies property changes for UI binding

## How to Use

1. **Run the application**
2. **Go to Visual menu → Show Bounding Box** to toggle the debug display
3. **When enabled, you'll see**:
   - Red dashed rectangle showing the exact clickable area
   - Blue dot showing the ball's center point
4. **The setting is persisted** in the configuration file

## Technical Details

### Coordinate System
- **Ball Model**: Uses center coordinates (X, Y)
- **Visual Display**: Image positioned at (X - Radius, Y - Radius)
- **Mouse Detection**: Now correctly uses canvas-relative coordinates
- **Bounding Box**: Shows the actual clickable area (diameter × diameter rectangle)

### Configuration Storage
The bounding box display setting is stored in `appsettings.json` and persists between application sessions.

### Performance Impact
The bounding box display has minimal performance impact as it only adds two simple UI elements that are hidden by default.

## Testing the Fix

1. Enable bounding box display
2. Try clicking at different positions around the ball
3. The red rectangle shows exactly where clicks will be detected
4. The blue dot shows the ball's center point for reference
5. Clicks should now register accurately within the red rectangle boundary

This fix resolves the original offset issue and provides a useful debugging tool for future development.