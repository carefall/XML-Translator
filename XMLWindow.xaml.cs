using Microsoft.Win32;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;

namespace XML_Translator
{
    public partial class XMLWindow : Window
    {
        private readonly List<OpenFile> _openFiles = new();
        private OpenFile? _currentFile;
        private ICollectionView? _view;
        private const double ScrollStep = 150;

        private string _searchText = "";
        private bool _hideApproved;
        private bool _hideUnchanged;
        private bool _hideChanged;

        public class OpenFile
        {
            public string Path { get; }

            public string Name => System.IO.Path.GetFileName(Path);

            public List<StringEntry> Entries { get; }

            public ICollectionView View { get; }

            public OpenFile(string path)
            {
                Path = path;

                Entries = StringEntry.LoadStrings(path);

                View = CollectionViewSource.GetDefaultView(Entries);
            }
        }

        public XMLWindow(string[] filePaths, bool folder)
        {
            InitializeComponent();

            if (folder)
                filePaths = Directory.GetFiles(filePaths[0], "*", SearchOption.AllDirectories);

            foreach (string path in filePaths)
            {
                var file = new OpenFile(path);
                _openFiles.Add(file);
                FilesList.Items.Add(file);
            }

            if (FilesList.Items.Count > 0)
                FilesList.SelectedIndex = 0;

            PreviewKeyDown += Window_PreviewKeyDown;
        }

        private void SetCurrentFile(OpenFile file)
        {
            _currentFile = file;

            DataContext = file.Entries;

            _view = file.View;
            _view.Filter = FilterItems;
            _view.Refresh();
        }

        private bool FilterItems(object obj)
        {
            if (obj is not StringEntry item)
                return false;

           
            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                string s = _searchText.ToLowerInvariant();

                bool match =
                    (item.Id?.ToLowerInvariant().Contains(s) ?? false) ||
                    (item.RuText?.ToLowerInvariant().Contains(s) ?? false) ||
                    (item.EngText?.ToLowerInvariant().Contains(s) ?? false) ||
                    (item.NewRuText?.ToLowerInvariant().Contains(s) ?? false) ||
                    (item.NewEngText?.ToLowerInvariant().Contains(s) ?? false);

                if (!match)
                    return false;
            }

            
            if (_hideApproved && item.IsApproved)
                return false;

            if (_hideUnchanged && !item.HasChanges)
                return false;

            
            if (_hideChanged && item.HasChanges)
                return false;

            return true;
        }

        private void FilterChanged(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb)
            {
                switch (cb.Content.ToString())
                {
                    case "Скрыть утверждённые":
                        _hideApproved = cb.IsChecked == true;
                        break;

                    case "Скрыть неизменённые":
                        _hideUnchanged = cb.IsChecked == true;
                        break;

                    case "Скрыть изменённые":
                        _hideChanged = cb.IsChecked == true;
                        break;
                }
            }

            _view?.Refresh();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
            {
                SearchBox.Focus();
                SearchBox.SelectAll();
                e.Handled = true;
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = SearchBox.Text;
            _view?.Refresh();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SaveFile();
        }

        private void SaveFile()
        {
            if (_currentFile == null)
                return;
            SaveFile( _currentFile );
        }

        private void SaveFile(OpenFile file)
        {
            if (file == null)
                return;
            XDocument doc = XDocument.Load(file.Path);
            var entries = file.Entries;
            foreach (var entry in entries)
            {
                if (!entry.IsApproved)
                    continue;

                if (!entry.HasChanges)
                {
                    entry.IsApproved = false;
                    continue;
                }

                var node = doc.Root!.Elements("string")
                    .FirstOrDefault(x => (string?)x.Attribute("id") == entry.Id);

                if (node == null)
                    continue;

                if (entry.HasRuChanges)
                {
                    var rus = node.Element("rus");
                    string text = StringEntry.EncodeMultiline(entry.NewRuText ?? "");
                    if (rus == null)
                        node.Add(new XElement("rus", text));
                    else
                        rus.Value = text;

                    entry.RuText = entry.NewRuText;
                }


                if (entry.HasEngChanges)
                {
                    var eng = node.Element("eng");
                    string text = StringEntry.EncodeMultiline(entry.NewEngText ?? "");
                    if (eng == null)
                        node.Add(new XElement("eng", text));
                    else
                        eng.Value = text;

                    entry.EngText = entry.NewEngText;
                }

                entry.IsApproved = false;
            }

            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.GetEncoding(1251),
                Indent = true
            };

