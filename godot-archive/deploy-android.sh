#!/bin/bash
# Quick Android deployment script for Starquill
# Builds and installs APK to connected Android device

set -e

echo "=== Starquill Android Deployment ==="

# Check if device is connected
if ! adb devices | grep -q "device$"; then
    echo "❌ No Android device connected!"
    echo "Please connect your device and enable USB debugging."
    exit 1
fi

DEVICE=$(adb devices | grep "device$" | awk '{print $1}')
echo "✓ Device connected: $DEVICE"

# Build directory
BUILD_DIR="build"
mkdir -p "$BUILD_DIR"

# Export the APK using Godot
echo "Building Android APK..."
godot --headless --export-debug "Android" "$BUILD_DIR/Starquill-debug.apk"

if [ ! -f "$BUILD_DIR/Starquill-debug.apk" ]; then
    echo "❌ Build failed - APK not found"
    exit 1
fi

echo "✓ APK built successfully"

# Install to device
echo "Installing to device..."
adb install -r "$BUILD_DIR/Starquill-debug.apk"

echo "✓ Installation complete!"
echo ""
echo "Launch the app from your device or run:"
echo "  adb shell am start -n com.starquill.game/com.godot.game.GodotApp"
