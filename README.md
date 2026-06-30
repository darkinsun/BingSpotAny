# BingSpotAny

A modern, cross-platform daily wallpaper manager built with **.NET 10.0** and **Avalonia UI**. 

BingSpotAny seamlessly fetches and applies beautiful daily wallpapers from providers like Bing and Windows Spotlight, bringing fresh backgrounds to your desktop environment every day. 

![BingSpotAny Screenshot](Assets/screenshot_placeholder.png)

---

## ✨ Features

* **Truly Cross-Platform:** Runs natively on Windows, Linux, and macOS.
* **Multiple Providers:** Choose between Bing's daily images or Windows Spotlight curations.
* **Auto-Start Integration:** Silently boots with your OS (utilizes Registry on Windows, `.desktop` on Linux, and `launchd` on macOS).
* **Single-Instance Lock:** Prevents multiple background processes from draining your system resources.
* **Favorites & Archive:** Easily save your favorite daily wallpapers to a dedicated folder.
* **Modern UI:** Clean, responsive, and resource-friendly interface powered by Avalonia UI.

## 💻 Tested Environments

BingSpotAny is actively developed and tested to ensure stability across various systems, including:
* **Windows:** Windows 10 (Build 10.0.19041) and later.
* **Linux:** CachyOS, actively tested on XFCE 4.20 desktop environment.
* **macOS:** Universal compatibility.

### 🐧 Universal Linux Support (Desktop Environment Agnostic)
No more fragmentation! BingSpotAny is built to work seamlessly across the diverse Linux ecosystem, regardless of your display server (X11 or Wayland) or desktop environment.

* **True Independence:** Works flawlessly on GNOME, KDE Plasma, XFCE, Cinnamon, and even the next-gen COSMIC desktop.
* **Native Autostart Integration:** Utilizes the Freedesktop.org (XDG Autostart) standard to quietly launch with your system at boot—no manual scripts, symlinks, or `.config` tweaking required.

---

## 🚀 Installation

BingSpotAny is distributed as a **self-contained** application. You do not need to install .NET or any other frameworks to run it!

### Arch Linux & Derivatives (Manjaro, CachyOS, EndeavourOS)
Add the custom Pacman repository to your system to install the package and receive automatic updates. Run the following universal command in your terminal:
```bash
printf "\n[bingspotany]\nSigLevel = Optional TrustAll\nServer = https://darkinsun.github.io/BingSpotAny/arch\n" | sudo tee -a /etc/pacman.conf
```
```bash
sudo pacman -Sy bingspotany
```

Note: Due to the ongoing AUR security crisis and the temporary lockdown on new repository registrations, BingSpotAny packges haven't been uploaded to Aur just yet.

### Ubuntu, Linux Mint & Debian
Add the custom APT repository to install the application and keep it updated seamlessly:

```bash
echo "deb [trusted=yes] https://darkinsun.github.io/BingSpotAny/debian /" | sudo tee /etc/apt/sources.list.d/bingspotany.list
```
```bash
sudo apt update && sudo apt install bingspotany
```

### Fedora & openSUSE

* **Option-1:** Enable the official Copr repository and install BingSpotAny via DNF:

```bash
sudo dnf copr enable darkinsun/bingspotany
```
```bash
sudo dnf install bingspotany-bin
```

* **Option-2:** Add the custom DNF repository configuration to your system and install the package:
```bash
printf "[bingspotany]\nname=BingSpotAny Official Repository\nbaseurl=https://darkinsun.github.io/BingSpotAny/fedora/\nenabled=1\ngpgcheck=0\n" | sudo tee /etc/yum.repos.d/bingspotany.repo
```
```bash
sudo dnf install bingspotany
```

### Linux (Portable)
1. Download the latest `BingSpotAny-Linux-x64.tar.gz` from the [Releases](https://github.com/darkinsun/BingSpotAny/releases) page.
2. Extract the archive to a folder in your home directory (e.g., `~/BingSpotAny`).
3. Open your terminal in the extracted folder and execute the application:
```bash
./BingSpotAny
```

### Windows
1. Download the latest `BingSpotAny-Windows-x64.zip` from the [Releases](https://github.com/darkinsun/BingSpotAny/releases) page.
2. Extract the folder to your preferred location.
3. Double-click `BingSpotAny.exe` to run. 
*(Note: If Windows SmartScreen prompts you, click "More info" and "Run anyway".)*

### macOS
1. Download the latest `BingSpotAny-macOS-x64.zip` from the [Releases](https://github.com/darkinsun/BingSpotAny/releases) page.
2. Extract the archive.
3. Right-click the extracted application and select **Open** (this is required on the first launch to bypass Gatekeeper).

> **⚠️ Important Note for macOS Users**
> 
> Since this application is open-source and not signed with a paid Apple Developer certificate, macOS Gatekeeper may show a warning stating the app *"cannot be opened because the developer cannot be verified"* when you first try to launch it.
> 
> To permanently remove this quarantine restriction and allow the app to run smoothly, open your **Terminal** and run the following command (assuming you extracted the app to your Downloads or Applications folder):
> 
> ```bash
> sudo xattr -cr /path/to/BingSpotAny.app
> ```
> *(Tip: You can simply type `sudo xattr -cr ` with a trailing space, and then drag & drop the `BingSpotAny.app` file into the terminal window to auto-fill the path, then hit Enter).*


### Build From Source (All Platforms)
If you prefer to compile the application yourself:
```bash
# Clone the repository
git clone https://github.com/darkinsun/BingSpotAny.git
cd BingSpotAny

# Build and run the project
dotnet run
```

---

## 🐛 Bug Reports & Support

We welcome community involvement! Here is how you can contribute or get help:

* **Bug Reports:** If you discover a bug or have a concrete feature request, please open an issue in the **[Issues](https://github.com/darkinsun/BingSpotAny/issues)** tab. Include your operating system details and steps to reproduce the problem.
* **Support & Questions:** Need help with installation, have a general question, or want to share an idea? Please join our community in the **[Discussions](https://github.com/darkinsun/BingSpotAny/discussions)** tab.

---

## 📜 Acknowledgements & License

This project is licensed under the **GNU General Public License v3.0 (GPLv3)**. See the [LICENSE](LICENSE) file for more details.

**Special Thanks:** This application utilizes a modified wallpaper-changing script originally adapted from the [Variety](https://github.com/varietywalls/variety) project, which is also licensed under GPLv3. We extend our gratitude to the Variety developers for their excellent work in the open-source Linux community.

---

## ☕ Support the Project

BingSpotAny is an open-source project distributed for free. If you find it useful and want to support its continued development, you can buy me a coffee!

Please visit the **[DONATE](DONATE.md)** page for details on how to support the project via Patreon or direct Cryptocurrency transfers.
