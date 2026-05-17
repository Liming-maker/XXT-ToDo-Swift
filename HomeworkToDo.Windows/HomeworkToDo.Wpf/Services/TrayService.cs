using System.Windows;
using HWND = System.IntPtr;
using WPF = System.Windows.Application;

namespace HomeworkToDo.Wpf.Services;

public class TrayService : IDisposable
{
    private System.Windows.Forms.NotifyIcon? _trayIcon;
    private readonly Window _mainWindow;

    public TrayService(Window mainWindow)
    {
        _mainWindow = mainWindow;
        CreateTrayIcon();
    }

    private void CreateTrayIcon()
    {
        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Text = "学习通待办",
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!),
            Visible = true
        };

        var contextMenu = new System.Windows.Forms.ContextMenuStrip();

        contextMenu.Items.Add("显示主窗口", null, (_, _) => ShowMainWindow());
        contextMenu.Items.Add("立即刷新", null, async (_, _) => await RefreshData());
        contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        contextMenu.Items.Add("退出", null, (_, _) => ExitApplication());

        _trayIcon.ContextMenuStrip = contextMenu;
        _trayIcon.DoubleClick += (_, _) => ShowMainWindow();
        _trayIcon.BalloonTipClicked += (_, _) => ShowMainWindow();
    }

    private void ShowMainWindow()
    {
        WPF.Current.Dispatcher.Invoke(() =>
        {
            _mainWindow.Show();
            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Activate();
        });
    }

    private static async System.Threading.Tasks.Task RefreshData()
    {
        await WPF.Current.Dispatcher.InvokeAsync(async () =>
        {
            if (WPF.Current.MainWindow?.DataContext is ViewModels.MainViewModel vm)
            {
                await vm.RefreshDataAsync();
            }
        });
    }

    private static void ExitApplication()
    {
        WPF.Current.Dispatcher.Invoke(() =>
        {
            WPF.Current.Shutdown();
        });
    }

    public void ShowNotification(string title, string text, int timeoutMs = 3000)
    {
        _trayIcon?.ShowBalloonTip(timeoutMs, title, text, System.Windows.Forms.ToolTipIcon.Info);
    }

    public void UpdateTooltip(string text)
    {
        if (_trayIcon != null)
            _trayIcon.Text = text;
    }

    public void Dispose()
    {
        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }
    }
}
