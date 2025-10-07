# Testing Guide - Runtime UI Switching

## Quick Start Testing

### 1. Build the Application
```bash
dotnet build src/Captura.sln --configuration Release
```

### 2. Run the Application
```bash
cd src/Captura/bin/Release
./Captura.exe
```

## Test Scenarios

### Scenario 1: Default Startup (Modern UI)
**Expected**: Application starts in Modern UI mode

**Verify**:
- [ ] Window width is compact (~440px)
- [ ] Simple HomePage visible
- [ ] Preview area embedded at bottom
- [ ] No "Show Preview Window" button
- [ ] FPS counter visible
- [ ] Fancy header with rounded borders

### Scenario 2: Switch to Classic UI
**Steps**:
1. Click the Contrast icon button (between time and Minimize)
2. Click OK on confirmation dialog

**Expected**:
- [ ] UI instantly changes to Classic layout
- [ ] Tab-based interface appears (Home, Video, Audio, etc.)
- [ ] "Show Preview Window" button visible at bottom
- [ ] No embedded preview area
- [ ] Output folder control visible
- [ ] Copyright label visible
- [ ] Window slightly wider (~450px)

**Verify No Issues**:
- [ ] No navigation back/forward buttons
- [ ] No UI shrinking
- [ ] All tabs accessible
- [ ] All buttons work

### Scenario 3: Use Classic Preview
**Steps** (in Classic mode):
1. Click "Show Preview Window" button

**Expected**:
- [ ] Separate PreviewWindow opens
- [ ] Window has fullscreen toggle button
- [ ] Stretch mode selector visible
- [ ] Preview updates when recording

### Scenario 4: Switch Back to Modern UI
**Steps** (in Classic mode):
1. Click Toggle UI button again
2. Click OK on confirmation

**Expected**:
- [ ] UI switches back to Modern layout
- [ ] PreviewWindow automatically closes
- [ ] Embedded preview appears
- [ ] Simple HomePage loads
- [ ] FPS counter visible

**Verify**:
- [ ] No navigation buttons
- [ ] Correct layout
- [ ] All functionality works

### Scenario 5: Restart Persistence
**Steps**:
1. Switch to Classic UI
2. Close application
3. Restart application

**Expected**:
- [ ] Application starts in Classic UI mode (remembered setting)

**Steps**:
1. Switch to Modern UI
2. Close application
3. Restart application

**Expected**:
- [ ] Application starts in Modern UI mode (remembered setting)

### Scenario 6: Recording in Modern Mode
**Steps**:
1. Ensure in Modern mode
2. Select a video source
3. Start recording

**Expected**:
- [ ] Preview shows in embedded area
- [ ] Recording works correctly
- [ ] FPS counter updates
- [ ] Timer shows in header

### Scenario 7: Recording in Classic Mode
**Steps**:
1. Switch to Classic mode
2. Click "Show Preview Window"
3. Select a video source
4. Start recording

**Expected**:
- [ ] Preview shows in separate PreviewWindow
- [ ] Recording works correctly
- [ ] Timer shows in header
- [ ] All tabs still accessible

### Scenario 8: Tab Navigation (Classic Only)
**Steps** (in Classic mode):
1. Click through all tabs: Home, Video, Audio, Webcam, etc.

**Expected**:
- [ ] All tabs load correctly
- [ ] No errors
- [ ] Content displays properly
- [ ] No navigation buttons

## Visual Verification

### Modern UI Should Look Like:
```
┌─────────────────────────────────────────┐
│ 📷 ⏺️  ⏸️        [Timer]      ❌ ↓ ─ 🌓 │ Header
├─────────────────────────────────────────┤
│                                         │
│  [Video Source Selector]                │
│  [Audio Source Selector]                │
│  [Webcam Selector]                      │
│                                         │
│─────────────────────────────────────────│
│ FPS: 60                                 │
│─────────────────────────────────────────│
│            Preview Area                 │
│        [Embedded Preview]               │
│                                         │
└─────────────────────────────────────────┘
```

### Classic UI Should Look Like:
```
┌──────────────────────────────────────────┐
│ 📷 ⏺️  ⏸️        [Timer]      ❌ ↓ ─ 🌓  │ Header
├──────────────────────────────────────────┤
│ 🏠│                                      │
│ 📹│  [Tab Content]                       │
│ 🎤│                                      │
│ 📷│                                      │
│ ⚙️│                                      │
│ 📁│                                      │
│───│                                      │
│   │                                      │
│   │                                      │
├──────────────────────────────────────────┤
│  [Show Preview Window]                   │
│  [Output Folder: C:\...]                 │
│  © Mathew Sachin                         │
└──────────────────────────────────────────┘
```

## Common Issues to Watch For

### ❌ Issues That Should NOT Occur:
- [ ] No back/forward navigation buttons
- [ ] No UI shrinking when switching
- [ ] No preview visible in Classic content area
- [ ] No errors in console
- [ ] No memory leaks

### ✅ Expected Behaviors:
- [ ] Instant switching (< 100ms)
- [ ] Smooth UI transitions
- [ ] All buttons responsive
- [ ] Preview works in both modes
- [ ] Settings save correctly

## Performance Testing

### Rapid Switching Test:
**Steps**:
1. Click Toggle UI button 10 times rapidly

**Expected**:
- [ ] No crashes
- [ ] No memory buildup
- [ ] UI responds correctly each time
- [ ] No visual artifacts

### Long Session Test:
**Steps**:
1. Use Modern UI for 30 minutes
2. Switch to Classic UI
3. Use Classic UI for 30 minutes
4. Switch back to Modern UI

**Expected**:
- [ ] No performance degradation
- [ ] No memory leaks
- [ ] UI remains responsive

## Edge Case Testing

### Edge Case 1: Switch During Recording
**Steps**:
1. Start recording in Modern mode
2. Try to switch UI mode

**Expected**: *(Implementation may block switching during recording - verify intended behavior)*

### Edge Case 2: Multiple Windows Open
**Steps** (in Classic mode):
1. Open PreviewWindow
2. Open OverlayWindow
3. Switch to Modern UI

**Expected**:
- [ ] PreviewWindow closes
- [ ] Other windows remain (or close as appropriate)

### Edge Case 3: First Launch
**Steps**:
1. Delete settings file
2. Launch application

**Expected**:
- [ ] Modern UI shown by default
- [ ] No errors
- [ ] Toggle button works

## Reporting Issues

When reporting issues, include:
1. UI mode (Modern or Classic)
2. Steps to reproduce
3. Expected behavior
4. Actual behavior
5. Screenshots if applicable
6. Console output if errors occur

## Success Criteria

All scenarios should PASS with:
- ✅ No errors or warnings
- ✅ No navigation artifacts
- ✅ No UI shrinking
- ✅ Preview works correctly
- ✅ Settings persist
- ✅ All functionality works in both modes

---

**Happy Testing!** 🧪