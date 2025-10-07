# Captura.Webcam

Modern webcam capture implementation using Microsoft Media Foundation.

## Features

- **MediaFoundation API**: Uses modern Windows Media Foundation for reliable webcam access
- **DirectShow Fallback**: Falls back to DirectShow for device enumeration when needed
- **Better Error Handling**: Provides clear error messages for camera access issues
- **Format Conversion**: Automatically converts video formats to BGR32 for compatibility

## Architecture

- `CaptureWebcam.cs` - Core capture implementation using Media Foundation IMFSourceReader
- `WebcamCapture.cs` - Thread-safe wrapper implementing IWebcamCapture interface
- `WebcamProvider.cs` - Enumerates available webcam devices
- `WebcamItem.cs` - Represents a single webcam device
- `Filter.cs` - Device enumeration using MediaFoundation with DirectShow fallback
- `DummyForm.cs` - Helper form for preview window management

## Dependencies

- MediaFoundation .NET (v3.1.0)
- DirectShowLib (v1.0.0) - for fallback device enumeration

## Original Attribution

Original DirectShow implementation adapted from [ScreenToGif](https://github.com/NickeManarin/ScreenToGif/)
Licensed under [Microsoft Public License](https://github.com/NickeManarin/ScreenToGif/blob/master/LICENSE.txt).

## Implementation Notes

This implementation has been completely rewritten to use MediaFoundation which provides:
- More reliable device access
- Better Windows 10/11 compatibility
- Clearer error messages for privacy/permission issues
- Simpler architecture without complex filter graph management
