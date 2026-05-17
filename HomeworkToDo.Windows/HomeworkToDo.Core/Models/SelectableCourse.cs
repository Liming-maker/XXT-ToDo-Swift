using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HomeworkToDo.Core.Models;

public class SelectableCourse : INotifyPropertyChanged
{
    private bool _isSelected;

    public string CourseId { get; init; } = "";
    public string Name { get; init; } = "";
    public string Teacher { get; init; } = "";

    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
