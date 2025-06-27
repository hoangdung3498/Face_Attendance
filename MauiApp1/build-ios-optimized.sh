#!/bin/bash

echo "=== Optimized iOS Build Script ==="
echo "Estimated time: 5-10 minutes"
echo ""

# Check system resources
echo "🔍 Checking system resources..."
echo "Available RAM: $(sysctl hw.memsize | awk '{print $2/1024/1024/1024}') GB"

# Check disk space
DISK_SPACE=$(df . | tail -1 | awk '{print $4}')
DISK_SPACE_GB=$((DISK_SPACE / 1024 / 1024))
echo "Available disk space: $(df -h . | tail -1 | awk '{print $4}') ($DISK_SPACE_GB GB)"

# Stop if less than 10GB free
if [ $DISK_SPACE_GB -lt 10 ]; then
    echo ""
    echo "❌ ERROR: Not enough disk space!"
    echo "💾 Need at least 10GB free, you have $DISK_SPACE_GB GB"
    echo ""
    echo "🧹 To free up space:"
    echo "1. Run: ./cleanup-disk-space.sh"
    echo "2. Empty Trash"
    echo "3. Delete old files in Downloads"
    echo "4. Remove unused applications"
    echo ""
    exit 1
fi

echo ""

# Set environment variables for faster builds
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export NUGET_XMLDOC_MODE=skip

# Clean aggressively
echo "🧹 Cleaning previous builds..."
dotnet clean --verbosity quiet
rm -rf bin obj
dotnet nuget locals all --clear

echo "📦 Restoring packages (this may take 2-3 minutes)..."
dotnet restore --no-cache --force --verbosity quiet

echo "🔨 Building iOS (Simulator only for faster build)..."
dotnet build --framework net9.0-ios \
  --configuration Debug \
  --runtime iossimulator-x64 \
  --verbosity minimal \
  /p:PublishTrimmed=false \
  /p:MtouchLink=None \
  /p:RunAOTCompilation=false \
  /p:OptimizeNativeCode=false \
  /p:DebugSymbols=false \
  /p:GenerateDocumentationFile=false

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Build SUCCESS!"
    echo "📱 For device build, use:"
    echo "   dotnet build -f net9.0-ios -r ios-arm64"
else
    echo ""
    echo "❌ Build FAILED. Check errors above."
    echo ""
    echo "🔧 Troubleshooting steps:"
    echo "1. Ensure Xcode is installed and updated"
    echo "2. Run: sudo xcodebuild -license accept"
    echo "3. Check disk space (need 5GB+ free)"
    echo "4. Close other applications to free RAM"
fi

echo ""
echo "=== Build Complete ===" 