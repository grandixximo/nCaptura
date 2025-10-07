# Dual Release Guide

This repository is configured to build **both UI versions** in a single release!

## 🎯 Release Strategy

When you create a version tag (e.g., `v10.1.0`), the build system automatically creates:

### nCaptura (Modern UI)
- ✅ Modern interface improvements
- ✅ Updated about page with multiple contributors
- ✅ Built from `main` branch
- **Files:**
  - `nCaptura-v10.1.0-Setup.exe` (Installer)
  - `nCaptura-v10.1.0-Portable.zip` (Portable)

### Captura Classic (8.0.0 UI)
- ✅ Original 8.0.0 interface design
- ✅ MathewSachin original attribution
- ✅ PayPal donation link
- ✅ Built from `classic-ui-modern-fixes` branch
- **Files:**
  - `Captura-v10.1.0-Classic-Setup.exe` (Installer)
  - `Captura-v10.1.0-Classic-Portable.zip` (Portable)

## 📦 What's Included in Both

Both versions include **ALL the same features and fixes**:
- FFmpeg downloader with reliable mirrors
- .NET Framework compatibility fixes
- Windows 11 support
- All bug fixes and improvements

**The ONLY difference is the UI design!**

## 🚀 How to Create a Release

### 1. Update Version Numbers

**On `main` branch:**
```bash
git checkout main
# Edit src/Captura/Properties/AssemblyInfo.cs
# Change [assembly: AssemblyVersion("10.0.5")] to new version
git add src/Captura/Properties/AssemblyInfo.cs
git commit -m "Bump version to 10.1.0"
git push origin main
```

**On `classic-ui-modern-fixes` branch:**
```bash
git checkout classic-ui-modern-fixes
# Edit src/Captura/Properties/AssemblyInfo.cs
# Change [assembly: AssemblyVersion("10.0.5")] to same new version
git add src/Captura/Properties/AssemblyInfo.cs
git commit -m "Bump version to 10.1.0"
git push origin classic-ui-modern-fixes
```

### 2. Create and Push Tag

```bash
# Create tag on main branch
git checkout main
git tag v10.1.0
git push origin v10.1.0
```

### 3. Watch the Build

The `dual-release.yml` workflow will automatically:
1. Build nCaptura from `main` branch
2. Build Captura Classic from `classic-ui-modern-fixes` branch
3. Create installer and portable for each
4. Package all 4 files into one GitHub Release

### 4. Release Created!

GitHub will create a release with all 4 downloads:
- nCaptura-v10.1.0-Setup.exe
- nCaptura-v10.1.0-Portable.zip
- Captura-v10.1.0-Classic-Setup.exe
- Captura-v10.1.0-Classic-Portable.zip

## 📋 Workflow File

The magic happens in `.github/workflows/dual-release.yml`:

```yaml
jobs:
  build-modern-ui:
    # Builds from main branch → nCaptura
    
  build-classic-ui:
    # Builds from classic-ui-modern-fixes → Captura Classic
    
  create-release:
    # Combines both builds into one release
```

## 🔧 File Naming Convention

| UI Version | Branch | Executable | Installer | Portable |
|------------|--------|------------|-----------|----------|
| Modern | `main` | captura.exe | nCaptura-{version}-Setup.exe | nCaptura-{version}-Portable.zip |
| Classic | `classic-ui-modern-fixes` | captura.exe | Captura-{version}-Classic-Setup.exe | Captura-{version}-Classic-Portable.zip |

## 🎨 UI Differences

### Modern UI (nCaptura)
```
About Page:
- Modern button layout (WrapPanel)
- All contributor credits (Mathew Sachin, Mr. Chip, grandixximo)
- PayPal donation button
- Links to grandixximo/nCaptura
```

### Classic UI (Captura)
```
About Page:
- Original vertical button layout
- All contributor credits (Mathew Sachin, Mr. Chip, grandixximo)
- PayPal donation button
- Links to grandixximo/nCaptura
- Classic window styling
- Includes Crop tool button
```

## 🛠️ Maintenance

### Syncing Fixes Between Branches

When you fix a bug on `main`, apply it to `classic-ui-modern-fixes`:

```bash
# Fix on main
git checkout main
# Make your fix
git commit -m "Fix XYZ bug"
git push origin main

# Cherry-pick to classic branch
git checkout classic-ui-modern-fixes
git cherry-pick <commit-hash>
git push origin classic-ui-modern-fixes
```

### Updating Classic UI Branch

To pull all fixes from main to classic branch (excluding UI changes):

```bash
git checkout classic-ui-modern-fixes
git rebase main
# Resolve any conflicts (usually just AboutPage.xaml)
# Keep the classic UI version
git push origin classic-ui-modern-fixes --force-with-lease
```

## 📊 Release Statistics

Each release will show:
- ✅ 4 downloadable files
- ✅ User choice between UI versions
- ✅ Identical functionality
- ✅ Professional installers for both

## 🎯 User Experience

Users see a release page like:

```
Release v10.1.0

🆕 nCaptura (Modern UI)
- nCaptura-v10.1.0-Setup.exe
- nCaptura-v10.1.0-Portable.zip

🎨 Captura Classic (8.0.0 UI)
- Captura-v10.1.0-Classic-Setup.exe
- Captura-v10.1.0-Classic-Portable.zip

Choose based on your UI preference!
```

## ⚠️ Important Notes

1. **Always update version numbers on BOTH branches**
2. **Create tag from main branch** (workflow checks out both branches)
3. **Test locally before tagging** if making significant changes
4. **Don't delete the `classic-ui-modern-fixes` branch** - it's needed for builds

## 🧪 Testing Before Release

Test the workflow without creating a release:

```bash
# Go to GitHub Actions tab
# Click "Dual Release - Modern & Classic UI"
# Click "Run workflow"
# Select branch: main
# Click "Run workflow"
```

This will build both versions without creating a release (workflow_dispatch trigger).

## 📝 Version History

- **10.0.5** - First dual-release version
  - nCaptura: Modern UI with all fixes
  - Captura Classic: 8.0.0 UI with all fixes

---

## Quick Reference

### Create a New Release

```bash
# 1. Update versions on both branches
# 2. Tag and push
git checkout main
git tag v10.1.0
git push origin v10.1.0
# 3. Watch GitHub Actions build both versions
# 4. Release automatically created with 4 files
```

That's it! The automated workflow handles everything else. 🎉
