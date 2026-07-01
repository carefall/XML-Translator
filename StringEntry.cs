using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Xml.Linq;

public class StringEntry : INotifyPropertyChanged
{
    private bool _isApproved;

    private string? _newEngText;
    private string? _engText;

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

    public bool HasChanges => EngText != NewEngText;

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
                    RuText = x.Element("rus")?.Value ?? "",
                    EngText = x.Element("eng")?.Value ?? "",
                    NewEngText = x.Element("eng")?.Value ?? ""
                }
            ).ToList();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка загрузки XML");
            return new List<StringEntry>();
        }
    }
}