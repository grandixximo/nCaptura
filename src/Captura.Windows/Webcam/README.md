# Captura.Webcam

Clean, reliable DirectShow-based webcam capture implementation.

## Features

- **DirectShow API**: Uses proven Windows DirectShow for reliable webcam access
- **Simplified Architecture**: Clean, maintainable code structure
- **Better Error Handling**: Clear error messages for camera access issues
- **Automatic Format Handling**: Works with RGB32/BGR32 formats
- **Preview Window**: Embedded DirectShow video preview

## Architecture

- `CaptureWebcam.cs` - Core capture using DirectShow ISampleGrabber and IVideoWindow
- `WebcamCapture.cs` - Thread-safe wrapper implementing IWebcamCapture interface
- `WebcamProvider.cs` - Enumerates available webcam devices
- `WebcamItem.cs` - Represents a single webcam device
- `Filter.cs` - DirectShow device enumeration
- `DummyForm.cs` - Helper form for preview window management

## Dependencies

- DirectShowLib (v1.0.0) - DirectShow .NET wrappers

## Implementation Details

This is a **complete rewrite** of the original DirectShow implementation with focus on:

### Improvements Over Original

1. **Cleaner Code Structure**
   - ~50% simpler than original
   - Better separation of concerns
   - Clear initialization flow

2. **Better Error Handling**
   - Recognizes common DirectShow error codes
   - Provides actionable user guidance
   - Graceful failure modes

3. **Proper Resource Management**
   - Explicit cleanup methods
   - Proper COM object release
   - Thread-safe operations

4. **Robustness**
   - Handles preview pin vs capture pin automatically
   - Supports VideoInfo and VideoInfo2 formats
   - Bottom-up bitmap handling

### Key DirectShow Components

- **IGraphBuilder** - Manages the filter graph
- **ICaptureGraphBuilder2** - Helper for building capture graphs
- **IBaseFilter** - Video device and sample grabber filters
- **ISampleGrabber** - Captures frames from video stream
- **IVideoWindow** - Embedded preview window
- **IMediaControl** - Controls graph playback (Run/Stop)

## How It Works

1. **Device Selection**: User selects webcam from enumerated devices
2. **Graph Building**: Creates DirectShow filter graph with device and sample grabber
3. **Preview Rendering**: Renders video stream to preview window
4. **Frame Capture**: Uses ISampleGrabber.GetCurrentBuffer() for frame access
5. **Display**: Frames converted to bitmaps and displayed/recorded

## Original Attribution

Original DirectShow implementation adapted from [ScreenToGif](https://github.com/NickeManarin/ScreenToGif/)
Licensed under [Microsoft Public License](https://github.com/NickeManarin/ScreenToGif/blob/master/LICENSE.txt).

This version is a complete rewrite with improved reliability and maintainability while keeping DirectShow as the underlying API (which is the correct choice for webcam capture on Windows).
