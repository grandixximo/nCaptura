# Captura 10.3.0 - Classic UI Build

This branch (`classic-ui-modern-fixes`) contains the latest Captura improvements and fixes while maintaining the classic 8.0.0 UI design with a modern twist - crediting all three contributors!

## What's Different from Main

### UI Changes
- **AboutPage**: Classic pre-eb5141e design with modern updates
  - Credits all three authors: Mathew Sachin (original), Mr. Chip (maintainer), grandixximo (current maintainer)
  - GitHub button links to grandixximo/nCaptura repository (current active fork)
  - Classic vertical button layout
  - Includes Crop and other tools
  - Original window styling

### What's Included from Latest (10.3.0)
✅ All bug fixes and stability improvements  
✅ Video source and webcam preview windows working  
✅ Refresh button with shake animations  
✅ Live region coordinate updates  
✅ Audio/video source listing fixes  
✅ Drawing tools functionality  
✅ FFmpeg compatibility fixes  
✅ Mirror download fixes with fallback support  
✅ .NET Framework compatibility improvements  

## Version
- **Version**: 10.3.0
- **Base**: Latest fixes from development
- **UI**: Classic 8.0.0 design with modern attribution
- **Date**: October 7, 2025

## Key Features

### From Classic UI (8.0.0)
- Original About page layout and design
- Classic tool buttons arrangement
- Separate preview windows for video source and webcam
- Crop and trimming windows
- Links to documentation

### Modern Improvements
- **Preview Windows**: Both video source and webcam previews fully functional
- **Refresh Animations**: UI elements shake when refreshing sources
- **Live Updates**: Region coordinates update in real-time during drag/resize
- **Source Listing**: Proper audio/video/webcam device enumeration
- **Drawing Tools**: Functional drawing tools in region selector
- FFmpeg downloader with multiple mirrors and fallback support
- Better error handling and timeout settings
- .NET Framework compatibility fixes
- Windows 11 support
- Updated dependencies

## Building

### Windows (Recommended)
```bash
# Using Visual Studio 2019 or newer
# Open src/Captura.sln and build

# Or using command line
dotnet build src/Captura.sln --configuration Release
```

### Notes
- This build maintains the classic UI aesthetic while including all modern fixes
- Full compatibility with latest Windows versions
- FFmpeg downloads work reliably with modern mirrors
- All recording features fully functional

## Why This Build?

This build is for users who:
- Prefer the original 8.0.0 UI design and layout
- Want the classic separate preview windows
- Like the vertical button arrangement
- But also need all the latest bug fixes and improvements

## What's New in 10.3.0

### Fixed Issues
- ✅ Video source preview window now opens and displays correctly
- ✅ Webcam preview window functional
- ✅ Refresh button triggers visual shake animations on affected UI elements
- ✅ Region coordinates update live in main window during drag/resize
- ✅ Audio sources (microphones/speakers) list correctly
- ✅ Webcam sources enumerate properly
- ✅ Drawing tools checkbox works and is hidden by default
- ✅ UI expands properly when drawing tools are shown

### About Page Updates
- Credits all three contributors with links to their GitHub profiles
- Maintained classic vertical layout
- Removed donation buttons (no longer applicable)
- Updated repository links to current active fork

## Comparison

| Feature | Main Branch (Modern UI) | This Branch (Classic UI) |
|---------|-------------------------|--------------------------|
| All Bug Fixes | ✅ | ✅ |
| Preview Windows | In-window | Separate windows ✨ |
| UI Layout | Modern wrap panels | Classic vertical |
| About Page | Modern compact | Classic spacious |
| All Features | ✅ | ✅ |
| Attribution | All 3 authors | All 3 authors |

## Testing Checklist
- [x] Launch application successfully
- [x] Verify About page shows classic UI with all three authors
- [x] Test video source preview opens in separate window
- [x] Test webcam preview opens in separate window
- [x] Verify refresh button shakes UI elements
- [x] Test region coordinates update live during drag
- [x] Verify audio/video sources list properly
- [x] Test drawing tools show/hide functionality
- [x] Test FFmpeg download with new mirrors
- [x] Verify all recording features work

## License
Maintains the original Captura license. See LICENSE.md for details.

## Contributors

### Original Author
- **Mathew Sachin** - Created Captura, the foundation of this amazing tool

### Maintainers
- **Mr. Chip (mrchipset)** - Kept the project alive and maintained
- **grandixximo** - Current maintainer, bug fixes and improvements
