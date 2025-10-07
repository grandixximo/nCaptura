# Webcam Implementation Notes

## Overview
Complete ground-up rewrite of webcam preview using MediaFoundation instead of DirectShow.

## Key Changes

### 1. **CaptureWebcam.cs** - Core Implementation
- **Old**: DirectShow filter graph with IVideoWindow for preview
- **New**: MediaFoundation IMFSourceReader for frame capture
- **Benefits**:
  - Simpler architecture (no complex filter graph management)
  - More reliable on Windows 10/11
  - Better error handling with clear COM error codes
  - Automatic format conversion (RGB32/BGR32)

### 2. **WebcamCapture.cs** - Wrapper
- Thread-safe wrapper using SyncContextManager
- Enhanced error handling with user-friendly messages
- Handles common error scenarios:
  - E_ACCESSDENIED (0x80070005) - Privacy settings blocked camera
  - MF_E_VIDEO_RECORDING_DEVICE_INVALIDATED - Camera in use by another app
  - MF_E_ATTRIBUTENOTFOUND - Configuration error

### 3. **Filter.cs** - Device Enumeration
- **Primary**: MediaFoundation device enumeration (modern)
- **Fallback**: DirectShow enumeration (legacy compatibility)
- Handles both MediaFoundation and DirectShow device identifiers

### 4. **WebcamProvider.cs** - Provider
- Added null/empty checks
- Returns empty list on error instead of throwing exceptions
- Better defensive programming

### 5. **WebcamItem.cs** - Device Item
- Enhanced error messages with actionable steps
- Graceful failure (returns null instead of throwing)

## Architecture Differences

### DirectShow (Old)
```
Device → Filter Graph → Video Window (embedded preview)
                      → Sample Grabber (frame capture)
```

### MediaFoundation (New)
```
Device → Media Source → Source Reader → Frame Capture
```

## Preview Window Handling

**Important Note**: MediaFoundation IMFSourceReader doesn't provide embedded preview windows like DirectShow IVideoWindow. 

The preview is now handled differently:
1. **Old approach**: DirectShow video window embedded in UI
2. **New approach**: Frame capture only - UI must poll Capture() to display frames

The UI layer (WebcamPage.xaml.cs) can:
- Call `Capture()` repeatedly to get frames for display
- Use a timer or rendering loop for continuous preview
- Or rely on recording loop to capture frames (which is the primary use case)

`UpdatePreview(IWindow, Rectangle)` method is maintained for interface compatibility but doesn't create an embedded preview window.

## Error Handling

### Common Error Codes
- `0x80070005` (E_ACCESSDENIED) - Camera privacy settings in Windows
- `0xC00D3E86` (MF_E_VIDEO_RECORDING_DEVICE_INVALIDATED) - Camera in use
- `0xC00D36E6` (MF_E_ATTRIBUTENOTFOUND) - Format configuration error

### User Messages
All error messages now include:
- Clear problem description
- Actionable steps to resolve
- Direct links to settings when possible

## Testing Checklist

- [ ] Device enumeration works
- [ ] Can initialize webcam capture
- [ ] Frame capture returns valid bitmaps
- [ ] Width/Height properties are correct
- [ ] Proper cleanup on disposal
- [ ] Error messages display correctly
- [ ] Works with multiple cameras
- [ ] Camera switch works correctly
- [ ] Recording with webcam overlay works
- [ ] Separate webcam file recording works

## Dependencies

Required NuGet packages:
- MediaFoundation (v3.1.0) - COM wrappers for Media Foundation
- DirectShowLib (v1.0.0) - Fallback device enumeration

## Compatibility

- **Windows 10/11**: Full support via MediaFoundation
- **Windows 7/8**: Should work via DirectShow fallback
- **Privacy Settings**: Properly detects and reports access denied errors
- **.NET Framework**: 4.7.2 or higher

## Future Improvements

Potential enhancements:
1. Add async frame capture support
2. Implement preview callback for smoother UI updates
3. Add camera capability detection (resolution, framerate options)
4. Support for multiple simultaneous cameras
5. Hardware acceleration via DXGI if available
