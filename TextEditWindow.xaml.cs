using System.Windows;


namespace XML_Translator
{
    public partial class TextEditWindow : Window
    {
        public string ResultText => Editor.Text;

        public TextEditWindow(string text)
        {
            InitializeComponent();
            Editor.Text = text;
            Editor.Focus();
            Editor.CaretIndex = Editor.Text.Length;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
