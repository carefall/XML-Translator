using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

namespace XML_Translator
{
    public partial class XMLWindow : Window
    {
        private readonly string _filePath;

        public XMLWindow(string filePath)
        {
            InitializeComponent();
            _filePath = filePath;
            DataContext = StringEntry.LoadStrings(filePath);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                XDocument doc = XDocument.Load(_filePath);
                var entries = (List<StringEntry>)DataContext;
                foreach (var entry in entries)
                {
                    if (!entry.IsApproved) continue;
                    if (entry.EngText == entry.NewEngText)
                    {
                        entry.IsApproved = false;
                        continue;
                    }
                    var node = doc.Root!.Elements("string").FirstOrDefault(x => (string?)x.Attribute("id") == entry.Id);
                    if (node == null) continue;
                    var eng = node.Element("eng");
                    if (eng == null) node.Add(new XElement("eng", entry.NewEngText));
                    else eng.Value = entry.NewEngText!;
                    entry.EngText = entry.NewEngText;
                    entry.IsApproved = false;
                }
                doc.Save(_filePath);
                MessageBox.Show("Файл сохранён", "XML Translator");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка сохранения");
            }
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
            if (sender is Button btn && btn.DataContext is StringEntry entry) entry.IsApproved = !entry.IsApproved; 
        }
    }
}