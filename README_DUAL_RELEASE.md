# 🎉 Dual Release System Setup Complete!

## What We've Built

This repository now automatically builds **TWO versions** of Captura with a single release:

### 📦 Release Packages (4 Files Per Release)

1. **nCaptura-v{version}-Setup.exe** - Modern UI installer
2. **nCaptura-v{version}-Portable.zip** - Modern UI portable
3. **Captura-v{version}-Classic-Setup.exe** - Classic UI installer
4. **Captura-v{version}-Classic-Portable.zip** - Classic UI portable

## 🚀 How to Create a Release

### Quick Steps:

```bash
# 1. Update version on both branches (same version number)
git checkout main
# Edit src/Captura/Properties/AssemblyInfo.cs → change version to 10.1.0

git checkout classic-ui-modern-fixes  
# Edit src/Captura/Properties/AssemblyInfo.cs → change version to 10.1.0

# 2. Commit and push both
git checkout main
git add src/Captura/Properties/AssemblyInfo.cs
git commit -m "Bump version to 10.1.0"
git push

git checkout classic-ui-modern-fixes
git add src/Captura/Properties/AssemblyInfo.cs
git commit -m "Bump version to 10.1.0"
git push

# 3. Create tag from main and push
git checkout main
git tag v10.1.0
git push origin v10.1.0
```

**That's it!** The GitHub Actions workflow will:
- ✅ Build nCaptura (modern UI) from `main` branch
- ✅ Build Captura Classic from `classic-ui-modern-fixes` branch
- ✅ Create installers and portable versions for both
- ✅ Package all 4 files into one GitHub Release

## 📁 Files Created

### `.github/workflows/dual-release.yml`
The automated build workflow that handles everything.

### `DUAL_RELEASE_GUIDE.md`
Comprehensive guide for maintaining and using the dual-release system.

### `CLASSIC_UI_BUILD.md`
Documentation about the classic UI build on the `classic-ui-modern-fixes` branch.

## 🎯 Key Features

- ✅ **Single tag** triggers both builds
- ✅ **Identical functionality** - only UI differs
- ✅ **User choice** - modern or classic interface
- ✅ **Both formats** - installer and portable for each
- ✅ **Automated** - no manual steps needed
- ✅ **Professional installers** - using Inno Setup

## 🔧 Technical Details

| Aspect | nCaptura (Modern) | Captura Classic |
|--------|------------------|-----------------|
| **Branch** | main | classic-ui-modern-fixes |
| **UI Version** | Latest (10.0+) | 8.0.0 style |
| **About Page** | Modern layout | Classic layout |
| **Author Credits** | All contributors | All contributors |
| **Donation Link** | PayPal visible | PayPal visible |
| **GitHub Link** | grandixximo/nCaptura | grandixximo/nCaptura |
| **Functionality** | 100% identical | 100% identical |
| **Bug Fixes** | All included | All included |

## 📊 Example Release

When you push tag `v10.1.0`, users will see:

```
Release v10.1.0

🆕 nCaptura (Modern UI)
💾 nCaptura-v10.1.0-Setup.exe (15 MB)
📦 nCaptura-v10.1.0-Portable.zip (14 MB)

🎨 Captura Classic (8.0.0 UI)
💾 Captura-v10.1.0-Classic-Setup.exe (15 MB)
📦 Captura-v10.1.0-Classic-Portable.zip (14 MB)

Choose your preferred UI - both have identical features!
```

## 🛠️ Maintenance

See `DUAL_RELEASE_GUIDE.md` for:
- Syncing fixes between branches
- Testing builds before release
- Troubleshooting
- Version management

## ✅ Current Status

- ✅ Dual-release workflow created
- ✅ Both branches configured
- ✅ Documentation complete
- ✅ Ready for first release!

## 🎉 Benefits for Users

1. **Choice** - Pick the UI you prefer
2. **Latest fixes** - Both get all improvements
3. **Familiar experience** - Classic users keep their favorite UI
4. **Modern option** - New users get updated interface

---

**Next Step:** When ready, just tag a release and watch the magic happen! 🚀

For detailed instructions, see `DUAL_RELEASE_GUIDE.md`
