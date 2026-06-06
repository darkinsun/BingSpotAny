@echo off
:: Windows Wallpaper Switcher for BingSpotAny
:: This script takes the image path passed from C# and triggers the Windows API.

set "IMAGE_PATH=%~1"

:: Exit if no argument is provided
if "%IMAGE_PATH%"=="" exit /b 1

:: Call user32.dll via PowerShell to update the wallpaper instantly
powershell -ExecutionPolicy Bypass -WindowStyle Hidden -Command "Add-Type -TypeDefinition 'using System; using System.Runtime.InteropServices; public class Wallpaper { [DllImport(\"user32.dll\", CharSet=CharSet.Auto)] public static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni); public static void Set(string path) { SystemParametersInfo(20, 0, path, 3); } }'; [Wallpaper]::Set('%IMAGE_PATH%')"
