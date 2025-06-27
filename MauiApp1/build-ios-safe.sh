#!/bin/bash

# MauiApp1 iOS Build Script for Mac
# This script handles disk space management and uses safe build parameters

echo "🍎 Starting iOS Build for MauiApp1..."

# Function to get available disk space in GB
get_disk_space() {
    df -BG . | awk 'NR==2 {print $4}' | sed 's/G//'
}

# Function to clean disk space if needed
cleanup_if_needed() {
    local available_space=$(get_disk_space)
    echo "💾 Available disk space: ${available_space}GB"
    
    if [ "$available_space" -lt 10 ]; then
        echo "⚠️  Low disk space detected (${available_space}GB). Cleaning up..."
        
        # Clean dotnet cache
        echo "🧹 Cleaning .NET cache..."
        dotnet nuget locals all --clear
        
        # Clean build artifacts
        echo "🧹 Cleaning build artifacts..."
        find . -name "bin" -type d -exec rm -rf {} + 2>/dev/null || true
        find . -name "obj" -type d -exec rm -rf {} + 2>/dev/null || true
        
        # Clean simulator data
        echo "🧹 Cleaning iOS Simulator data..."
        xcrun simctl delete unavailable 2>/dev/null || true
        xcrun simctl erase all 2>/dev/null || true
        
        # Clean Xcode cache
        echo "🧹 Cleaning Xcode cache..."
        rm -rf ~/Library/Developer/Xcode/DerivedData/* 2>/dev/null || true
        
        local new_space=$(get_disk_space)
        echo "✅ Cleanup complete. Available space: ${new_space}GB"
        
        if [ "$new_space" -lt 8 ]; then
            echo "❌ Still insufficient disk space (${new_space}GB). Please free up more space manually."
            exit 1
        fi
    fi
}

# Function to build for iOS
build_ios() {
    echo "🔨 Building for iOS..."
    echo "📱 Target: net9.0-ios"
    echo "⚙️  Configuration: Debug"
    echo "🔧 Using safe build parameters..."
    
    # Use project file settings (no conflicting command line parameters)
    dotnet build \
        --framework net9.0-ios \
        --configuration Debug \
        --verbosity minimal \
        | tee build.log
    
    local exit_code=${PIPESTATUS[0]}
    
    if [ $exit_code -eq 0 ]; then
        echo "✅ iOS build completed successfully!"
        echo "📱 Ready for iOS Simulator testing"
    else
        echo "❌ iOS build failed with exit code: $exit_code"
        echo "📋 Check build.log for details"
        echo ""
        echo "🔍 Common issues and solutions:"
        echo "   • Disk space: Run 'df -h' to check available space"
        echo "   • Clean build: Run 'dotnet clean' then try again"
        echo "   • Restart Mac: Sometimes helps with AOT compilation issues"
        echo "   • Check iOS Simulator: Make sure iOS Simulator is closed"
        return $exit_code
    fi
}

# Function to build for iOS Simulator only (faster)
build_ios_simulator() {
    echo "🔨 Building for iOS Simulator only (faster build)..."
    
    dotnet build \
        --framework net9.0-ios \
        --configuration Debug \
        --runtime iossimulator-x64 \
        --verbosity minimal \
        | tee build-simulator.log
    
    local exit_code=${PIPESTATUS[0]}
    
    if [ $exit_code -eq 0 ]; then
        echo "✅ iOS Simulator build completed successfully!"
        echo "📱 Ready for iOS Simulator testing"
    else
        echo "❌ iOS Simulator build failed with exit code: $exit_code"
        echo "📋 Check build-simulator.log for details"
        return $exit_code
    fi
}

# Main execution
main() {
    echo "🚀 MauiApp1 iOS Build Script"
    echo "=========================="
    
    # Check disk space and cleanup if needed
    cleanup_if_needed
    
    # Ask user for build type
    echo ""
    echo "Choose build type:"
    echo "1) iOS Simulator only (faster, recommended for development)"
    echo "2) Full iOS build (slower, includes device support)"
    echo ""
    read -p "Enter choice (1 or 2): " choice
    
    case $choice in
        1)
            build_ios_simulator
            ;;
        2)
            build_ios
            ;;
        *)
            echo "Invalid choice. Using iOS Simulator build..."
            build_ios_simulator
            ;;
    esac
    
    local build_result=$?
    
    if [ $build_result -eq 0 ]; then
        echo ""
        echo "🎉 Build completed successfully!"
        echo "💡 Next steps:"
        echo "   • Open iOS Simulator"
        echo "   • Run: dotnet run --framework net9.0-ios"
        echo "   • Or deploy from Visual Studio for Mac"
    else
        echo ""
        echo "💥 Build failed. Check the logs above for details."
        echo "🛠️  Troubleshooting tips:"
        echo "   • Make sure Xcode Command Line Tools are installed"
        echo "   • Restart Visual Studio for Mac/Xcode"
        echo "   • Try running: dotnet workload repair"
    fi
    
    return $build_result
}

# Make sure we're in the right directory
if [ ! -f "MauiApp1.csproj" ]; then
    echo "❌ Error: MauiApp1.csproj not found. Please run this script from the MauiApp1 directory."
    exit 1
fi

# Run main function
main "$@" 