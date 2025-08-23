# Cursor Image Handling Guide

## Overview
The Ball Drag Drop application implements a sophisticated cursor management system that provides visual feedback based on user interactions with the ball. The system uses custom PNG images converted to Windows cursors at runtime.

## Architecture

### Core Components

1. **CursorImageLoader** - Handles PNG to cursor conversion
2. **HandStateMachine** - Manages cursor state transitions
3. **CursorService** - Coordinates cursor updates
4. **TestCursorGenerator** - Creates test cursor images

### Cursor States

The application supports the following cursor states:

- **Default** - Standard arrow cursor for general UI interaction
- **Hover** - Special cursor when hovering over the ball
- **Grabbing** - Cursor shown while dragging the ball
- **Releasing** - Brief cursor shown when releasing the ball

## File Structure

```
Resources/Cursors/
├── default.png     - Default arrow cursor
├── hover.png       - Hover state cursor
├── grabbing.png    - Drag state cursor
└── releasing.png   - Release state cursor
```

## Image Requirements

### Format Specifications
- **Format**: PNG with transparency support
- **Size**: 32x32 pixels (recommended)
- **Color Depth**: 32-bit RGBA
- **Hotspot**: Automatically set to center (16,16)

### Design Guidelines
- Use clear, recognizable icons
- Ensure good contrast against various backgrounds
- Keep designs simple for 32x32 resolution
- Use transparency for smooth edges

## Technical Implementation

### PNG to Cursor Conversion Process

1. **File Loading**: PNG files are loaded from the Resources/Cursors directory
2. **Validation**: File size and format are validated
3. **Bitmap Creation**: PNG is converted to System.Drawing.Bitmap
4. **Resizing**: Image is resized to 32x32 if needed
5. **Cursor Creation**: Bitmap is converted to Windows cursor format
6. **Caching**: Converted cursors are cached for performance

### State Machine Integration

The cursor system integrates with the hand state machine:

```
Default → Hover (mouse over ball)
Hover → Grabbing (mouse down on ball)
Grabbing → Releasing (mouse up)
Releasing → Default (after brief delay)
```

## Configuration

### Application Settings
Cursor behavior can be configured in `appsettings.json`:

```json
{
  "CustomCursorsEnabled": true,
  "CursorSize": 32,
  "CursorHotspotX": 16,
  "CursorHotspotY": 16
}
```

### Runtime Control
- Custom cursors can be disabled via configuration
- Falls back to system cursors when disabled
- Supports dynamic enabling/disabling

## Troubleshooting

### Common Issues

1. **Cursors Not Appearing**
   - Check PNG files exist in Resources/Cursors/
   - Verify files are marked as "Content" with "Copy Always"
   - Ensure PNG format is valid with transparency

2. **Performance Issues**
   - Cursors are cached after first load
   - Large images are automatically resized
   - Consider reducing image complexity

3. **State Transition Problems**
   - Check hand state machine logs
   - Verify mouse event handling
   - Review state transition triggers

### Debug Information

The system provides extensive logging:
- Cursor loading progress
- State transitions
- File validation results
- Performance metrics

Log entries are prefixed with "CURSOR DEBUG:" or "CURSOR LOADING:" for easy filtering.

## Testing

### Test Cursor Generation
The application includes a test cursor generator that creates simple colored cursors for testing:

```csharp
// Generate test cursors
var generator = new TestCursorGenerator();
generator.GenerateAllTestCursors();
```

### Manual Testing
1. Run the application
2. Hover over the ball - cursor should change
3. Click and drag - cursor should change to grabbing state
4. Release - cursor should briefly show releasing state
5. Move away from ball - cursor returns to default

## Performance Considerations

- Cursors are loaded asynchronously when possible
- Bitmap operations are performed on background threads
- Memory usage is optimized through proper disposal
- Caching prevents repeated conversions

## Future Enhancements

Potential improvements to the cursor system:
- Animated cursor support
- Multiple cursor themes
- User-customizable cursors
- High-DPI cursor variants
- Cursor preview in settings