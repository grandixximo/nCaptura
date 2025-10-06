# Captura 10.0.5 - Classic UI Build

This branch (`classic-ui-modern-fixes`) contains the latest Captura improvements and fixes while maintaining the classic 8.0.0 UI design.

## What's Different from Main

### UI Changes
- **AboutPage**: Reverted to classic pre-eb5141e design
  - Shows only MathewSachin as the original author
  - GitHub button links to original MathewSachin/Captura repository
  - PayPal donation button is visible
  - Classic button layout (vertical instead of wrapped panels)
  - Includes Image Editor and Crop tools
  - Original window design

### What's Included from Latest (10.0.4)
✅ All FFmpeg compatibility fixes
✅ Mirror download fixes with fallback support
✅ .NET Framework compatibility improvements
✅ Windows 2022 build fixes
✅ ModernUI assembly dependency fixes
✅ FFmpegDownloader DataContext fixes
✅ All bug fixes and stability improvements

## Version
- **Version**: 10.0.5
- **Base**: Latest main branch (10.0.4)
- **UI**: Classic 8.0.0 design (pre-eb5141e)
- **Date**: October 6, 2025

## Commits
```
2cde888 Revert AboutPage to classic 8.0.0 UI (pre-eb5141e)
b69bf09 Merge pull request #9 from grandixximo/cursor/finalize-10-0-3-release-and-clean-drafts-d9e2
5e374f0 Update README with new copyright and build status
5b9ccb3 Update version and add contributor to About page
be911b7 Fix FFmpegDownloaderWindow DataContext initialization
... (all improvements from main branch)
```

## Key Features

### From Classic UI (8.0.0)
- Original About page design by MathewSachin
- PayPal donation support
- Classic tool buttons layout
- Image Editor and Crop windows
- Links to original documentation

### Modern Improvements
- FFmpeg downloader with multiple mirrors:
  - Primary: gyan.dev
  - Fallback 1: GitHub BtbN builds
  - Fallback 2: Alternate gyan.dev URL
- Better error handling and timeout settings
- .NET Framework compatibility fixes
- Windows 2022 runner support
- Updated dependencies

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
- This is the ONLY UI change from the main branch
- All other functionality remains identical
- Full compatibility with latest Windows versions
- FFmpeg downloads work reliably with modern mirrors

## Why This Build?

This build is for users who:
- Prefer the original 8.0.0 UI design
- Want to support the original author
- Like the classic layout and button arrangement
- But also need modern FFmpeg compatibility and bug fixes

## Testing Checklist
- [ ] Launch application successfully
- [ ] Verify About page shows classic UI
- [ ] Test FFmpeg download with new mirrors
- [ ] Verify PayPal donation button works
- [ ] Check GitHub link points to MathewSachin/Captura
- [ ] Test Image Editor tool
- [ ] Test Crop tool
- [ ] Verify all recording features work

## Comparison

| Feature | Main Branch (10.0.4) | This Branch (10.0.5) |
|---------|---------------------|---------------------|
| FFmpeg Fixes | ✅ | ✅ |
| Bug Fixes | ✅ | ✅ |
| Modern UI | ✅ | ❌ |
| Classic UI | ❌ | ✅ |
| Multi-author Credits | ✅ | ❌ |
| PayPal Donate | ❌ | ✅ |
| Image Editor | ✅ | ✅ |
| All Features | ✅ | ✅ |

## License
Maintains the original Captura license. See LICENSE.md for details.
