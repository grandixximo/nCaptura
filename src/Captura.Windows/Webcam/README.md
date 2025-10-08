# Captura.Webcam

DirectShow-based webcam capture implementation.

## Components

- `CaptureWebcam.cs` - Core DirectShow capture using ISampleGrabber
- `WebcamCapture.cs` - Thread-safe wrapper
- `Filter.cs` - Device enumeration
- `WebcamProvider.cs` - Enumerates available cameras
- `WebcamItem.cs` - Represents a camera device
- `DummyForm.cs` - Helper form for preview window

## Dependencies

- DirectShowLib (v1.0.0)

## Attribution

Original DirectShow implementation adapted from [ScreenToGif](https://github.com/NickeManarin/ScreenToGif/)
Licensed under [Microsoft Public License](https://github.com/NickeManarin/ScreenToGif/blob/master/LICENSE.txt).
