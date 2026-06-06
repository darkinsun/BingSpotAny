using Avalonia.Controls;
using Avalonia.Interactivity;

namespace BingSpotAny
{
    public partial class MessageBox : Window
    {
        public MessageBox()
        {
            InitializeComponent();
        }

        // Dışarıdan başlık ve mesaj metni alabileceğimiz özel kurucu metot
        public MessageBox(string title, string message) : this()
        {
            Title = title;
            var textBlock = this.FindControl<TextBlock>("MessageText");
            if (textBlock != null)
            {
                textBlock.Text = message;
            }
        }

        // Tamam butonuna basılınca pencereyi kapat
        private void CloseButton_Click(object? sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}