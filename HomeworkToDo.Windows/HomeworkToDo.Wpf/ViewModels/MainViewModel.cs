using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using HomeworkToDo.Core.Models;
using HomeworkToDo.Core.Services;
using HomeworkToDo.Wpf.Services;

namespace HomeworkToDo.Wpf.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly XxtService _service = new();
    private readonly StorageService _storage = new();
    private readonly BackgroundService _background;

    public MainViewModel()
    {
        _background = new BackgroundService(RefreshDataAsync);
        RefreshCommand = new RelayCommand(async _ => await RefreshDataAsync());
        OpenSettingsCommand = new RelayCommand(_ => OpenSettings());
        UpdateHomeworkCommand = new RelayCommand<Homework>(async hw => await UpdateHomeworkDeadline(hw));
        UpdateExamCommand = new RelayCommand<Exam>(async exam => await UpdateExamDeadline(exam));

        LoadSavedData();
    }

    public ObservableCollection<Homework> Homeworks { get; } = new();
    public ObservableCollection<Exam> Exams { get; } = new();

    private Homework? _selectedHomework;
    public Homework? SelectedHomework
    {
        get => _selectedHomework;
        set { _selectedHomework = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsHomeworkDialogOpen)); }
    }

    private Exam? _selectedExam;
    public Exam? SelectedExam
    {
        get => _selectedExam;
        set { _selectedExam = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsExamDialogOpen)); }
    }

    public bool IsHomeworkDialogOpen => SelectedHomework != null;
    public bool IsExamDialogOpen => SelectedExam != null;

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    private string? _errorMessage;
    public string? ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); }
    }

    private bool _isSettingsOpen;
    public bool IsSettingsOpen
    {
        get => _isSettingsOpen;
        set { _isSettingsOpen = value; OnPropertyChanged(); }
    }

    private string _statusText = "";
    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); }
    }

    public ICommand RefreshCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand UpdateHomeworkCommand { get; }
    public ICommand UpdateExamCommand { get; }

    public void StartBackgroundRefresh()
    {
        var settings = _storage.LoadSettings();
        _background.Start(TimeSpan.FromMinutes(settings.RefreshInterval));
    }

    public void StopBackgroundRefresh() => _background.Stop();

    public async Task RefreshDataAsync()
    {
        var settings = _storage.LoadSettings();
        if (string.IsNullOrEmpty(settings.Phone) || string.IsNullOrEmpty(settings.Password))
        {
            ErrorMessage = "请在设置中配置账号密码";
            IsSettingsOpen = true;
            return;
        }

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var success = await _service.LoginAsync(settings.Phone, settings.Password);
            if (!success)
            {
                ErrorMessage = "登录失败，请检查账号密码";
                return;
            }

            var allHomework = await _service.FetchAllHomeworkAsync();
            var allExams = await _service.FetchAllExamsAsync();

            // Deduplication & sorting
            var orderedHw = DeduplicateAndSortHomework(allHomework);
            var orderedExams = DeduplicateAndSortExams(allExams);

            _storage.SaveHomeworks(orderedHw);
            _storage.SaveExams(orderedExams);

            ApplyFiltering(orderedHw, orderedExams);

            // Schedule notifications
            var thresholds = ParseThresholds(settings.NotificationThresholds);
            NotificationService.ScheduleNotifications(orderedHw, orderedExams, thresholds);

            StatusText = $"最后更新: {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"加载失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void ApplyFiltering()
    {
        var settings = _storage.LoadSettings();
        var homeworks = _storage.LoadHomeworks();
        var exams = _storage.LoadExams();
        ApplyFiltering(homeworks, exams, settings);
    }

    private void ApplyFiltering(List<Homework> homeworks, List<Exam> exams, Core.Models.AppSettings? settings = null)
    {
        settings ??= _storage.LoadSettings();

        var thresholds = ParseThresholds(settings.NotificationThresholds);
        NotificationService.ScheduleNotifications(homeworks, exams, thresholds);

        var selectedIds = string.IsNullOrEmpty(settings.SelectedCourseIds)
            ? new HashSet<string>()
            : settings.SelectedCourseIds.Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

        Homeworks.Clear();
        Exams.Clear();

        if (selectedIds.Count == 0)
        {
            foreach (var hw in homeworks) Homeworks.Add(hw);
            foreach (var exam in exams) Exams.Add(exam);
        }
        else
        {
            foreach (var hw in homeworks.Where(h => selectedIds.Contains(h.CourseId)))
                Homeworks.Add(hw);
            foreach (var exam in exams.Where(e => selectedIds.Contains(e.CourseId)))
                Exams.Add(exam);
        }
    }

    private async Task UpdateHomeworkDeadline(Homework? hw)
    {
        if (hw == null) return;
        try
        {
            var settings = _storage.LoadSettings();
            var success = await _service.LoginAsync(settings.Phone, settings.Password);
            if (success)
            {
                var updated = await _service.UpdateHomeworkDeadlineAsync(hw);
                var homeworks = _storage.LoadHomeworks();
                var idx = homeworks.FindIndex(h => h.Id == hw.Id);
                if (idx >= 0)
                {
                    homeworks[idx] = updated;
                    _storage.SaveHomeworks(homeworks);
                    ApplyFiltering();
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"更新失败: {ex.Message}";
        }
    }

    private async Task UpdateExamDeadline(Exam? exam)
    {
        if (exam == null) return;
        try
        {
            var settings = _storage.LoadSettings();
            var success = await _service.LoginAsync(settings.Phone, settings.Password);
            if (success)
            {
                var updated = await _service.UpdateExamDeadlineAsync(exam);
                var exams = _storage.LoadExams();
                var idx = exams.FindIndex(e => e.Id == exam.Id);
                if (idx >= 0)
                {
                    exams[idx] = updated;
                    _storage.SaveExams(exams);
                    ApplyFiltering();
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"更新失败: {ex.Message}";
        }
    }

    private void OpenSettings()
    {
        IsSettingsOpen = true;
    }

    public void OnSettingsClosed()
    {
        IsSettingsOpen = false;
        _ = RefreshDataAsync();
    }

    private void LoadSavedData()
    {
        var homeworks = _storage.LoadHomeworks();
        var exams = _storage.LoadExams();

        if (homeworks.Count == 0 && exams.Count == 0)
        {
            _ = RefreshDataAsync();
        }
        else
        {
            ApplyFiltering(homeworks, exams);
            StatusText = $"上次数据: {File.GetLastWriteTime(GetSavedFilePath()):HH:mm:ss}";
        }
    }

    private string GetSavedFilePath()
    {
        var basePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HomeworkToDo");
        return Path.Combine(basePath, "saved_homeworks.json");
    }

    private static List<Homework> DeduplicateAndSortHomework(List<Homework> homeworks)
    {
        var seen = new HashSet<string>();
        var deduped = new List<Homework>();
        foreach (var hw in homeworks)
        {
            if (seen.Add(hw.Id)) deduped.Add(hw);
        }

        deduped.Sort((a, b) =>
        {
            var aAction = !a.IsCompleted && !a.IsOverdue;
            var bAction = !b.IsCompleted && !b.IsOverdue;
            if (aAction != bAction) return aAction ? -1 : 1;
            if (aAction) return DateTime.Compare(a.DeadlineDate ?? DateTime.MaxValue, b.DeadlineDate ?? DateTime.MaxValue);
            return 0;
        });
        return deduped;
    }

    private static List<Exam> DeduplicateAndSortExams(List<Exam> exams)
    {
        var seen = new HashSet<string>();
        var deduped = new List<Exam>();
        foreach (var exam in exams)
        {
            if (seen.Add(exam.Id)) deduped.Add(exam);
        }

        deduped.Sort((a, b) =>
        {
            var aAction = !a.IsCompleted && !a.IsOverdue;
            var bAction = !b.IsCompleted && !b.IsOverdue;
            if (aAction != bAction) return aAction ? -1 : 1;
            if (aAction) return DateTime.Compare(a.DeadlineDate ?? DateTime.MaxValue, b.DeadlineDate ?? DateTime.MaxValue);
            return 0;
        });
        return deduped;
    }

    private static List<int> ParseThresholds(string data)
        => data.Split(',', StringSplitOptions.RemoveEmptyEntries)
               .Select(s => int.TryParse(s.Trim(), out var v) ? v : -1)
               .Where(v => v > 0)
               .ToList();

    // INotifyPropertyChanged
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

// Simple RelayCommand
public class RelayCommand : ICommand
{
    private readonly Func<object?, Task>? _asyncAction;
    private readonly Action<object?>? _action;
    private bool _isExecuting;

    public RelayCommand(Action<object?> action) => _action = action;
    public RelayCommand(Func<object?, Task> asyncAction) => _asyncAction = asyncAction;

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => !_isExecuting;

    public async void Execute(object? parameter)
    {
        if (_isExecuting) return;
        _isExecuting = true;
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        try
        {
            if (_asyncAction != null) await _asyncAction(parameter);
            else _action?.Invoke(parameter);
        }
        finally
        {
            _isExecuting = false;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public class RelayCommand<T> : ICommand
{
    private readonly Func<T?, Task>? _asyncAction;
    private readonly Action<T?>? _action;
    private bool _isExecuting;

    public RelayCommand(Func<T?, Task> asyncAction) => _asyncAction = asyncAction;
    public RelayCommand(Action<T?> action) => _action = action;

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => !_isExecuting;

    public async void Execute(object? parameter)
    {
        if (_isExecuting) return;
        _isExecuting = true;
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        try
        {
            if (_asyncAction != null) await _asyncAction((T?)parameter);
            else _action?.Invoke((T?)parameter);
        }
        finally
        {
            _isExecuting = false;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
