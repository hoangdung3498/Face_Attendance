#!/bin/bash

# Disk Space Cleanup Script for Mac Development
# Specifically designed for MAUI/iOS development cleanup

echo "🧹 Mac Development Disk Cleanup"
echo "==============================="
echo "This script will clean up development-related files to free disk space."
echo ""

# Function to get disk space in GB
get_disk_space() {
    df -BG . | awk 'NR==2 {print $4}' | sed 's/G//'
}

# Function to calculate size and show progress
show_space_freed() {
    local before=$1
    local after=$(get_disk_space)
    local freed=$((after - before))
    echo "   💾 Freed: ${freed}GB (was ${before}GB, now ${after}GB)"
}

# Get initial disk space
INITIAL_SPACE=$(get_disk_space)
echo "🔍 Initial disk space: ${INITIAL_SPACE}GB"
echo ""

# Confirm before proceeding
read -p "🤔 Proceed with cleanup? (y/N): " confirm
if [[ ! $confirm =~ ^[Yy]$ ]]; then
    echo "❌ Cleanup cancelled"
    exit 0
fi

echo ""
echo "🚀 Starting cleanup process..."

# 1. Clean .NET related caches
echo ""
echo "🧹 Cleaning .NET caches and temp files..."
SPACE_BEFORE=$(get_disk_space)

# Clean NuGet caches
dotnet nuget locals all --clear 2>/dev/null || true

# Clean .NET temp files
rm -rf ~/.dotnet/TelemetryStorageService 2>/dev/null || true
rm -rf ~/.dotnet/.toolstorecache 2>/dev/null || true
rm -rf ~/.nuget/packages/.tools 2>/dev/null || true

show_space_freed $SPACE_BEFORE

# 2. Clean project build artifacts
echo ""
echo "🧹 Cleaning build artifacts in current directory..."
SPACE_BEFORE=$(get_disk_space)

# Find and remove bin/obj directories
find . -name "bin" -type d -exec rm -rf {} + 2>/dev/null || true
find . -name "obj" -type d -exec rm -rf {} + 2>/dev/null || true

# Remove specific build artifacts
rm -rf */bin */obj **/bin **/obj 2>/dev/null || true

show_space_freed $SPACE_BEFORE

# 3. Clean Xcode cache
echo ""
echo "🧹 Cleaning Xcode DerivedData..."
SPACE_BEFORE=$(get_disk_space)

rm -rf ~/Library/Developer/Xcode/DerivedData/* 2>/dev/null || true
rm -rf ~/Library/Developer/Xcode/iOS\ DeviceSupport/*/Symbols 2>/dev/null || true
rm -rf ~/Library/Developer/Xcode/Archives 2>/dev/null || true

show_space_freed $SPACE_BEFORE

# 4. Clean iOS Simulator data
echo ""
echo "🧹 Cleaning iOS Simulator data..."
SPACE_BEFORE=$(get_disk_space)

# Delete unavailable simulators
xcrun simctl delete unavailable 2>/dev/null || true

# Reset all simulators (removes apps and data)
read -p "🤔 Reset all iOS Simulators? This will remove all apps and data (y/N): " reset_sim
if [[ $reset_sim =~ ^[Yy]$ ]]; then
    xcrun simctl erase all 2>/dev/null || true
    echo "   📱 All simulators reset"
fi

show_space_freed $SPACE_BEFORE

# 5. Clean Visual Studio for Mac cache
echo ""
echo "🧹 Cleaning Visual Studio for Mac cache..."
SPACE_BEFORE=$(get_disk_space)

rm -rf ~/Library/Caches/VisualStudio 2>/dev/null || true
rm -rf ~/Library/Caches/com.microsoft.visual-studio 2>/dev/null || true
rm -rf ~/Library/VisualStudio/*/Logs 2>/dev/null || true

show_space_freed $SPACE_BEFORE

# 6. Clean system temp files
echo ""
echo "🧹 Cleaning system temporary files..."
SPACE_BEFORE=$(get_disk_space)

# Clean user temp
rm -rf ~/Library/Caches/Cleanup\ At\ Startup 2>/dev/null || true
rm -rf ~/Library/Logs/* 2>/dev/null || true

# Clean downloads if user confirms
read -p "🤔 Clean ~/Downloads folder? (y/N): " clean_downloads
if [[ $clean_downloads =~ ^[Yy]$ ]]; then
    find ~/Downloads -type f -mtime +30 -delete 2>/dev/null || true
    echo "   📁 Deleted files older than 30 days from Downloads"
fi

show_space_freed $SPACE_BEFORE

# 7. Clean package managers cache
echo ""
echo "🧹 Cleaning package manager caches..."
SPACE_BEFORE=$(get_disk_space)

# Clean npm cache if exists
which npm >/dev/null && npm cache clean --force 2>/dev/null || true

# Clean yarn cache if exists  
which yarn >/dev/null && yarn cache clean 2>/dev/null || true

# Clean CocoaPods cache
which pod >/dev/null && pod cache clean --all 2>/dev/null || true

show_space_freed $SPACE_BEFORE

# 8. Empty Trash
echo ""
echo "🧹 Emptying Trash..."
SPACE_BEFORE=$(get_disk_space)

read -p "🤔 Empty Trash? (y/N): " empty_trash
if [[ $empty_trash =~ ^[Yy]$ ]]; then
    rm -rf ~/.Trash/* 2>/dev/null || true
    echo "   🗑️  Trash emptied"
    show_space_freed $SPACE_BEFORE
else
    echo "   ⏭️  Skipping Trash cleanup"
fi

# 9. Final summary
echo ""
echo "✅ Cleanup Complete!"
echo "==================="

FINAL_SPACE=$(get_disk_space)
TOTAL_FREED=$((FINAL_SPACE - INITIAL_SPACE))

echo "📊 Disk Space Summary:"
echo "   • Before: ${INITIAL_SPACE}GB"
echo "   • After:  ${FINAL_SPACE}GB"
echo "   • Freed:  ${TOTAL_FREED}GB"

echo ""
if [ $FINAL_SPACE -lt 8 ]; then
    echo "⚠️  Still low on disk space (${FINAL_SPACE}GB)"
    echo "💡 Additional cleanup suggestions:"
    echo "   • Review large files: sudo du -sh ~/Downloads ~/Documents ~/Desktop"
    echo "   • Move files to external storage"
    echo "   • Delete unused applications"
    echo "   • Consider upgrading storage"
elif [ $FINAL_SPACE -lt 15 ]; then
    echo "⚠️  Moderate disk space (${FINAL_SPACE}GB)"
    echo "💡 Should be enough for development, but monitor usage"
else
    echo "✅ Good disk space available (${FINAL_SPACE}GB)"
    echo "🚀 Ready for iOS development!"
fi

echo ""
echo "🎯 For MAUI iOS builds, recommended minimum: 15GB free"
echo "📱 ONNX Runtime requires ~5GB when extracted during build" 