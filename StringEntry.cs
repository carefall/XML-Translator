using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Xml.Linq;

public class StringEntry : INotifyPropertyChanged
{
    private bool _isApproved;

    private string? _newEngText;
    private string? _engText;
    private string? _newRuText;

    public string? NewRuText
    {
        get => _newRuText;
        set
        {
            _newRuText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasChanges));
        }
    }

    public string? Id { get; set; }
    public string? RuText { get; set; }

    public string? EngText
    {
        get => _engText;
        set
        {
            if (_engText != value)
            {
                _engText = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasChanges));
            }
        }
    }

    public string? NewEngText
    {
        get => _newEngText;
        set
        {
            _newEngText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasChanges));
        }
    }

    public bool IsApproved
    {
        get => _isApproved;
        set
        {
            _isApproved = value;
            OnPropertyChanged();
        }
    }

    public bool HasEngChanges => EngText != NewEngText;
    public bool HasRuChanges => RuText != NewRuText;
    public bool HasChanges => HasEngChanges || HasRuChanges;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public static List<StringEntry> LoadStrings(string fileName)
    {
        try
        {
            XDocument doc = XDocument.Load(fileName);
            return doc.Root!.Elements("string").Select(x => new StringEntry
            {
                Id = x.Attribute("id")?.Value ?? "",
                RuText = DecodeMultiline(x.Element("rus")?.Value ?? ""),
                NewRuText = DecodeMultiline(x.Element("rus")?.Value ?? ""),
                EngText = DecodeMultiline(x.Element("eng")?.Value ?? ""),
                NewEngText = DecodeMultiline(x.Element("eng")?.Value ?? ""),
            }).ToList();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка загрузки XML");
            return new List<StringEntry>();
        }
    }

    public static string DecodeMultiline(string text)
    {
        return text.Replace("\\n", Environment.NewLine);
    }

    public static string EncodeMultiline(string text)
    {
        return text.Replace("\r\n", "\\n")
                   .Replace("\n", "\\n");
    }
}