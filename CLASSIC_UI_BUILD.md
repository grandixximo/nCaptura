# Captura 10.3.0 - Classic UI Build

This branch (`classic-ui-modern-fixes`) contains the latest Captura improvements and fixes while maintaining the classic 8.0.0 UI design.

## What's Different from Main

### UI Changes
- **AboutPage**: Classic pre-eb5141e design with modern updates
  - Credits all three authors: Mathew Sachin, Mr. Chip, and grandixximo
  - GitHub button links to grandixximo/nCaptura repository
  - Classic button layout (vertical instead of wrapped panels)
  - Includes Image Editor and Crop tools
  - Original window design

### What's Included from Latest (10.3.0)
✅ All FFmpeg compatibility fixes
✅ Mirror download fixes with fallback support
✅ .NET Framework compatibility improvements
✅ Windows 11/2022 build fixes
✅ ModernUI assembly dependency fixes
✅ FFmpegDownloader DataContext fixes
✅ Video source preview window fixes
✅ Webcam preview window fixes
✅ Region selector live update fixes
✅ Refresh button shake animations
✅ All bug fixes and stability improvements

## Version
- **Version**: 10.3.0
- **Base**: Latest main branch
- **UI**: Classic 8.0.0 design with modern fixes
- **Date**: October 7, 2025

## Key Features

### From Classic UI (8.0.0)
- Original About page design by MathewSachin
- Classic tool buttons layout
- Image Editor and Crop windows
- Original window styling and layout

### Modern Improvements
- FFmpeg downloader with multiple mirrors:
  - Primary: gyan.dev
  - Fallback 1: GitHub BtbN builds
  - Fallback 2: Alternate gyan.dev URL
- Better error handling and timeout settings
- .NET Framework compatibility fixes
- Windows 11/2022 runner support
- Updated dependencies
- Working preview windows for video source and webcam
- Live region coordinate updates
- Shake animations for refresh operations

## Building

### Windows (Recommended)
```bash
# Using Visual Studio 2019 or newer
# Open src/Captura.sln and build

# Or using Cake build script
dotnet tool install -g Cake.Tool --version 0.32.1
dotnet-cake --configuration=Release
```

### Notes
- This maintains the classic UI layout
- All functionality remains identical to modern UI
- Full compatibility with latest Windows versions
- FFmpeg downloads work reliably with modern mirrors
- Preview windows restored and working

## Why This Build?

This build is for users who:
- Prefer the original 8.0.0 UI design
- Like the classic layout and button arrangement
- But also need modern FFmpeg compatibility and bug fixes
- Want all the latest improvements with familiar interface

## Testing Checklist
- [ ] Launch application successfully
- [ ] Verify About page shows classic UI with all three authors
- [ ] Test FFmpeg download with new mirrors
- [ ] Check GitHub link points to grandixximo/nCaptura
- [ ] Test Image Editor tool
- [ ] Test Crop tool
- [ ] Test video source preview window
- [ ] Test webcam preview window
- [ ] Verify region selector updates coordinates live
- [ ] Test refresh button shake animations
- [ ] Verify all recording features work

## Comparison

| Feature | Modern UI (Main) | Classic UI (This Branch) |
|---------|-----------------|--------------------------|
| FFmpeg Fixes | ✅ | ✅ |
| Bug Fixes | ✅ | ✅ |
| Modern UI Layout | ✅ | ❌ |
| Classic UI Layout | ❌ | ✅ |
| All Three Authors | ✅ | ✅ |
| Image Editor | ✅ | ✅ |
| Preview Windows | ✅ | ✅ |
| All Features | ✅ | ✅ |

## Credits

**Original Author:**
- **Mathew Sachin** - Created Captura

**Maintainer:**
- **Mr. Chip (mrchipset)** - Kept the project alive

**Current Fork:**
- **grandixximo** - Ongoing maintenance and fixes

## License
Maintains the original Captura license. See LICENSE.md for details.
