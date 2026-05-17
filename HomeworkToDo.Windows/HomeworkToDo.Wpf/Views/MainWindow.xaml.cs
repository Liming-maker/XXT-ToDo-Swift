using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using HomeworkToDo.Core.Models;
using HomeworkToDo.Wpf.Services;
using HomeworkToDo.Wpf.ViewModels;

namespace HomeworkToDo.Wpf.Views;

public partial class MainWindow : Window
{
    private readonly TrayService _trayService;
    private readonly DispatcherTimer _refreshTimer = new();
    private readonly MainViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();

        _vm = new MainViewModel();
        DataContext = _vm;

        _trayService = new TrayService(this);

        // Periodic UI refresh for countdowns
        _refreshTimer.Interval = TimeSpan.FromSeconds(30);
        _refreshTimer.Tick += (_, _) =>
        {
            if (_vm.Homeworks.Count > 0)
            {
                // Force re-evaluation of remaining time display
                var hwCopy = _vm.Homeworks.ToList();
                _vm.Homeworks.Clear();
                foreach (var h in hwCopy) _vm.Homeworks.Add(h);
            }
            if (_vm.Exams.Count > 0)
            {
                var examCopy = _vm.Exams.ToList();
                _vm.Exams.Clear();
                foreach (var e in examCopy) _vm.Exams.Add(e);
            }
        };
        _refreshTimer.Start();

        // Monitor for opening settings
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.IsSettingsOpen) && _vm.IsSettingsOpen)
            {
                var sw = new SettingsWindow { Owner = this };
                sw.ShowDialog();
                _vm.OnSettingsClosed();
            }
        };

        _vm.StartBackgroundRefresh();

        Loaded += (_, _) => HideWindowIfBackgroundLaunch();
    }

    private void HideWindowIfBackgroundLaunch()
    {
        var args = Environment.GetCommandLineArgs();
        if (args.Length > 1 && args[1] == "--background")
            Hide();
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
        _trayService.ShowNotification("学习通待办", "应用已最小化到系统托盘", 1000);
    }

    protected override void OnStateChanged(EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
            _trayService.ShowNotification("学习通待办", "应用已最小化到系统托盘", 1000);
        }
        base.OnStateChanged(e);
    }

    // Event handlers for item double-click
    private void HomeworkItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is Homework hw)
        {
            var result = System.Windows.MessageBox.Show(
                $"{hw.CourseName}\n{hw.Name}\n\n当前截止时间：\n{hw.Deadline}\n\n是否更新截止时间？",
                "作业详情",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
            {
                _vm.UpdateHomeworkCommand.Execute(hw);
            }
        }
    }

    private void ExamItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is Exam exam)
        {
            var result = System.Windows.MessageBox.Show(
                $"{exam.CourseName}\n{exam.Name}\n\n当前截止时间：\n{exam.Deadline}\n\n是否更新截止时间？",
                "考试详情",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (result == MessageBoxResult.Yes)
            {
                _vm.UpdateExamCommand.Execute(exam);
            }
        }
    }
}
