/*
 * BEGIN LICENSE
 * Copyright (c) 2026 BingSpotAny Contributors
 * * This program is free software: you can redistribute it and/or modify it
 * under the terms of the GNU General Public License version 3, as published
 * by the Free Software Foundation.
 *
 * This program is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranties of
 * MERCHANTABILITY, SATISFACTORY QUALITY, or FITNESS FOR A PARTICULAR
 * PURPOSE.  See the GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with this program.  If not, see <http://www.gnu.org/licenses/>.
 * END LICENSE
 */
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace BingSpotAny
{
    public partial class YesNoDialog : Window
    {
        public YesNoDialog() 
        { 
            InitializeComponent(); 
        }

        public YesNoDialog(string title, string message) : this()
        {
            Title = title;
            var txtMessage = this.FindControl<TextBlock>("TxtMessage");
            if (txtMessage != null) txtMessage.Text = message;
        }

        private void Yes_Click(object? sender, RoutedEventArgs e)
        {
            // Close the dialog and return true
            Close(true);
        }

        private void No_Click(object? sender, RoutedEventArgs e)
        {
            // Close the dialog and return false
            Close(false);
        }
    }
}