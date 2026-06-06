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