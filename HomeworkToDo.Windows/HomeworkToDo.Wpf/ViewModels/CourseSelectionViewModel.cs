using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using HomeworkToDo.Core.Models;
using HomeworkToDo.Core.Services;

namespace HomeworkToDo.Wpf.ViewModels;

public class CourseSelectionViewModel : INotifyPropertyChanged
{
    private readonly XxtService _service = new();
    private readonly StorageService _storage = new();

    public CourseSelectionViewModel()
    {
        SaveCommand = new RelayCommand(_ => Save());
        LoadData();
    }

    public ObservableCollection<SelectableFolder> Folders { get; } = new();

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    public ICommand SaveCommand { get; }

    public event Action? Saved;

    private async void LoadData()
    {
        var settings = _storage.LoadSettings();

        IsLoading = true;
        try
        {
            var loginOk = await _service.LoginAsync(settings.Phone, settings.Password);
            if (loginOk)
            {
                var folders = await _service.FetchCourseFoldersAsync();

                // Load existing selections
                var selectedIds = string.IsNullOrEmpty(settings.SelectedCourseIds)
                    ? new HashSet<string>()
                    : settings.SelectedCourseIds.Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

                Folders.Clear();
                foreach (var f in folders)
                {
                    var sf = new SelectableFolder
                    {
                        Id = f.Id,
                        Name = f.Name,
                        Courses = f.Courses.Select(c => new SelectableCourse
                        {
                            CourseId = c.Id,
                            Name = c.Name,
                            Teacher = c.Teacher,
                            IsSelected = selectedIds.Contains(c.Id) || selectedIds.Count == 0
                        }).ToList()
                    };
                    Folders.Add(sf);
                }
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Save()
    {
        var selectedIds = Folders.SelectMany(f => f.Courses)
                                 .Where(c => c.IsSelected)
                                 .Select(c => c.CourseId)
                                 .ToList();

        var settings = _storage.LoadSettings();
        settings.SelectedCourseIds = string.Join(",", selectedIds);
        _storage.SaveSettings(settings);
        Saved?.Invoke();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
