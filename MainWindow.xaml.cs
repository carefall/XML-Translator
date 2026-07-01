using Microsoft.Win32;
using System.Windows;

namespace XML_Translator
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new()
            {
                Filter = "XML files (*.xml)|*.xml"
            };
            if (dialog.ShowDialog() == true) OpenEditor(dialog.FileName);
            
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effects = DragDropEffects.Copy;
            else e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0 && System.IO.Path.GetExtension(files[0]).ToLower() == ".xml")
            {
                OpenEditor(files[0]);
            }
        }

        private void OpenEditor(string filePath)
        {
            XMLWindow window = new(filePath);
            window.Show();
            Close();
        }
    }
}