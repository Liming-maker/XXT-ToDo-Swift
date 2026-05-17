using System.Text.Json.Serialization;

namespace HomeworkToDo.Core.Models;

public class Homework
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Status { get; init; } = "";
    public string CourseName { get; init; } = "";
    public string CourseId { get; init; } = "";
    public string Deadline { get; init; } = "";
    public string? DetailUrl { get; init; }
    public bool IsPreciseDeadline { get; init; }

    [JsonIgnore]
    public string RemainingTimeDisplay => RemainingTime();

    [JsonIgnore]
    public DateTime? DeadlineDate
    {
        get
        {
            var formats = new[]
            {
                "yyyy-MM-dd HH:mm",
                "yyyy-MM-dd HH:mm:ss",
                "yyyy/MM/dd HH:mm",
                "yyyy年MM月dd日 HH:mm"
            };
            foreach (var fmt in formats)
            {
                if (DateTime.TryParseExact(Deadline, fmt, null,
                    System.Globalization.DateTimeStyles.None, out var dt))
                    return dt;
            }
            return null;
        }
    }

    [JsonIgnore]
    public bool IsCompleted
    {
        get
        {
            var done = new[] { "已完成", "已批阅", "已提交", "待批阅", "已互评" };
            return done.Any(s => Status.Contains(s));
        }
    }

    [JsonIgnore]
    public bool IsOverdue => !IsCompleted && DeadlineDate.HasValue && DeadlineDate.Value < DateTime.Now;

    public string RemainingTime(DateTime? at = null)
    {
        var now = at ?? DateTime.Now;

        if (IsCompleted) return "";

        if (DeadlineDate is { } dDate)
        {
            var diff = dDate - now;

            if (diff.TotalSeconds <= 0)
            {
                var abs = diff.Duration();
                var d = (int)abs.TotalDays;
                var h = abs.Hours;
                var m = abs.Minutes;
                return d > 0 ? $"已超期{d}天{h}小时"
                     : h > 0 ? $"已超期{h}小时{m}分钟"
                     : $"已超期{m}分钟";
            }

            var days = (int)diff.TotalDays;
            var hours = diff.Hours;
            var mins = diff.Minutes;
            return days > 0 ? $"剩余{days}天{hours}小时"
                 : hours > 0 ? $"剩余{hours}小时{mins}分钟"
                 : $"剩余{mins}分钟";
        }

        // Fallback: parse "剩余X天X小时" text
        if (Deadline != "暂无截止时间" && Deadline.Contains("剩余"))
        {
            return ParseRemainingText(Deadline);
        }

        return "";
    }

    private static string ParseRemainingText(string raw)
    {
        var d = 0; var h = 0; var m = 0;

        var dayMatch = System.Text.RegularExpressions.Regex.Match(raw, @"(\d+)天");
        if (dayMatch.Success) d = int.Parse(dayMatch.Groups[1].Value);

        var hourMatch = System.Text.RegularExpressions.Regex.Match(raw, @"(\d+)小时");
        if (hourMatch.Success) h = int.Parse(hourMatch.Groups[1].Value);

        var minMatch = System.Text.RegularExpressions.Regex.Match(raw, @"(\d+)分钟?");
        if (minMatch.Success) m = int.Parse(minMatch.Groups[1].Value);

        if (h >= 24) { d += h / 24; h %= 24; }

        return d > 0 ? $"剩余{d}天{h}小时"
             : h > 0 ? $"剩余{h}小时{m}分钟"
             : m > 0 ? $"剩余{m}分钟"
             : raw;
    }
}
