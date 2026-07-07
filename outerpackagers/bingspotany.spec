Name:           bingspotany-bin
Version:        1.1.1
Release:        2%{?dist}
Summary:        A modern, cross-platform daily wallpaper manager
License:        GPLv3
URL:            https://github.com/darkinsun/BingSpotAny
Source0:        https://github.com/darkinsun/BingSpotAny/releases/download/v%{version}/BingSpotAny-Linux-x64.tar.gz

Provides:       bingspotany
# Disable debug package generation since we are using a pre-compiled binary
%global debug_package %{nil}

%description
A modern, cross-platform daily wallpaper manager fetching from Bing and Spotlight. Memory optimized and built with Avalonia.

%prep
# Extract the tarball and enter the BingSpotAny-Linux-x64 directory
%setup -q -n BingSpotAny-Linux-x64

%build
# Skip build step
true

%install
# 1. Create standard Linux system directories
mkdir -p %{buildroot}/opt/BingSpotAny
mkdir -p %{buildroot}/usr/bin
mkdir -p %{buildroot}/usr/share/applications

# 2. Copy all files inside the folder using archive mode to preserve strict permissions
cp -a * %{buildroot}/opt/BingSpotAny/

# 3. Ensure the main binary is executable
chmod +x %{buildroot}/opt/BingSpotAny/BingSpotAny

# 4. Create a symlink so the user can just type 'bingspotany'
ln -s /opt/BingSpotAny/BingSpotAny %{buildroot}/usr/bin/bingspotany

# 5. Generate a native Linux Desktop Shortcut
cat <<EOF > %{buildroot}/usr/share/applications/bingspotany.desktop
[Desktop Entry]
Name=BingSpotAny
Comment=Daily wallpaper manager (Runs in system tray)
Exec=/usr/bin/bingspotany
Icon=/opt/BingSpotAny/Assets/icon.png
Terminal=false
Type=Application
Categories=Utility;DesktopSettings;
EOF

%files
# Specify the files and directories owned by this package
/opt/BingSpotAny/
/usr/bin/bingspotany
/usr/share/applications/bingspotany.desktop

%changelog
* Tue Jul 06 2026 darkinsun <42946064+darkinsun@users.noreply.github.com> - 1.1.1-2
- Fix file permissions and dynamic versioning for Fedora.
- Standardize 0644/0755 permissions for desktop and binary files.
- Enforce absolute symlink paths in the spec file.
- Ensure proper use of archive mode (cp -a) to preserve file metadata.