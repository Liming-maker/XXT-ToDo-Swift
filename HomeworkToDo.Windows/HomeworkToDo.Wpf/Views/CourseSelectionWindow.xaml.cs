using System.Windows;
using HomeworkToDo.Wpf.ViewModels;

namespace HomeworkToDo.Wpf.Views;

public partial class CourseSelectionWindow : Window
{
    private readonly CourseSelectionViewModel _vm;

    public CourseSelectionWindow()
    {
        InitializeComponent();
        _vm = new CourseSelectionViewModel();
        DataContext = _vm;
        _vm.Saved += () =>
        {
            DialogResult = true;
            Close();
        };
    }
}
