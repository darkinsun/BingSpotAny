@echo off
:: BEGIN LICENSE
:: ScriptVersion: 1.1.0
:: Copyright (c) 2026 BingSpotAny Contributors
:: *This program is free software: you can redistribute it and/or modify it
:: under the terms of the GNU General Public License version 3, as published
:: by the Free Software Foundation.
::
:: This program is distributed in the hope that it will be useful, but
:: WITHOUT ANY WARRANTY; without even the implied warranties of
:: MERCHANTABILITY, SATISFACTORY QUALITY, or FITNESS FOR A PARTICULAR
:: PURPOSE.  See the GNU General Public License for more details.
::
:: You should have received a copy of the GNU General Public License along
:: with this program.  If not, see <http://www.gnu.org/licenses/>.
:: END LICENSE
::
:: Windows Wallpaper Switcher for BingSpotAny
:: This script takes the image path passed from C# and triggers the Windows API.

set "IMAGE_PATH=%~1"

:: Exit if no argument is provided
if "%IMAGE_PATH%"=="" exit /b 1

:: Call user32.dll via PowerShell to update the wallpaper instantly
powershell -ExecutionPolicy Bypass -WindowStyle Hidden -Command "Add-Type -TypeDefinition 'using System; using System.Runtime.InteropServices; public class Wallpaper { [DllImport(\"user32.dll\", CharSet=CharSet.Auto)] public static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni); public static void Set(string path) { SystemParametersInfo(20, 0, path, 3); } }'; [Wallpaper]::Set('%IMAGE_PATH%')"
