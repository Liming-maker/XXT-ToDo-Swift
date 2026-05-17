using System.Windows;
using HomeworkToDo.Core.Services;
using HomeworkToDo.Wpf.ViewModels;

namespace HomeworkToDo.Wpf.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _vm;

    public SettingsWindow()
    {
        InitializeComponent();

        _vm = new SettingsViewModel();
        DataContext = _vm;

        // Load password from storage
        var storage = new StorageService();
        var settings = storage.LoadSettings();
        PasswordBox.Password = settings.Password;

        PasswordBox.PasswordChanged += (_, _) => _vm.Password = PasswordBox.Password;

        _vm.Saved += OnSaved;
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SettingsViewModel.IsCourseSelectionOpen) && _vm.IsCourseSelectionOpen)
            {
                var csw = new CourseSelectionWindow { Owner = this };
                csw.ShowDialog();
                _vm.OnCourseSelectionClosed();
            }
        };
    }

    private void OnSaved()
    {
        DialogResult = true;
        Close();
    }
}
