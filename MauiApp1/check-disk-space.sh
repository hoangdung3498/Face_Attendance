#!/bin/bash

# Disk Space Check Script for Mac Development
# Monitor disk space and provide recommendations

echo "💾 Disk Space Monitor for MAUI Development"
echo "========================================="

# Function to get disk space in GB
get_disk_space() {
    df -BG . | awk 'NR==2 {print $4}' | sed 's/G//'
}

# Function to get total disk space
get_total_space() {
    df -BG . | awk 'NR==2 {print $2}' | sed 's/G//'
}

# Function to get used space
get_used_space() {
    df -BG . | awk 'NR==2 {print $3}' | sed 's/G//'
}

# Get disk information
AVAILABLE=$(get_disk_space)
TOTAL=$(get_total_space)
USED=$(get_used_space)
PERCENT_USED=$((100 * USED / TOTAL))

echo "📊 Current Disk Usage:"
echo "   • Total:     ${TOTAL}GB"
echo "   • Used:      ${USED}GB (${PERCENT_USED}%)"
echo "   • Available: ${AVAILABLE}GB"
echo ""

# Analyze space and provide recommendations
if [ $AVAILABLE -lt 5 ]; then
    echo "🚨 CRITICAL: Very low disk space (${AVAILABLE}GB)"
    echo ""
    echo "⚠️  Immediate actions needed:"
    echo "   1. Run cleanup script: ./cleanup-disk-space.sh"
    echo "   2. Empty Trash completely"
    echo "   3. Delete large files from Downloads"
    echo "   4. Uninstall unused applications"
    echo ""
    echo "❌ iOS builds will likely fail with this space"
    
elif [ $AVAILABLE -lt 10 ]; then
    echo "⚠️  WARNING: Low disk space (${AVAILABLE}GB)"
    echo ""
    echo "💡 Recommended actions:"
    echo "   1. Run cleanup script: ./cleanup-disk-space.sh"
    echo "   2. Review and delete old files"
    echo "   3. Move large files to external storage"
    echo ""
    echo "⚠️  iOS builds may fail or be very slow"
    
elif [ $AVAILABLE -lt 15 ]; then
    echo "⚠️  MODERATE: Moderate disk space (${AVAILABLE}GB)"
    echo ""
    echo "💡 Suggestions:"
    echo "   • Should be enough for basic iOS builds"
    echo "   • Monitor space during builds"
    echo "   • Consider cleanup if builds fail"
    echo ""
    echo "✅ iOS builds should work but may be slow"
    
else
    echo "✅ GOOD: Sufficient disk space (${AVAILABLE}GB)"
    echo ""
    echo "🚀 Excellent for iOS development!"
    echo "   • Fast builds expected"
    echo "   • No space limitations"
fi

echo ""
echo "📱 MAUI iOS Build Requirements:"
echo "   • Minimum:     8GB free (basic builds)"
echo "   • Recommended: 15GB free (optimal performance)"
echo "   • Ideal:       25GB+ free (multiple projects)"

echo ""
echo "🔍 Space Usage by ONNX Runtime:"
echo "   • Download:    ~500MB"
echo "   • Extracted:   ~5GB during build"
echo "   • Final app:   ~50-100MB"

echo ""
echo "🧹 Quick Cleanup Commands:"
echo "   • Full cleanup:     ./cleanup-disk-space.sh"
echo "   • Project only:     dotnet clean && rm -rf bin obj"
echo "   • NuGet cache:      dotnet nuget locals all --clear"
echo "   • Xcode cache:      rm -rf ~/Library/Developer/Xcode/DerivedData/*"

# Show top space consumers if available space is low
if [ $AVAILABLE -lt 15 ]; then
    echo ""
    echo "🔍 Finding large directories..."
    echo "   (This may take a moment...)"
    
    # Find large directories in common locations
    echo ""
    echo "📁 Largest directories in home folder:"
    du -sh ~/Downloads ~/Documents ~/Desktop ~/Library/Caches 2>/dev/null | sort -hr | head -5
    
    echo ""
    echo "📁 Largest directories in current project:"
    find . -type d -name "bin" -o -name "obj" -o -name "packages" -o -name "node_modules" 2>/dev/null | while read dir; do
        if [ -d "$dir" ]; then
            du -sh "$dir" 2>/dev/null
        fi
    done | sort -hr | head -5
fi

echo ""
echo "📊 For detailed analysis, run:"
echo "   du -sh ~/* | sort -hr | head -10"
echo "   df -h" 