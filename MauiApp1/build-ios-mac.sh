#!/bin/bash

echo "=== Building MauiApp1 for iOS on Mac ==="

# Clean previous builds
echo "1. Cleaning project..."
dotnet clean

# Restore packages
echo "2. Restoring packages..."
dotnet restore

# Build for iOS
echo "3. Building for iOS..."
dotnet build --framework net9.0-ios -c Debug

# If build fails with onnxruntime error, try this alternative
if [ $? -ne 0 ]; then
    echo ""
    echo "=== Build failed. Trying alternative approach ==="
    echo ""
    
    # Force linker to None
    echo "4. Building with forced linker settings..."
    dotnet build --framework net9.0-ios -c Debug -p:MtouchLink=None -p:PublishTrimmed=false
fi

echo ""
echo "=== Build complete ==="
echo ""
echo "If you still see onnxruntime errors, please ensure:"
echo "1. You have Xcode installed and updated"
echo "2. You have accepted Xcode license (run: sudo xcodebuild -license accept)"
echo "3. You have iOS SDK installed"
echo ""
echo "For deployment to device, use:"
echo "dotnet build --framework net9.0-ios -c Debug -p:RuntimeIdentifier=ios-arm64" 