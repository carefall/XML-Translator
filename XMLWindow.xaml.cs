using Microsoft.Win32;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Xml;
using System.Xml.Linq;

namespace XML_Translator
{
    public partial class XMLWindow : Window
    {
        private readonly string _filePath;
        private ICollectionView _view;
        private string _searchText = "";
        private bool _hideApproved;
        private bool _hideUnchanged;
        private bool _hideChanged;

        public XMLWindow(string filePath)
        {
            InitializeComponent();
            _filePath = filePath;
            DataContext = StringEntry.LoadStrings(filePath);
            _view = CollectionViewSource.GetDefaultView(DataContext);
            _view.Filter = FilterItems;
            PreviewKeyDown += Window_PreviewKeyDown;
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
            try
            {
                XDocument doc = XDocument.Load(_filePath);
                var entries = (List<StringEntry>)DataContext;

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

                using var writer = XmlWriter.Create(_filePath, settings);
                doc.Save(writer);
                MessageBox.Show("Файл сохранён", "XML Translator");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка сохранения");
            }
        }

        private void Return_Click(object sender, RoutedEventArgs e)
        {
            MainWindow window = new();
            window.Show();
            Close();
        }

        private void LoadTranslation_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new()
            {
                Filter = "XML файлы (*.xml)|*.xml"
            };
            if (dialog.ShowDialog() != true)
                return;
            var secondFile = StringEntry.LoadStrings(dialog.FileName);
            var current = (List<StringEntry>)DataContext;
            foreach (var item in current)
            {
                var match = secondFile.FirstOrDefault(x => x.Id == item.Id);
                if (match != null && !string.IsNullOrWhiteSpace(match.EngText)) item.NewEngText = match.EngText;
            }
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
    }
}