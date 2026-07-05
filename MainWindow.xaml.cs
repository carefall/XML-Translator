using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using static XML_Translator.XMLWindow;

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
                Filter = "XML files (*.xml)|*.xml",
                Multiselect = true,
                Title = "Выберите файл(ы) для редактирования"
            };
            if (dialog.ShowDialog() == true) OpenEditor(dialog.FileNames, false);
            
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
                OpenEditor([files[0]], false);
            }
        }

        private void OpenEditor(string[] paths, bool folder)
        {
            XMLWindow window = new(paths, folder);
            window.Show();
            Close();
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog dialog = new()
            {
                Title = "Выберите папку с xml файлами",
                Multiselect = false
            };
            if (dialog.ShowDialog() == true) OpenEditor([dialog.FolderName], true);
        }



    }
}