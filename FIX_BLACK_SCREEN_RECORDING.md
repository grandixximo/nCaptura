# Fix for Black Screen Recording on Fullscreen/Monitor Capture

## Problem
On certain laptop configurations, fullscreen and monitor capture produce black recordings, while window and region capture work correctly. This issue exists in mrchipset's fork but not in Sachin's original release.

## Root Cause
The issue is in how the monitor handle (HMONITOR) is obtained for Windows Graphics Capture (WGC) API:

**Previous Implementation:**
- `WgcScreenImageProvider` used `MonitorHelper.GetMonitorFromRect()` 
- This function uses `MonitorFromPoint()` with the center point of the screen rectangle
- On some laptop configurations (especially with unusual multi-monitor setups or hybrid graphics), this can return an incorrect or invalid monitor handle
- An invalid monitor handle causes WGC to fail silently, resulting in black frames

**Why Window Capture Works:**
- Window capture uses `WgcImageProvider` which takes a window handle (HWND), not a monitor handle
- Window handles are more reliable and don't have the same identification issues

## Solution
Use the DXGI API's monitor handle instead of calculating it from a point:

1. **`WindowsPlatformServices.GetScreenProvider()`** - Modified to:
   - Call `FindOutput()` to get the DXGI `Output1` for the screen
   - Extract the monitor handle from `Output.Description.Monitor` (this is the actual HMONITOR from DXGI)
   - Pass this handle to `WgcScreenImageProvider`
   - Falls back to `MonitorHelper` if DXGI Output is not found

2. **`WindowsPlatformServices.GetAllScreensProvider()`** - Modified to:
   - Get the primary screen's DXGI `Output1`
   - Use its `Output.Description.Monitor` handle for the fullscreen capture
   - Falls back to `MonitorHelper` if DXGI Output is not found

3. **`WgcScreenImageProvider` constructor** - Modified to:
   - Accept an optional `IntPtr? monitorHandle` parameter
   - Use the provided handle if available, otherwise fallback to `MonitorHelper.GetMonitorFromRect()`

## Files Changed
1. `/workspace/src/Captura.Windows/WindowsPlatformServices.cs`
2. `/workspace/src/Captura.Windows/WindowsGraphicsCapture/WgcScreenImageProvider.cs`

## Why This Fix Works
- The DXGI API provides accurate monitor handles through `Output.Description.Monitor`
- This is the same handle that the underlying graphics driver uses
- By using the handle from DXGI instead of calculating it from a point, we ensure consistency
- The Desktop Duplication method already uses DXGI Outputs, so this aligns WGC with the same approach

## Testing
To test this fix:
1. Build the application using Visual Studio or MSBuild on Windows
2. Run on the affected laptop
3. Try fullscreen capture - should now show actual content instead of black screen
4. Try monitor selection - should now show actual content instead of black screen
5. Verify that window and region capture still work (they should be unaffected)

## Backward Compatibility
- The fix maintains backward compatibility with fallback to `MonitorHelper` if DXGI Output is not found
- No breaking changes to existing APIs
- Window and region capture are completely unaffected

## Related Information
- DXGI_OUTPUT_DESC structure: https://docs.microsoft.com/en-us/windows/win32/api/dxgi/ns-dxgi-dxgi_output_desc
- The `Monitor` field in DXGI_OUTPUT_DESC is the official HMONITOR handle for the display
- This is more reliable than using `MonitorFromPoint()` which can fail on complex display configurations
