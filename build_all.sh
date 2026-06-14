#!/bin/bash
set -e # Exit immediately if a command exits with a non-zero status

VERSION="1.0.0"
DIST_DIR="./publish"

echo "🧹 Cleaning previous builds..."
rm -rf "$DIST_DIR"
mkdir -p "$DIST_DIR"

echo "📦 1. BUILDING WINDOWS PACKAGE..."
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:PublishReadyToRun=true -o "$DIST_DIR/windows_tmp"
# Create the Windows archive
cd "$DIST_DIR/windows_tmp"
zip -r "../BingSpotAny-Windows-x64.zip" *
cd ../..
rm -rf "$DIST_DIR/windows_tmp"

echo "📦 2. BUILDING LINUX PORTABLE PACKAGE..."
dotnet publish -c Release -r linux-x64 --self-contained true /p:PublishSingleFile=true -o "$DIST_DIR/BingSpotAny-Linux-x64"
# Create the Linux archive
cd "$DIST_DIR"
tar -czvf "BingSpotAny-Linux-x64.tar.gz" "BingSpotAny-Linux-x64"
cd ..

echo "📦 3. BUILDING MACOS APP BUNDLE..."
dotnet publish -c Release -r osx-x64 --self-contained true /p:PublishSingleFile=true -o "$DIST_DIR/osx_tmp"

# Construct macOS .app bundle structure natively on Linux
APP_DIR="$DIST_DIR/BingSpotAny.app"
mkdir -p "$APP_DIR/Contents/MacOS"
mkdir -p "$APP_DIR/Contents/Resources"

# Copy binaries and icon
cp -r "$DIST_DIR/osx_tmp"/* "$APP_DIR/Contents/MacOS/"
cp "Assets/icon.icns" "$APP_DIR/Contents/Resources/" 2>/dev/null || true

# Generate Info.plist dynamically
cat <<EOF > "$APP_DIR/Contents/Info.plist"
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleExecutable</key>
    <string>BingSpotAny</string>
    <key>CFBundleIconFile</key>
    <string>icon.icns</string>
    <key>CFBundleIdentifier</key>
    <string>io.github.darkinsun.bingspotany</string>
    <key>CFBundleName</key>
    <string>BingSpotAny</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleShortVersionString</key>
    <string>$VERSION</string>
</dict>
</plist>
EOF

# Compress the macOS app bundle
cd "$DIST_DIR"
zip -r "BingSpotAny-MacOs-x64.zip" "BingSpotAny.app"
rm -rf "osx_tmp" "BingSpotAny.app"
cd ..

echo "🚀 ALL BUILDS COMPLETED SUCCESSFULLY!"
echo "Outputs are located in the '$DIST_DIR' directory."