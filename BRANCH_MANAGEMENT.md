# 🎯 BRANCH MANAGEMENT GUIDE FOR nCaptura

## Branch Structure Overview

### `main` - Modern UI Branch
- **UI Type:** Modern, minimalist design (post-v9.0)
- **Key Files PRESENT:** All standard pages (About, Audio, Home, Webcam, etc.)
- **Key Files ABSENT:** 
  - ❌ NO `ConfigPage.xaml/cs`
  - ❌ NO `ExtrasPage.xaml/cs` 
  - ❌ NO `src/Captura/Windows/CropWindow.*`
  - ❌ NO `src/Captura/Windows/FFmpegLogWindow.*`
  - ❌ NO `src/Captura/Windows/PreviewWindow.*`
  - ❌ NO `src/Captura/Controls/WebcamControl.*`

### `classic-ui-modern-fixes` - Classic UI Branch
- **UI Type:** Classic/original design (v8.0.0 base)
- **Key Files PRESENT:** Modern pages PLUS:
  - ✅ `ConfigPage.xaml/cs`
  - ✅ `ExtrasPage.xaml/cs`
  - ✅ `CrashLogsPage.xaml/cs`
  - ✅ `FFmpegLogsPage.xaml/cs`
  - ✅ `src/Captura/Windows/CropWindow.*`
  - ✅ `src/Captura/Windows/FFmpegLogWindow.*`
  - ✅ `src/Captura/Windows/LicensesWindow.*`
  - ✅ `src/Captura/Windows/OverlayWindow.*`
  - ✅ `src/Captura/Windows/PreviewWindow.*`
  - ✅ `src/Captura/Windows/WebCamWindow.*`
  - ✅ `src/Captura/Controls/WebcamControl.*`

---

## Files Safe to Modify in BOTH Branches

These backend/core files are typically identical:

### ✅ ALWAYS SAFE (Backend Logic):
```
src/Captura.Core/                    - Core business logic
src/Captura.Audio/                   - Audio handling
src/Captura.FFmpeg/                  - FFmpeg integration
src/Captura.Windows/                 - Windows-specific backend
  ├── MediaFoundation/               - MF video encoding
  ├── Webcam/CaptureWebcam.cs        - DirectShow capture
  └── Webcam/Filter.cs               - Webcam enumeration
src/Screna/                          - Screen capture
src/Captura.Hotkeys/                 - Global hotkeys
src/Captura.Imgur/                   - Imgur upload
```

### ⚠️ CHECK BEFORE MODIFYING (Shared ViewModels):
```
src/Captura.ViewCore/
  ├── ViewModels/                    - Most are safe
  ├── RememberByName.cs              - Check for UI-specific references
  └── ViewConditionsModel.cs         - Usually safe
```

### 🚫 NEVER Cherry-Pick Blindly (UI-Specific):
```
src/Captura/
  ├── Pages/*.xaml                   - DIFFERENT structure per branch
  ├── Pages/*.xaml.cs                - References XAML elements
  ├── Windows/*.xaml                 - DIFFERENT windows exist
  ├── Windows/*.xaml.cs              - References XAML elements
  └── Controls/*.xaml                - DIFFERENT controls
```

---

## Workflow for Making Changes

### Scenario 1: Backend Logic Change (MfWriterProvider, FFmpeg, etc.)

```bash
# 1. Make change on ONE branch (choose either)
git checkout main
# Edit src/Captura.Windows/MediaFoundation/MfWriterProvider.cs
git add src/Captura.Windows/MediaFoundation/MfWriterProvider.cs
git commit -m "Fix: Improve MF encoder detection"
git push origin main

# 2. Apply to other branch via cherry-pick
git checkout classic-ui-modern-fixes
git cherry-pick main
git push origin classic-ui-modern-fixes
```

**✅ Safe because:** Backend files are identical in both branches

---

### Scenario 2: UI-Specific Change (WebcamPage, etc.)

```bash
# 1. FIRST check if file structure is different
git diff main:src/Captura/Pages/WebcamPage.xaml \
         classic-ui-modern-fixes:src/Captura/Pages/WebcamPage.xaml

# 2a. If OUTPUT (files are different):
#     Make changes SEPARATELY on each branch
#     DO NOT cherry-pick

# 2b. If NO OUTPUT (files are identical):
#     Can cherry-pick safely

# 3. When making UI changes:
git checkout main
# Make changes to WebcamPage.xaml.cs
# CHECK: Does classic have same XAML elements?
grep "PreviewGrid\|PreviewTarget" src/Captura/Pages/WebcamPage.xaml
# If found, note the names

git checkout classic-ui-modern-fixes
grep "PreviewGrid\|PreviewTarget" src/Captura/Pages/WebcamPage.xaml
# If NOT found or different names → DON'T cherry-pick from main!
```

---

### Scenario 3: Shared ViewModel Change (RememberByName, etc.)

```bash
# 1. Check if it references UI-specific properties
git diff main:src/Captura.ViewCore/RememberByName.cs \
         classic-ui-modern-fixes:src/Captura.ViewCore/RememberByName.cs

# 2. If files are identical → Safe to cherry-pick
# 3. If files differ → Check what's different and apply manually
```

---

## Pre-Flight Checks

### Before ANY Cherry-Pick:

```bash
# Step 1: What's in the commit?
git show <commit-hash> --stat

# Step 2: Does it touch UI files?
git show <commit-hash> --stat | grep -E "\.xaml|Pages/|Windows/|Controls/"

# Step 3: If YES to UI files:
#   - Compare files between branches first
#   - Apply changes manually if different
#   - DON'T cherry-pick blindly

# Step 4: If NO UI files (backend only):
#   - Cherry-pick is usually safe
#   - Still test the build after!
```

