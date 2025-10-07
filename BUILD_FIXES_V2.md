# Build Fixes - Version 2

## Additional Error Fixed

### ✅ Missing OverlayWindow
**Error**: `The name 'OverlayWindow' does not exist in the current context` in ConfigPage.xaml.cs

**Problem**: 
- ConfigPage.xaml has a button that calls `OpenOverlayManager()`
- This method tries to call `OverlayWindow.ShowInstance()`
- OverlayWindow.xaml/.cs were not copied from classic branch

**Solution**:
- Copied `OverlayWindow.xaml` from classic-ui-modern-fixes branch
- Copied `OverlayWindow.xaml.cs` from classic-ui-modern-fixes branch
- OverlayWindow is used to manage text/image/webcam overlays in classic UI

## Complete Classic UI Windows Added

All necessary classic UI windows are now present:

### Main Windows:
- ✅ `PreviewWindow.xaml` / `.xaml.cs` - Video preview window
- ✅ `WebCamWindow.xaml` / `.xaml.cs` - Webcam view window
- ✅ `OverlayWindow.xaml` / `.xaml.cs` - Overlay manager window

### Classic Pages:
- ✅ `MainPageClassic.xaml` / `.xaml.cs` - Main tab interface
- ✅ `ConfigPage.xaml` / `.xaml.cs` - Configuration page
- ✅ `ExtrasPage.xaml` / `.xaml.cs` - Theme/accent color settings

### Controls:
- ✅ `WebcamControl.xaml` / `.xaml.cs` - Webcam control component

### Existing Windows (already in modern UI):
- ✅ `SettingsWindow.xaml` / `.xaml.cs` - Settings dialog
- ✅ `FFmpegDownloaderWindow.xaml` / `.xaml.cs` - FFmpeg downloader

## Window Reference Summary

| Window | Used By | Purpose |
|--------|---------|---------|
| PreviewWindow | PreviewWindowService | Video preview (classic mode) |
| WebCamWindow | WebcamPage | Webcam preview (classic mode) |
| OverlayWindow | ConfigPage | Manage overlays (classic mode) |
| SettingsWindow | MainPage, ConfigPage | Application settings |
| FFmpegDownloaderWindow | FFmpegPage | Download FFmpeg binaries |

## Files Added in This Fix

- `src/Captura/Windows/OverlayWindow.xaml`
- `src/Captura/Windows/OverlayWindow.xaml.cs`

## Build Status

All errors should now be resolved:
- ✅ No missing window references
- ✅ All classic UI components present
- ✅ All modern UI components intact
- ✅ Dual-mode architecture complete

## OverlayWindow Features

The OverlayWindow provides:
- Text overlay configuration
- Image overlay configuration  
- Webcam overlay configuration
- Mouse click overlay settings
- Keystroke overlay settings
- Real-time preview of overlays
- Position and opacity controls

This is a key feature of the classic UI that was missing.