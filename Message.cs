using System.ComponentModel;
using System.Text.Json.Serialization;

namespace BobikApp;

public class Message : INotifyPropertyChanged
{
    private bool _isSelected;
    private string _role;
    private string _content;
    private string _displayContent;
    
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
                OnPropertyChanged(nameof(BackgroundColor)); // Добавлено для динамического изменения цвета
            }
        }
    }
    
    [JsonIgnore] // Добавляем это, чтобы свойство не сериализовалось
    public Color BackgroundColor => IsSelected ? Color.FromArgb("#4D90D2") : 
        (Role == "user" ? Color.FromArgb("#273f87") : Color.FromArgb("#08457E"));

    [JsonPropertyName("role")]
    public string Role
    {
        get => _role;
        set
        {
            _role = value;
            OnPropertyChanged(nameof(Role));
        }
    }

    [JsonPropertyName("content")]
    public string Content
    {
        get => _content;
        set
        {
            _content = value;
            OnPropertyChanged(nameof(Content));
            DisplayContent = value.Replace(FilterPhrase, string.Empty).Trim() ?? string.Empty;
        }
    }

    public string DisplayContent
    {
        get => _displayContent;
        set
        {
            _displayContent = value;
            OnPropertyChanged(nameof(DisplayContent));
        }
    }

    private const string FilterPhrase =
        "Отвечай как голосовой ассистент, дружелюбно и по делу, но строго не более 650 символов!";

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}