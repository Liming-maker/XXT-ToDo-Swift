using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using HomeworkToDo.Core.Models;
using HomeworkToDo.Core.Services;

namespace HomeworkToDo.Wpf.ViewModels;

public class SettingsViewModel : INotifyPropertyChanged
{
    private readonly StorageService _storage = new();

    public SettingsViewModel()
    {
        SaveCommand = new RelayCommand(_ => Save());
        AddThresholdCommand = new RelayCommand(_ => AddThreshold());
        RemoveThresholdCommand = new RelayCommand<int>(RemoveThreshold);
        OpenCourseSelectionCommand = new RelayCommand(_ => IsCourseSelectionOpen = true);

        Load();
    }

    private string _phone = "";
    public string Phone
    {
        get => _phone;
        set { _phone = value; OnPropertyChanged(); }
    }

    private string _password = "";
    public string Password
    {
        get => _password;
        set { _password = value; OnPropertyChanged(); }
    }

    private double _refreshInterval = 30;
    public double RefreshInterval
    {
        get => _refreshInterval;
        set { _refreshInterval = value; OnPropertyChanged(); }
    }

    private string _newThreshold = "";
    public string NewThreshold
    {
        get => _newThreshold;
        set { _newThreshold = value; OnPropertyChanged(); }
    }

    private bool _autoStart;
    public bool AutoStart
    {
        get => _autoStart;
        set { _autoStart = value; OnPropertyChanged(); }
    }

    public ObservableCollection<int> Thresholds { get; } = new();

    private bool _isCourseSelectionOpen;
    public bool IsCourseSelectionOpen
    {
        get => _isCourseSelectionOpen;
        set { _isCourseSelectionOpen = value; OnPropertyChanged(); }
    }

    public ICommand SaveCommand { get; }
    public ICommand AddThresholdCommand { get; }
    public ICommand RemoveThresholdCommand { get; }
    public ICommand OpenCourseSelectionCommand { get; }

    public event Action? Saved;

    private void Load()
    {
        var s = _storage.LoadSettings();
        Phone = s.Phone;
        Password = s.Password;
        RefreshInterval = s.RefreshInterval;
        AutoStart = s.AutoStart;

        Thresholds.Clear();
        foreach (var t in s.NotificationThresholds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                     .Select(v => int.TryParse(v.Trim(), out var n) ? n : -1).Where(v => v > 0))
            Thresholds.Add(t);

        if (Thresholds.Count == 0)
        {
            Thresholds.Add(60);
            Thresholds.Add(1440);
        }
    }

    private void Save()
    {
        var s = new AppSettings
        {
            Phone = Phone,
            Password = Password,
            RefreshInterval = RefreshInterval,
            AutoStart = AutoStart,
            NotificationThresholds = string.Join(",", Thresholds),
            SelectedCourseIds = _storage.LoadSettings().SelectedCourseIds
        };
        _storage.SaveSettings(s);
        Saved?.Invoke();
    }

    private void AddThreshold()
    {
        if (int.TryParse(NewThreshold, out var val) && val > 0 && !Thresholds.Contains(val))
        {
            Thresholds.Add(val);
            var sorted = Thresholds.OrderBy(x => x).ToList();
            Thresholds.Clear();
            foreach (var v in sorted) Thresholds.Add(v);
            NewThreshold = "";
        }
    }

    private void RemoveThreshold(int value) => Thresholds.Remove(value);

    public void OnCourseSelectionClosed()
    {
        IsCourseSelectionOpen = false;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