            using var writer = XmlWriter.Create(file.Path, settings);
            doc.Save(writer);
            var result = MessageBox.Show("Файл сохранён. Желаете закрыть его?", "XML Translator", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                if (_currentFile == null)
                    return;

                int index = FilesList.SelectedIndex;

                _openFiles.Remove(_currentFile);
                FilesList.Items.Remove(_currentFile);

                if (FilesList.Items.Count == 0)
                {
                    Close();
                    return;
                }

                FilesList.SelectedIndex = Math.Min(index, FilesList.Items.Count - 1);
            }
        }

        private void LoadBufferTranslation_Click(object sender, RoutedEventArgs e)
        {
            if (_currentFile == null)
                return;

            if (!Clipboard.ContainsText())
            {
                MessageBox.Show("Буфер обмена не содержит текста.");
                return;
            }

            var translations = StringEntry
                .LoadStringsFromXml(Clipboard.GetText())
                .ToDictionary(x => x.Id!);

            foreach (var item in _currentFile.Entries)
            {
                if (translations.TryGetValue(item.Id!, out var tr) &&
                    !string.IsNullOrWhiteSpace(tr.EngText))
                {
                    item.NewEngText = tr.EngText;
                }
                if (translations.TryGetValue(item.Id, out var tr2) &&
                    !string.IsNullOrWhiteSpace(tr2.RuText))
                {
                    item.NewRuText = tr2.RuText;
                }
            }

            _view?.Refresh();

            MessageBox.Show("Переводы загружены из буфера.", "XML Translator");
        }

        private void LoadTranslation_Click(object sender, RoutedEventArgs e)
        {
            if (_currentFile == null)
                return;

            OpenFileDialog dialog = new()
            {
                Filter = "XML файлы (*.xml)|*.xml"
            };

            if (dialog.ShowDialog() != true)
                return;

            var translations = StringEntry.LoadStrings(dialog.FileName).ToDictionary(x => x.Id!);
            foreach (var item in _currentFile.Entries)
            {
                if (translations.TryGetValue(item.Id!, out var tr) &&
                    !string.IsNullOrWhiteSpace(tr.EngText))
                {
                    item.NewEngText = tr.EngText;
                }
            }

            _view?.Refresh();

            MessageBox.Show("Переводы загружены", "XML Translator");
        }

        private void Approve_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not StringEntry clicked)
                return;

            var selected = Grid.SelectedItems.Cast<StringEntry>().ToList();
            if (!selected.Contains(clicked))
                selected = new List<StringEntry> { clicked };

            if (clicked.IsApproved)
            {
                foreach (var item in selected.Where(x => x.IsApproved))
                    item.IsApproved = false;
            }
            else
            {
                foreach (var item in selected.Where(x => x.HasChanges && !x.IsApproved))
                    item.IsApproved = true;
            }
        }

        private void EditLongText(object sender, MouseButtonEventArgs e)
        {
            if (sender is not TextBox tb)
                return;
            if (tb.IsReadOnly)
                return;
            var title = tb.DataContext is StringEntry entry &&
            tb.GetBindingExpression(TextBox.TextProperty)?.ParentBinding.Path.Path == nameof(StringEntry.NewRuText)
    ? "Редактирование русского текста"
    : "Редактирование английского перевода";

            var dlg = new TextEditWindow(tb.Text)
            {
                Owner = this,
                Title = title
            };

            if (dlg.ShowDialog() == true)
            {
                tb.Text = dlg.ResultText;
            }

            e.Handled = true;
        }

        private void FilesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FilesList.SelectedItem is OpenFile file)
                SetCurrentFile(file);
        }

        private static T? FindVisualChild<T>(DependencyObject parent)
    where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T result)
                    return result;

                var nested = FindVisualChild<T>(child);
                if (nested != null)
                    return nested;
            }

            return null;
        }

        private void TabsLeft_Click(object sender, RoutedEventArgs e)
        {
            TabsScroll.ScrollToHorizontalOffset(
                TabsScroll.HorizontalOffset - ScrollStep);
        }

        private void TabsRight_Click(object sender, RoutedEventArgs e)
        {
            TabsScroll.ScrollToHorizontalOffset(
                TabsScroll.HorizontalOffset + ScrollStep);
        }

        private void FilesList_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            TabsScroll.ScrollToHorizontalOffset(
                TabsScroll.HorizontalOffset - e.Delta);

            e.Handled = true;
        }

        private void CloseFile(OpenFile file)
        {
            if (file == null)
                return;

            // есть ли утверждённые, но несохранённые изменения
            bool hasUnsavedApprovedChanges = file.Entries
                .Any(x => x.IsApproved && x.HasChanges);

            if (hasUnsavedApprovedChanges)
            {
                var result = MessageBox.Show(
                    "Есть несохранённые утверждённые изменения. Сохранить перед закрытием?",
                    "XML Translator",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Cancel)
                    return;

                if (result == MessageBoxResult.Yes)
                {
                    SaveFile(file);
                }
            }

            // удаляем из коллекций
            _openFiles.Remove(file);
            FilesList.Items.Remove(file);

            // если закрыли текущий файл — переключаемся
            if (_currentFile == file)
            {
                _currentFile = null;

                if (_openFiles.Count > 0)
                {
                    var next = _openFiles.FirstOrDefault();
                    if (next != null)
                    {
                        FilesList.SelectedItem = next;
                    }
                }
                else
                {
                    DataContext = null;
                }
            }

            // если ничего не осталось — закрываем окно
            if (_openFiles.Count == 0)
            {
                Close();
            }
        }

        private void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is OpenFile file)
            {
                CloseFile(file);
            }
        }

        private void TabsScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scroll = (ScrollViewer)sender;

            double offset = scroll.HorizontalOffset - e.Delta * 0.5;
            scroll.ScrollToHorizontalOffset(offset);

            e.Handled = true;
        }


    }
}