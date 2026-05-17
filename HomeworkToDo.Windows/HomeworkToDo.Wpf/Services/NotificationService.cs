using System.Windows;
using HomeworkToDo.Core.Models;
using HomeworkToDo.Core.Services;

namespace HomeworkToDo.Wpf.Services;

public static class NotificationService
{
    public static void ScheduleNotifications(List<Homework> homeworks, List<Exam> exams, List<int> thresholdsInMinutes)
    {
        // Windows Toast notifications via WinRT interop
        if (!IsToastSupported()) return;

        // Clear previous
        ClearNotifications();

        var now = DateTime.Now;

        foreach (var hw in homeworks.Where(h => !h.IsCompleted && !h.IsOverdue && h.DeadlineDate.HasValue))
        {
            foreach (var mins in thresholdsInMinutes)
            {
                var trigger = hw.DeadlineDate!.Value.AddMinutes(-mins);
                if (trigger <= now) continue;

                ScheduleToast(
                    $"hw_{hw.Id}_{mins}",
                    "作业截止提醒",
                    $"课程：{hw.CourseName}\n作业：{hw.Name}\n离截止时间还有约 {FormatMinutes(mins)}。",
                    trigger);
            }
        }

        foreach (var exam in exams.Where(e => !e.IsCompleted && !e.IsOverdue && e.DeadlineDate.HasValue))
        {
            foreach (var mins in thresholdsInMinutes)
            {
                var trigger = exam.DeadlineDate!.Value.AddMinutes(-mins);
                if (trigger <= now) continue;

                ScheduleToast(
                    $"exam_{exam.Id}_{mins}",
                    "考试截止提醒",
                    $"课程：{exam.CourseName}\n考试：{exam.Name}\n离截止时间还有约 {FormatMinutes(mins)}。",
                    trigger);
            }
        }
    }

    private static void ScheduleToast(string tag, string title, string body, DateTime triggerTime)
    {
        // Use a background timer to show the toast at the right time
        var delay = triggerTime - DateTime.Now;
        if (delay.TotalSeconds <= 0) return;

        var timer = new System.Threading.Timer(_ =>
        {
            ShowToast(title, body, tag);
        }, null, delay, System.Threading.Timeout.InfiniteTimeSpan);

        // Store timer reference to prevent GC
        _timers[tag] = timer;
    }

    private static readonly Dictionary<string, System.Threading.Timer> _timers = new();

    private static void ShowToast(string title, string body, string tag)
    {
        try
        {
            // Windows Toast via WinRT
            _ = ShowToastInternalAsync(title, body, tag);
        }
        catch { /* silently fail if toast not available */ }
    }

    private static async Task ShowToastInternalAsync(string title, string body, string tag)
    {
        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            try
            {
                var notification = new System.Windows.Controls.Primitives.Popup();
                // Simple approach: use taskbar notification area
                var notifyIcon = new System.Windows.Forms.NotifyIcon
                {
                    Visible = true,
                    Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!),
                    BalloonTipTitle = title,
                    BalloonTipText = body
                };
                notifyIcon.ShowBalloonTip(5000);

                // Clean up after showing
                System.Threading.Tasks.Task.Delay(10000).ContinueWith(_ =>
                {
                    System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        notifyIcon.Visible = false;
                        notifyIcon.Dispose();
                    });
                });
            }
            catch { /* WinForms NotifyIcon fallback */ }
        });
    }

    private static void ClearNotifications()
    {
        foreach (var timer in _timers.Values)
            timer.Dispose();
        _timers.Clear();
    }

    private static bool IsToastSupported()
    {
        try
        {
            // Check if we're on Windows
            return Environment.OSVersion.Platform == PlatformID.Win32NT;
        }
        catch { return false; }
    }

    internal static string FormatMinutes(int minutes)
    {
        if (minutes >= 1440)
        {
            var days = minutes / 1440;
            var hours = (minutes % 1440) / 60;
            return hours > 0 ? $"{days}天{hours}小时" : $"{days}天";
        }
        if (minutes >= 60)
        {
            var hours = minutes / 60;
            var mins = minutes % 60;
            return mins > 0 ? $"{hours}小时{mins}分钟" : $"{hours}小时";
        }
        return $"{minutes}分钟";
    }
}
