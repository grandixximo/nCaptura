# Webcam Implementation Troubleshooting Guide

## Build Issues

### Issue: MediaFoundation namespace not found
**Error:** `The type or namespace name 'MediaFoundation' could not be found`

**Solution:** 
Ensure the MediaFoundation NuGet package (v3.1.0) is installed:
```
Install-Package MediaFoundation -Version 3.1.0
```

### Issue: DirectShowLib namespace not found
**Error:** `The type or namespace name 'DirectShowLib' could not be found`

**Solution:**
Ensure the DirectShowLib NuGet package (v1.0.0) is installed:
```
Install-Package DirectShowLib -Version 1.0.0
```

### Issue: IMFSourceReader not found
**Error:** `The type or namespace name 'IMFSourceReader' could not be found`

**Solution:**
This is defined in the MediaFoundation package. Verify:
1. Package is properly installed
2. Using statement is present: `using MediaFoundation;`
3. Clean and rebuild solution

---

## Runtime Issues

### Issue: Camera not detected
**Symptoms:** No cameras appear in dropdown

**Solutions:**
1. Check if camera is connected and working (test in Windows Camera app)
2. Check Windows Device Manager for camera drivers
3. Try clicking the Refresh button
4. Check application logs for enumeration errors

### Issue: "Camera Access Denied" error (0x80070005)
**Symptoms:** Error message about Windows blocking camera access

**Solutions:**
1. Open Windows Settings → Privacy & security → Camera
2. Turn on "Camera access"
3. Turn on "Let apps access your camera"
4. Turn on "Let desktop apps access your camera"
5. Restart the application

**Windows 10:**
Settings → Privacy → Camera

**Windows 11:**
Settings → Privacy & security → Camera

### Issue: "Camera Not Available" error (0xC00D3E86)
**Symptoms:** Camera is in use by another application

**Solutions:**
1. Close other apps that might be using the camera:
   - Skype, Zoom, Teams
   - Windows Camera app
   - Browser tabs with camera access
   - Other recording software
2. Restart the application
3. In extreme cases, restart the computer

### Issue: Black screen or no preview
**Symptoms:** Webcam selected but no preview shows

**Possible Causes:**
1. **UI not polling for frames**: The new MediaFoundation implementation doesn't provide an embedded preview window like DirectShow. The UI needs to call `Capture()` repeatedly to get frames.

2. **Camera initialization failed silently**: Check application logs

3. **Format not supported**: Some cameras may not support RGB32 output

**Solutions:**
1. Verify the UI has frame polling/rendering mechanism
2. Check logs for initialization errors
3. Try a different camera
4. Ensure webcam overlay is enabled if recording with overlay

### Issue: Low frame rate / stuttering
**Symptoms:** Preview or recording is choppy

**Solutions:**
1. Close other applications using the camera
2. Check CPU usage (recording is CPU-intensive)
3. Try lower recording resolution
4. Update camera drivers
5. Try USB 3.0 port instead of USB 2.0

### Issue: Camera disconnects during recording
**Symptoms:** Recording fails or stops when camera disconnects

**Solution:**
This is expected behavior. The implementation includes proper cleanup:
1. Any in-progress recording should fail gracefully
2. Reconnect camera and start new recording
3. Consider using built-in laptop camera for more stable connection

---

## Code-Level Issues

### Issue: Marshal.ThrowExceptionForHR exceptions
**Symptoms:** Unhandled COM exceptions with HRESULT codes

**Common HRESULTs:**
- `0x80070005` - E_ACCESSDENIED (privacy settings)
- `0xC00D3E86` - MF_E_VIDEO_RECORDING_DEVICE_INVALIDATED (in use)
- `0xC00D36E6` - MF_E_ATTRIBUTENOTFOUND (config error)
- `0xC00D36B3` - MF_E_INVALIDMEDIATYPE (format not supported)

**Solutions:**
1. Check error handling in `WebcamCapture.HandleCameraException()`
2. Ensure proper try-catch blocks around MediaFoundation calls
3. Add logging to identify exact failure point

### Issue: Memory leak or high memory usage
**Symptoms:** Memory increases over time

**Check:**
1. Verify all COM objects are released (Marshal.ReleaseComObject)
2. Ensure Dispose() is called on capture objects
3. Check frame buffer isn't growing unbounded
4. Verify GCHandle.Free() is called after pinning buffers

**In Code:**
- `CaptureWebcam.Cleanup()` should release all COM objects
- `WebcamCapture.Dispose()` should be called
- Frame buffers should be reused, not reallocated every frame

### Issue: Thread safety issues
**Symptoms:** Random crashes or undefined behavior

**Check:**
1. Verify `_lock` is used around all MediaFoundation calls
2. SyncContextManager is properly used in WebcamCapture
3. No race conditions in frame capture

---

## Testing Checklist

Use this to verify the implementation works:

### Basic Tests
- [ ] Build succeeds without errors
- [ ] Application starts without crashes
- [ ] Webcam dropdown populates with available cameras
- [ ] Can select a camera
- [ ] Can switch between cameras
- [ ] Width and Height properties return valid values

### Functional Tests
- [ ] Can capture still image from webcam
- [ ] Can record video with webcam overlay
- [ ] Can record separate webcam file
- [ ] Preview updates when changing settings
- [ ] Cleanup happens on camera deselect

### Error Handling Tests
- [ ] Clear message when no camera connected
- [ ] Clear message when camera access denied
- [ ] Clear message when camera in use
- [ ] Graceful failure when camera unplugged during use
- [ ] No crashes on invalid operations

### Edge Cases
- [ ] Works with USB webcam
- [ ] Works with built-in laptop camera
- [ ] Works with multiple cameras connected
- [ ] Handles rapid camera selection changes
- [ ] Proper cleanup on application exit

---

## Debug Tips

### Enable Detailed Logging
Add logging to these key points:
1. `Filter.EnumerateMediaFoundationDevices()` - device enumeration
2. `CaptureWebcam.InitializeDevice()` - initialization
3. `CaptureWebcam.GetFrame()` - frame capture
4. `WebcamCapture.HandleCameraException()` - error handling

### Use Process Monitor
Microsoft Process Monitor can help identify:
- Registry access for privacy settings
- File system access for drivers
- COM object creation

### Check Event Viewer
Windows Event Viewer may contain:
- Application errors with stack traces
- System errors related to camera drivers
- COM activation errors

### Debug MediaFoundation
Tools:
- **GraphStudioNext** - Not needed for MF, but useful for DirectShow debugging
- **MF Trace** - Media Foundation trace utility
- **WinDbg** - For low-level debugging

---

## Getting Help

If issues persist:

1. **Check Logs**: Enable verbose logging and examine output
2. **Event Viewer**: Check Windows event logs for system errors
3. **Test System**: Try on different computer to isolate hardware issues
4. **Camera Test**: Verify camera works in Windows Camera app
5. **Driver Update**: Update camera drivers to latest version
6. **Clean Install**: Uninstall/reinstall application

---

## Known Limitations

1. **Preview Window**: No embedded DirectShow preview window. UI must poll `Capture()` for frames.
2. **Format**: Currently hardcoded to RGB32. Some cameras may need different formats.
3. **Resolution**: Uses camera's default resolution. No resolution selection yet.
4. **Multiple Cameras**: Can only use one camera at a time per instance.

---

## Contact

For persistent issues:
1. Check GitHub issues
2. Review implementation notes in IMPLEMENTATION_NOTES.md
3. Consult MediaFoundation documentation: https://docs.microsoft.com/en-us/windows/win32/medfound/
