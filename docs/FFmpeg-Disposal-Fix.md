# FFmpeg Process Disposal Fix

## Problem Description

Users were experiencing issues where:
1. **Video files appeared as 0kb or partial** during/after recording
2. **Files couldn't be deleted** while the application was running
3. **Application hung** when closing after recording
4. **Files only populated properly** after force-closing the application

## Root Cause

The issue was caused by **improper FFmpeg process disposal** in multiple places:

1. **No timeouts on `Process.WaitForExit()`** - Could hang indefinitely if FFmpeg didn't exit cleanly
2. **Missing pipe flush operations** - Data buffered in pipes wasn't being flushed before disposal
3. **Process objects not being disposed** - File handles remained open
4. **No graceful shutdown** - Processes weren't being killed if they hung

This created a situation where:
- Video data was buffered in memory/pipes but not written to disk
- FFmpeg processes kept file handles open
- The application couldn't exit cleanly
- Only when force-killed would the OS flush buffers and close handles

## Solution

Fixed **5 FFmpeg-related classes** with proper disposal patterns:

### 1. FFmpegVideoWriter.cs (Main video writer)
**Changes:**
- Added explicit pipe flushing before disposal
- Added 10-second timeout to `WaitForExit()`
- Force kill process if timeout is exceeded
- Added try/finally to ensure process disposal
- Properly dispose Process object

**Before:**
```csharp
public void Dispose()
{
    _lastFrameTask?.Wait();
    _lastAudio?.Wait();
    _ffmpegIn.Dispose();
    _audioPipe?.Dispose();
    _ffmpegProcess.WaitForExit();  // Could hang forever!
    _videoBuffer = null;
}
```

**After:**
```csharp
public void Dispose()
{
    try
    {
        _lastFrameTask?.Wait();
        _lastAudio?.Wait();
        
        // Flush pipes to ensure data is sent
        try
        {
            _ffmpegIn?.Flush();
            _audioPipe?.Flush();
        }
        catch { }
        
        _ffmpegIn?.Dispose();
        _audioPipe?.Dispose();
        
        // Wait with timeout, force kill if needed
        if (!_ffmpegProcess.WaitForExit(10000))
        {
            try
            {
                _ffmpegProcess.Kill();
                _ffmpegProcess.WaitForExit(2000);
            }
            catch { }
        }
    }
    finally
    {
        _ffmpegProcess?.Dispose();
        _videoBuffer = null;
    }
}
```

### 2. FFmpegAudioWriter.cs
- Added 10-second timeout
- Added process disposal
- Added try/finally for safety

### 3. FFmpegReplayWriter.cs
- Added 30-second timeout (concat operations take longer)
- Added process disposal
- Same graceful shutdown pattern

### 4. FFmpegVideoConverter.cs
- Added 5-minute timeout (conversion can take longer)
- Added TimeoutException for better error reporting
- Added process disposal

### 5. FFmpegGifConverter.cs
- Added 5-minute timeout
- Added TimeoutException
- Added process disposal

## Testing

To verify the fix works:

### Test 1: Basic Recording
1. Start a screen recording with AMF encoder
2. Record for 10-30 seconds
3. **Stop the recording**
4. Check that:
   - ✅ Video file appears immediately
   - ✅ File has correct size (not 0kb)
   - ✅ Video plays correctly
   - ✅ You can delete the file immediately

### Test 2: Multiple Quick Recordings
1. Start recording
2. Stop after 5 seconds
3. Immediately start another recording
4. Stop after 5 seconds
5. Repeat 3-4 times
6. Check that:
   - ✅ All videos are properly saved
   - ✅ No 0kb files
   - ✅ Application remains responsive

### Test 3: Application Exit
1. Start a recording
2. Record for 10 seconds
3. Stop recording
4. **Close the application**
5. Check that:
   - ✅ Application exits cleanly within 1-2 seconds
   - ✅ Video file is complete and playable
   - ✅ No FFmpeg processes left running (check Task Manager)

### Test 4: Long Recording
1. Start a recording
2. Record for 5+ minutes
3. Stop recording
4. Check that:
   - ✅ Video file is immediately accessible
   - ✅ File size is appropriate
   - ✅ Entire recording is present

## Technical Details

### Timeout Values Chosen

- **10 seconds** for normal recording disposal:
  - Adequate time for FFmpeg to finish encoding buffered frames
  - Not too long that users notice delay
  
- **30 seconds** for replay/concat operations:
  - Concatenation requires re-encoding
  - More complex operations need more time
  
- **5 minutes** for conversion operations:
  - Full video conversion can be lengthy
  - Prevents infinite hangs while allowing legitimate long conversions

### Why Flush is Important

Named pipes are buffered. Without explicit flushing:
1. Data sits in pipe buffer
2. FFmpeg hasn't received it yet
3. Closing the pipe loses that data
4. Result: truncated or corrupt video

Flushing ensures:
1. All buffered data is sent to FFmpeg
2. FFmpeg receives EOF signal
3. FFmpeg finishes encoding properly
4. File is complete

### Process Disposal

The `Process` class holds resources including:
- File handles to stdin/stdout/stderr
- Handle to the child process
- Event handlers

Not disposing can lead to:
- File handle leaks
- Memory leaks
- Zombie processes

## Related Issues

This fix resolves:
- Video files being 0kb or incomplete
- "Cannot delete file" errors
- Application hanging on exit
- Need to force-close to get complete videos
- FFmpeg processes remaining in Task Manager

## Backward Compatibility

✅ **Fully backward compatible** - no API changes, only internal disposal improvements.

Existing code continues to work, but now:
- Exits cleanly
- Doesn't hang
- Properly releases resources
- Completes video files immediately
