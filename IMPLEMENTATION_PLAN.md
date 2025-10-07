# Complete UI Switching Implementation Plan

## Analysis of Current Issues

### Problems Identified:
1. ❌ **Frame navigation causes web-like back/forward buttons** - Frame control has built-in navigation UI
2. ❌ **Preview area still visible in classic mode** - Should be completely hidden
3. ❌ **UI shrinks** - Classic UI has different layout structure
4. ❌ **Button layout is wrong** - Modern UI uses CollapsedBar, ScreenShotButton, PauseButton controls that don't exist in classic
5. ❌ **Using Navigate() instead of proper content swapping**

## Root Cause

The fundamental issue is that **Modern UI and Classic UI have COMPLETELY DIFFERENT MainWindow structures**:

### Modern UI MainWindow:
```
- Expander.Header with fancy styling (CollapsedBar, special borders)
- Custom controls: ScreenShotButton, PauseButton
- Expander content has:
  - Frame → HomePage.xaml (simple)
  - Preview area (WinFormsHost + D3DImage) - EMBEDDED IN MAIN WINDOW
```

### Classic UI MainWindow:
```
- Expander.Header with simple button layout
- Standard ModernButton controls only
- Expander content has:
  - Frame → MainPage.xaml (complex tab structure)
  - NO PREVIEW AREA AT ALL
  - Preview is in separate PreviewWindow
```

## The Right Solution

We need TWO DIFFERENT MainWindow layouts, not just Frame navigation.

## Complete Implementation Plan

### Phase 1: Create Dual MainWindow Structure ✅ PRIORITY
- [ ] Create MainWindow_Modern.xaml (current modern layout)
- [ ] Create MainWindow_Classic.xaml (classic layout from branch)
- [ ] Modify MainWindow.xaml to be a shell that loads either layout
- [ ] Remove Frame navigation - use ContentControl instead
- [ ] Hide/show preview area based on UI mode

### Phase 2: Fix Header Layout
- [ ] Remove CollapsedBar dependency from classic mode
- [ ] Remove ScreenShotButton/PauseButton custom controls from classic mode
- [ ] Use simple ModernButton layout in classic header
- [ ] Keep fancy styling for modern mode only

### Phase 3: Fix Content Area
- [ ] Modern: Load HomePage.xaml directly (no tabs)
- [ ] Classic: Load MainPageClassic.xaml (with tabs)
- [ ] Modern: Show embedded preview area
- [ ] Classic: Hide preview area completely
- [ ] Classic: Add "Show Preview" button to open PreviewWindow

### Phase 4: Fix Preview Service
- [ ] Modern mode: Use embedded preview (current implementation)
- [ ] Classic mode: ONLY use PreviewWindow (no embedded preview)
- [ ] Ensure PreviewWindow shows when clicking button in classic mode

### Phase 5: Fix Window Sizing
- [ ] Modern: MaxWidth="440", compact layout
- [ ] Classic: MaxWidth="450", allow more space for tabs
- [ ] Fix Height calculation for both modes

### Phase 6: Fix Navigation
- [ ] Remove Frame.Navigate() calls
- [ ] Use ContentControl with direct content loading
- [ ] No back/forward navigation - direct UI replacement

### Phase 7: Testing
- [ ] Test switching from Modern to Classic
- [ ] Test switching from Classic to Modern
- [ ] Verify preview works in both modes
- [ ] Verify all buttons work correctly
- [ ] Verify no web-like navigation appears
- [ ] Test recording in both modes

## Detailed Task Breakdown

### Task 1: Separate Modern UI Layout
**File**: `src/Captura/Windows/MainWindow_Modern.xaml`
- Copy current modern MainWindow.xaml Expander.Header
- Keep CollapsedBar, ScreenShotButton, PauseButton
- Keep embedded preview area
- Frame source: HomePage.xaml

### Task 2: Separate Classic UI Layout  
**File**: `src/Captura/Windows/MainWindow_Classic.xaml`
- Copy classic MainWindow.xaml Expander.Header from branch
- Use only ModernButton controls
- NO preview area
- Frame source: MainPageClassic.xaml
- Add button to show PreviewWindow

### Task 3: Create Shell MainWindow
**File**: `src/Captura/Windows/MainWindow.xaml`
- Keep tray icon and chrome
- Use ContentControl to hold either modern or classic layout
- Switch content based on Settings.UI.UseClassicUI
- No Frame navigation

### Task 4: Update MainWindow Code-Behind
**File**: `src/Captura/Windows/MainWindow.xaml.cs`
- Load appropriate layout on startup
- Switch layouts when toggle clicked
- Dispose/recreate content properly
- No Navigate() calls

### Task 5: Create Layout Loading Logic
```csharp
void LoadModernLayout()
{
    // Load Modern UI structure
    // Show embedded preview
    // Hide preview window
}

void LoadClassicLayout()
{
    // Load Classic UI structure
    // Hide embedded preview completely
    // Use separate PreviewWindow
}
```

## Why This Approach is Correct

1. **No Frame Navigation** - Direct content replacement, no back/forward buttons
2. **Complete Layout Swap** - Not just page swap, but entire structure changes
3. **Preview Properly Handled** - Embedded vs. Separate window
4. **Clean Separation** - Each UI mode has its own XAML
5. **No Hybrid Issues** - Each mode is pure, no mixing

## Timeline

1. **Task 1-2**: Create separate XAML files (30 min)
2. **Task 3-4**: Modify shell and code-behind (45 min)
3. **Task 5**: Implement switching logic (30 min)
4. **Testing**: Verify all functionality (30 min)

**Total**: ~2-3 hours for complete, proper implementation

## Expected Result

✅ Toggle button switches entire window layout
✅ Modern: Compact, embedded preview, simple HomePage
✅ Classic: Tabs, separate PreviewWindow, full MainPage structure
✅ No navigation buttons
✅ No shrinking
✅ Preview works correctly in both modes
✅ Clean, fast switching without artifacts

## Current vs. Proper Approach

| Current (Wrong) | Proper (Right) |
|-----------------|----------------|
| Frame.Navigate() | ContentControl content swap |
| Same MainWindow layout | Different MainWindow layouts |
| Preview always visible | Preview shown/hidden per mode |
| Navigation buttons appear | No navigation |
| Hybrid/broken UI | Clean, separated UIs |