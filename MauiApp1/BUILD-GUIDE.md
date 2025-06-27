# 🍎 MAUI iOS Build Guide for Mac

Complete guide for building MauiApp1 with ONNX Runtime on macOS, including solutions for common disk space and AOT compilation issues.

## 🚨 Known Issues & Solutions

### Issue 1: AOT Compilation Errors (Exit Code 139)
**Symptoms:**
- `Failed to AOT compile System.Private.Windows.Core.dll` with code 139
- `Failed to AOT compile aot-instances.dll` with code 139
- Build takes 20+ minutes and fails

**Solution:**
- ✅ **Fixed**: AOT compilation is now disabled in project settings
- ✅ **Fixed**: iOS linker set to `None` mode
- ✅ **Fixed**: Interpreter mode enabled for faster builds

### Issue 2: Disk Space Issues ("No space left on device")
**Symptoms:**
- Build fails with "No space left on device"
- Very slow builds (20+ minutes)
- ONNX Runtime extraction fails

**Solution:**
- ✅ **Automated**: Use `build-ios-safe.sh` script with auto-cleanup
- ✅ **Monitoring**: Use `check-disk-space.sh` to monitor space
- ✅ **Manual cleanup**: Use `cleanup-disk-space.sh` for deep cleaning

### Issue 3: Linker XML Wildcard Warnings
**Symptoms:**
- `IL2108: XML contains unsupported wildcard for assembly 'fullname' attribute`
- Multiple MT7091 warnings about framework paths

**Solution:**
- ✅ **Fixed**: Updated linker.xml files to use specific assembly/type names
- ✅ **Fixed**: Removed unsupported wildcard patterns

## 🛠️ Prerequisites

1. **Xcode** (latest version)
2. **Xcode Command Line Tools**:
   ```bash
   xcode-select --install
   ```
3. **.NET 9 SDK** with MAUI workload:
   ```bash
   dotnet workload install maui
   ```
4. **Minimum 15GB free disk space** (recommended 25GB+)

## 🚀 Quick Start (Automated)

### Option 1: Use Safe Build Script (Recommended)
```bash
cd MauiApp1
chmod +x build-ios-safe.sh
./build-ios-safe.sh
```

This script will:
- ✅ Check and manage disk space automatically
- ✅ Use safe build parameters 
- ✅ Offer simulator-only builds for faster development
- ✅ Provide detailed error messages and solutions

### Option 2: Manual Build (Advanced)
```bash
dotnet build --framework net9.0-ios --configuration Debug
```

## 📱 Build Options

### 1. iOS Simulator Build (Fastest)
```bash
dotnet build \
    --framework net9.0-ios \
    --configuration Debug \
    --runtime iossimulator-x64
```
- ⚡ **Speed**: ~5-10 minutes
- 💾 **Space**: Uses less disk space
- 🎯 **Use**: Development and testing

### 2. Full iOS Build (Device Support)
```bash
dotnet build \
    --framework net9.0-ios \
    --configuration Debug
```
- ⚡ **Speed**: ~10-15 minutes
- 💾 **Space**: Requires more disk space
- 🎯 **Use**: Device deployment

## 🧹 Disk Space Management

### Check Current Space
```bash
chmod +x check-disk-space.sh
./check-disk-space.sh
```

### Auto Cleanup (if space is low)
```bash
chmod +x cleanup-disk-space.sh
./cleanup-disk-space.sh
```

### Manual Cleanup Commands
```bash
# Clean project artifacts
dotnet clean
rm -rf bin obj

# Clear .NET caches
dotnet nuget locals all --clear

# Clear Xcode cache
rm -rf ~/Library/Developer/Xcode/DerivedData/*

# Reset iOS Simulators
xcrun simctl delete unavailable
xcrun simctl erase all
```

## 📊 Disk Space Requirements

| Component | Space Required | Notes |
|-----------|----------------|-------|
| ONNX Runtime Download | ~500MB | Initial download |
| ONNX Runtime Extracted | ~5GB | During build process |
| Build Artifacts | 2-3GB | Temporary build files |
| Final App | ~50-100MB | Compiled application |
| **Recommended Total** | **15GB+** | For smooth builds |

## ⚙️ Project Configuration

The project is now optimized with these settings:

### iOS Configuration (MauiApp1.csproj)
```xml
<!-- iOS Linker Configuration -->
<MtouchLink Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios'">None</MtouchLink>
<PublishTrimmed Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios'">false</PublishTrimmed>
<RunAOTCompilation Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios'">false</RunAOTCompilation>
<MtouchInterpreter Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios'">-all</MtouchInterpreter>
```

### Linker Protection (Platforms/iOS/linker.xml)
```xml
<!-- Preserves ONNX Runtime assemblies without wildcards -->
<assembly fullname="Microsoft.ML.OnnxRuntime" preserve="all" />
<assembly fullname="InferencingSampleCore" preserve="all" />
<!-- ... specific type preservation ... -->
```

## 🔧 Troubleshooting

### Build Still Fails?

1. **Check disk space**: `./check-disk-space.sh`
2. **Clean everything**: `./cleanup-disk-space.sh`
3. **Restart services**:
   ```bash
   # Restart build services
   killall dotnet
   killall msbuild
   killall Xcode
   ```
4. **Verify workloads**:
   ```bash
   dotnet workload list
   dotnet workload repair
   ```

### Performance Issues?

1. **Use simulator builds** for development
2. **Close unnecessary apps** during build
3. **Monitor Activity Monitor** for high CPU/memory usage
4. **Consider external storage** for large files

### Still Getting Linker Errors?

1. Check `build.log` or `build-simulator.log` for details
2. Verify all ONNX model files are present:
   ```bash
   ls -la *.onnx
   ```
3. Check InferencingSampleCore reference version (should be 1.0.2+)

## 📋 Build Scripts Reference

| Script | Purpose | Usage |
|--------|---------|-------|
| `build-ios-safe.sh` | Safe automated build with disk management | `./build-ios-safe.sh` |
| `check-disk-space.sh` | Monitor disk space and get recommendations | `./check-disk-space.sh` |
| `cleanup-disk-space.sh` | Comprehensive cleanup for development environment | `./cleanup-disk-space.sh` |

## 🎯 Best Practices

1. **Always check disk space** before building
2. **Use simulator builds** for development iteration
3. **Clean regularly** to prevent space issues
4. **Monitor build logs** for early warning signs
5. **Keep 25GB+ free** for optimal performance

## 🆘 Emergency Recovery

If builds completely fail and you need to reset:

```bash
# Nuclear option - reset everything
./cleanup-disk-space.sh
dotnet workload uninstall maui
dotnet workload install maui
dotnet clean
rm -rf bin obj
dotnet restore
./build-ios-safe.sh
```

## ✅ Success Indicators

You know it's working when:
- ✅ Build completes in 5-10 minutes (simulator) or 10-15 minutes (full)
- ✅ No disk space warnings
- ✅ No AOT compilation errors
- ✅ No linker wildcard warnings
- ✅ App deploys successfully to iOS Simulator

## 📞 Support

If you continue having issues:
1. Run `./check-disk-space.sh` and share output
2. Check `build.log` for specific error messages
3. Verify you have the latest Xcode and .NET 9 SDK
4. Consider upgrading to a Mac with more storage if persistently low on space 