---

## Quick Branch Identification

### Check if you're on the correct branch:

```bash
# Method 1: Check current branch
git branch --show-current

# Method 2: Check for classic UI markers
ls src/Captura/Pages/ | grep ConfigPage
# If found → You're on classic-ui-modern-fixes
# If not found → You're on main

# Method 3: Count windows
ls src/Captura/Windows/*.xaml | wc -l
# Main: ~10 windows
# Classic: ~18 windows
```

---

## Recovery Procedures

### If Main Has Classic UI Files (WRONG):

```bash
git checkout main

# Remove classic-only files
rm -f src/Captura/Pages/ConfigPage.*
rm -f src/Captura/Pages/ExtrasPage.*
rm -f src/Captura/Controls/WebcamControl.*
rm -f src/Captura/Windows/CropWindow.*
rm -f src/Captura/Windows/FFmpegLogWindow.*
rm -f src/Captura/Windows/LicensesWindow.*
rm -f src/Captura/Windows/OverlayWindow.*
rm -f src/Captura/Windows/PreviewWindow.*
rm -f src/Captura/Windows/WebCamWindow.*

git add -A
git commit -m "Remove classic UI files from modern UI branch"
git push origin main --force  # Force if needed
```

### If Classic Missing Classic UI Files (WRONG):

```bash
git checkout classic-ui-modern-fixes

# Find last good commit with classic files
git log --oneline | grep -E "classic UI|Restore|v8.0"

# Reset to that commit
git reset --hard <good-commit>
git push origin classic-ui-modern-fixes --force
```

### If Branches are Completely Mixed Up:

```bash
# Find known good commits
# Main modern UI: Look for "Remove classic UI files" or similar
# Classic UI: Look for "Add multi-codec" or "classic UI restoration"

git checkout main
git reset --hard <good-modern-commit>
git push origin main --force

git checkout classic-ui-modern-fixes
git reset --hard <good-classic-commit>
git push origin classic-ui-modern-fixes --force
```

---

## Golden Rules to Prevent Confusion

1. **ALWAYS verify branch before changes:**
   ```bash
   git branch --show-current
   ls src/Captura/Pages/ | grep Config  # Should return nothing on main
   ```

2. **NEVER cherry-pick commits that touch .xaml files without checking first**

3. **Backend changes (.cs in Captura.Windows/, Captura.Core/):**
   - ✅ Usually safe to cherry-pick
   
4. **UI changes (.xaml, Pages/, Windows/, Controls/):**
   - ⚠️ Check file structure first
   - 🚫 Often need manual application

5. **When fixing bugs affecting both:**
   - Fix on one branch
   - Test build succeeds
   - Check if other branch has same file structure
   - Apply accordingly

6. **Test builds after EVERY change:**
   - Even "safe" backend changes can break if dependencies differ

---

## Common Mistakes to Avoid

### ❌ DON'T:
```bash
# Cherry-pick without checking
git cherry-pick main  # DANGER!

# Assume files are the same
# Edit WebcamPage.xaml.cs without checking XAML

# Force push without verifying state
git push origin main --force  # Without checking what you're overwriting
```

### ✅ DO:
```bash
# Always check first
git show main --stat
git diff main:path classic:path

# Verify after changes
git status
ls src/Captura/Pages/ | grep Config  # Should match branch type

# Test build locally or wait for CI
```

---

## File Structure Reference

### Modern UI Only (main):
```
src/Captura/
├── Pages/
│   ├── AboutPage.xaml
│   ├── AudioPage.xaml
│   ├── HomePage.xaml
│   ├── WebcamPage.xaml
│   └── ... (NO ConfigPage, NO ExtrasPage)
└── Windows/
    ├── MainWindow.xaml
    ├── SettingsWindow.xaml
    └── ... (~10 windows total)
```

### Classic UI (classic-ui-modern-fixes):
```
src/Captura/
├── Pages/
│   ├── AboutPage.xaml
│   ├── AudioPage.xaml
│   ├── ConfigPage.xaml          ← Classic only
│   ├── ExtrasPage.xaml          ← Classic only
│   ├── CrashLogsPage.xaml       ← Classic only
│   ├── FFmpegLogsPage.xaml      ← Classic only
│   └── ...
├── Windows/
│   ├── CropWindow.xaml          ← Classic only
│   ├── FFmpegLogWindow.xaml     ← Classic only
│   ├── LicensesWindow.xaml      ← Classic only
│   ├── OverlayWindow.xaml       ← Classic only
│   ├── PreviewWindow.xaml       ← Classic only
│   ├── WebCamWindow.xaml        ← Classic only
│   └── ... (~18 windows total)
└── Controls/
    └── WebcamControl.xaml       ← Classic only
```

---

## Summary Checklist

Before making ANY change:

- [ ] `git branch --show-current` - Verify which branch
- [ ] `ls src/Captura/Pages/ | grep Config` - Verify branch type
- [ ] If backend file → Probably safe to cherry-pick
- [ ] If UI file → CHECK both branches first
- [ ] After commit → Verify files match expected branch type
- [ ] After push → Check CI builds pass

---

## Emergency Commands

```bash
# See all differences between branches
git diff main classic-ui-modern-fixes --stat

# See which files exist only on classic
git diff main classic-ui-modern-fixes --name-status | grep "^A"

# See which files exist only on main  
git diff classic-ui-modern-fixes main --name-status | grep "^A"

# Reset branch to known good state
git reflog  # Find good commit
git reset --hard <commit>
git push origin <branch> --force
```

---

**Remember: When in doubt, DON'T cherry-pick. Apply changes manually.**
