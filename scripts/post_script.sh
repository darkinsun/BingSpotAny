# BEGIN LICENSE
# Copyright (c) 2026 BingSpotAny Contributors
# *This program is free software: you can redistribute it and/or modify it
# under the terms of the GNU General Public License version 3, as published
# by the Free Software Foundation.
#
# This program is distributed in the hope that it will be useful, but
# WITHOUT ANY WARRANTY; without even the implied warranties of
# MERCHANTABILITY, SATISFACTORY QUALITY, or FITNESS FOR A PARTICULAR
# PURPOSE.  See the GNU General Public License for more details.
#
# You should have received a copy of the GNU General Public License along
# with this program.  If not, see <http://www.gnu.org/licenses/>.
# END LICENSE
# This script is an example for post script after a wallpaper change in Linux environment.

cp -f "$1" /usr/share/backgrounds/variety-current.jpg

chmod 644 /usr/share/backgrounds/variety-current.jpg
