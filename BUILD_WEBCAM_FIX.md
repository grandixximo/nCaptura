# Building Captura with Webcam Fix

## Quick Build Instructions

### Option 1: Using Visual Studio (Recommended)
1. Open `src/Captura.sln` in Visual Studio 2019 or newer
2. Set the build configuration to **Debug** (for testing)
3. Build the solution: **Build > Build Solution** (Ctrl+Shift+B)
4. The compiled executable will be in: `src/Captura/bin/Debug/`
5. Run `Captura.exe` from that folder

### Option 2: Using MSBuild from Command Line
```batch
cd src
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" Captura.sln /p:Configuration=Debug
```

## Viewing Diagnostic Logs

After building and running the fixed version, you can view diagnostic logs:

### Method 1: DebugView (Best for real-time viewing)
1. Download **DebugView** from Microsoft: https://learn.microsoft.com/en-us/sysinternals/downloads/debugview
2. Run DebugView.exe **as Administrator**
3. Go to **Capture > Capture Global Win32**
4. Run Captura and try to use the webcam
5. Look for lines starting with `[Captura Webcam]` - they will show exactly what's happening

### Method 2: Visual Studio Output Window
1. Run Captura from Visual Studio (F5 or Debug > Start Debugging)
2. When the webcam issue occurs, check the **Output** window
3. Look for `[Captura Webcam]` messages

## What the Logs Will Show

You should see messages like:
```
[Captura Webcam] Initializing webcam: USB Video Device
[Captura Webcam] Graph created successfully for: USB Video Device
[Captura Webcam] StartPreview called for: USB Video Device
[Captura Webcam] Want preview rendered: True
[Captura Webcam] Rendering preview stream for: USB Video Device
[Captura Webcam] Attempting Preview pin...
[Captura Webcam] Preview pin failed (HRESULT: 0x80040217), trying Capture pin...
[Captura Webcam] SUCCESS: Capture pin worked!
```

## What the Fixes Include

1. **Preview/Capture Pin Fallback**: Modern webcams often don't have a Preview pin, only a Capture pin
2. **Better Error Handling**: Shows detailed error messages for common issues:
   - Windows camera privacy settings blocking access
   - Camera in use by another application
   - Device disconnected or unavailable
3. **Diagnostic Logging**: Detailed trace output to help identify issues

## Troubleshooting

If you still see issues after building:

1. **Check Windows Camera Privacy Settings**:
   - Open Settings > Privacy & Security > Camera
   - Enable "Let desktop apps access your camera"

2. **Close Other Apps Using Camera**:
   - Close Teams, Zoom, Skype, Chrome, etc.

3. **Check Device Manager**:
   - Open Device Manager
   - Look under "Cameras" or "Imaging devices"
   - Make sure your webcam is enabled and has no error icon

4. **Share the Debug Output**:
   - Run with DebugView and copy all `[Captura Webcam]` messages
   - This will show exactly where it's failing