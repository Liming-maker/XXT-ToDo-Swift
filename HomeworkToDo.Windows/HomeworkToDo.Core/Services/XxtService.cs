using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;
using HomeworkToDo.Core.Models;

namespace HomeworkToDo.Core.Services;

public partial class XxtService : IDisposable
{
    private const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/110.0.0.0 Safari/537.36";

    private readonly HttpClient _client;
    private readonly HttpClientHandler _handler;

    public XxtService()
    {
        _handler = new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = new CookieContainer()
        };
        _client = new HttpClient(_handler);
        _client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
    }

    public void Dispose()
    {
        _client.Dispose();
        _handler.Dispose();
    }

    // ============ Login ============

    public async Task<bool> LoginAsync(string phone, string password)
    {
        var phoneEnc = EncryptionService.Encrypt(phone);
        var passEnc = EncryptionService.Encrypt(password);
        if (phoneEnc == null || passEnc == null) return false;

        var formData = new Dictionary<string, string>
        {
            ["uname"] = phoneEnc,
            ["password"] = passEnc,
            ["t"] = "true",
            ["doubleFactorLogin"] = "0",
            ["independentId"] = "0"
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "http://passport2.chaoxing.com/fanyalogin");
        request.Headers.Add("Host", "passport2.chaoxing.com");
        request.Headers.Add("Origin", "http://passport2.chaoxing.com");
        request.Content = new FormUrlEncodedContent(formData);

        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        var json = System.Text.Json.JsonDocument.Parse(body);
        return json.RootElement.TryGetProperty("status", out var status) && status.GetBoolean();
    }

    // ============ Courses ============

    public async Task<List<Course>> FetchCoursesAsync()
    {
        var url = $"https://mooc2-ans.chaoxing.com/mooc2-ans/visit/courses/list?v={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}&start=0&size=500&catalogId=0&superstarClass=0";
        var html = await GetStringAsync(url);
        if (string.IsNullOrEmpty(html)) return new();

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var courses = new List<Course>();
        var items = doc.DocumentNode.SelectNodes("//li[contains(@class, 'course')]");
        if (items == null) return courses;

        foreach (var li in items)
        {
            var nameSpan = li.SelectSingleNode(".//span[contains(@class, 'course-name')]");
            var name = HttpUtility.HtmlDecode(nameSpan?.GetAttributeValue("title", "") ?? nameSpan?.InnerText.Trim() ?? "未知课程");

            var link = li.SelectSingleNode(".//a[contains(@class, 'color1')]");
            var courseUrl = link?.GetAttributeValue("href", "") ?? "";

            if (string.IsNullOrEmpty(courseUrl) || !courseUrl.Contains("courseid=")) continue;

            var teacherP = li.SelectSingleNode(".//p[contains(@class, 'line2')]");
            var teacher = HttpUtility.HtmlDecode(teacherP?.GetAttributeValue("title", "") ?? teacherP?.InnerText.Trim() ?? "未知老师");

            var courseId = courseUrl.Split("courseid=").Last().Split('&').First();

            if (!courses.Any(c => c.Id == courseId))
            {
                courses.Add(new Course
                {
                    Id = courseId,
                    Name = name,
                    Teacher = teacher.Replace("&nbsp;", " ").Replace(" ", " "),
                    Url = courseUrl
                });
            }
        }
        return courses;
    }

    public async Task<List<CourseFolder>> FetchCourseFoldersAsync()
    {
        var url = $"https://mooc2-ans.chaoxing.com/mooc2-ans/visit/courses/list?v={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}&start=0&size=500&catalogId=0&superstarClass=0";
        var html = await GetStringAsync(url);
        if (string.IsNullOrEmpty(html)) return new();

        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var folders = new List<CourseFolder>();

        // Parse folder definitions
        var folderNodes = doc.DocumentNode.SelectNodes("//ul[contains(@class, 'file-list')]/li[@fileid]");
        if (folderNodes != null)
        {
            foreach (var fl in folderNodes)
            {
                var folderId = fl.GetAttributeValue("fileid", "");
                if (string.IsNullOrEmpty(folderId)) continue;

                var folderName = HttpUtility.HtmlDecode(
                    fl.SelectSingleNode(".//h3[contains(@class, 'file-name')]")?.InnerText.Trim() ?? "未知文件夹");

                var courseNodes = doc.DocumentNode.SelectNodes(
                    $"//li[contains(@class, 'course') and contains(@class, 'catalog_{folderId}')]");
                var courses = ParseCourseElements(courseNodes);

                folders.Add(new CourseFolder { Id = folderId, Name = folderName, Courses = courses });
            }
        }

        // Root courses
        var allCourseNodes = doc.DocumentNode.SelectNodes("//li[contains(@class, 'course')]");
        var allParsed = ParseCourseElements(allCourseNodes);
        var folderCourseIds = folders.SelectMany(f => f.Courses).Select(c => c.Id).ToHashSet();
        var rootCourses = allParsed.Where(c => !folderCourseIds.Contains(c.Id)).ToList();

        if (rootCourses.Count > 0)
            folders.Insert(0, new CourseFolder { Id = "root", Name = "未分类课程", Courses = rootCourses });

        return folders;
    }

    private static List<Course> ParseCourseElements(HtmlNodeCollection? elements)
    {
        var courses = new List<Course>();
        if (elements == null) return courses;

        foreach (var el in elements)
        {
            if (el.HasClass("folder") || el.GetAttributeValue("fileid", "") != "")
                continue;

            var nameSpan = el.SelectSingleNode(".//span[contains(@class, 'course-name')]");
            var name = HttpUtility.HtmlDecode(nameSpan?.GetAttributeValue("title", "") ?? nameSpan?.InnerText.Trim() ?? "");
            if (string.IsNullOrEmpty(name) || name == "未知课程") continue;

            var link = el.SelectSingleNode(".//div[contains(@class, 'course-info')]//a | .//div[contains(@class, 'course-cover')]//a");
            var courseUrl = link?.GetAttributeValue("href", "") ?? "";
            if (string.IsNullOrEmpty(courseUrl) || !courseUrl.Contains("courseid=")) continue;

            var teacherP = el.SelectSingleNode(".//p[contains(@class, 'line2')]");
            var teacher = HttpUtility.HtmlDecode(teacherP?.GetAttributeValue("title", "") ?? teacherP?.InnerText.Trim() ?? "未知老师");

            var courseId = courseUrl.Split("courseid=").Last().Split('&').First();

            if (!courses.Any(c => c.Id == courseId))
            {
                courses.Add(new Course
                {
                    Id = courseId,
                    Name = name,
                    Teacher = teacher.Replace("&nbsp;", " ").Replace(" ", " "),
                    Url = courseUrl
                });
            }
        }
        return courses;
    }

    // ============ Homework ============

    public async Task<List<Homework>> FetchAllHomeworkAsync()
    {
        var html = await GetStringAsync("https://mooc1.chaoxing.com/work/stu-work");
        if (string.IsNullOrEmpty(html)) return new();

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var items = doc.DocumentNode.SelectNodes("//li[@onclick[starts-with(.,'goTask')]]")
                 ?? doc.DocumentNode.SelectNodes("//ul[contains(@class, 'nav')]/li");
        if (items == null) return new();

        var tempData = new List<(string Id, string Name, string Status, string CourseName, string Deadline, string CourseId, string DetailUrl)>();
        var needsCourseRepair = false;

        foreach (var li in items)
        {
            var dataUrl = li.GetAttributeValue("data", "");
            if (string.IsNullOrEmpty(dataUrl)) continue;

            var div = li.SelectSingleNode(".//div[@role='option']");
            if (div == null) continue;

            var name = HttpUtility.HtmlDecode(
                div.SelectSingleNode(".//p")?.InnerText.Trim() ?? "未知作业");

            var status = "";
            var statusSpan = div.SelectSingleNode(".//span[contains(@class, 'status')]");
            if (statusSpan != null)
                status = statusSpan.InnerText.Trim();

            // Course name
            var courseName = "未知课程";
            foreach (var span in div.SelectNodes(".//span") ?? Enumerable.Empty<HtmlNode>())
            {
                var text = HttpUtility.HtmlDecode(span.InnerText);
                if (text.Contains("《"))
                {
                    courseName = text.Replace("《", "").Replace("》", "").Trim();
                    break;
                }
            }
            if (courseName.Contains("...") || courseName.EndsWith("…"))
                needsCourseRepair = true;

            // Deadline text
            var deadline = "暂无截止时间";
            var frSpan = div.SelectSingleNode(".//span[contains(@class, 'fr')]");
            if (frSpan != null)
                deadline = frSpan.InnerText.Trim();

            // Parse URL
            var hwId = Guid.NewGuid().ToString();
            var courseId = "";
            var detailUrl = "";

            var parsed = new Uri(dataUrl.Contains("://") ? dataUrl : "https://mooc1.chaoxing.com" + (dataUrl.StartsWith('/') ? "" : "/") + dataUrl);
            var qs = HttpUtility.ParseQueryString(parsed.Query);
            hwId = qs["taskrefId"] ?? hwId;
            courseId = qs["courseid"] ?? qs["courseId"] ?? "";
            detailUrl = dataUrl;

            tempData.Add((hwId, name, status, courseName, deadline, courseId, detailUrl));
        }

        return await BuildHomeworkResult(tempData, needsCourseRepair);
    }

    private async Task<List<Homework>> BuildHomeworkResult(
        List<(string Id, string Name, string Status, string CourseName, string Deadline, string CourseId, string DetailUrl)> tempData,
        bool needsCourseRepair)
    {
        var courseMap = new Dictionary<string, string>();
        var fullCourseList = new List<Course>();

        if (needsCourseRepair)
        {
            fullCourseList = await FetchCoursesAsync();
            foreach (var c in fullCourseList)
            {
                var parsed = new Uri(c.Url);
                var qs = HttpUtility.ParseQueryString(parsed.Query);
                var realId = qs["courseid"] ?? "";
                if (!string.IsNullOrEmpty(realId))
                    courseMap[realId] = c.Name;
            }
        }

        var savedHomeworks = new StorageService().LoadHomeworks();
        var resolvedNames = new Dictionary<string, string>();
        var unfinished = new[] { "未交", "未完成", "未提交", "待互评" };
        var result = new List<Homework>();

        foreach (var item in tempData)
        {
            var finalCourseName = courseMap.GetValueOrDefault(item.CourseId)
                               ?? resolvedNames.GetValueOrDefault(item.CourseId)
                               ?? item.CourseName;

            if (finalCourseName == item.CourseName && needsCourseRepair)
            {
                var cleanName = item.CourseName.Replace("...", "").Replace("…", "").Trim();
                if (cleanName.Length < item.CourseName.Length)
                {
                    var match = fullCourseList.FirstOrDefault(c => c.Name.StartsWith(cleanName));
                    if (match != null)
                    {
                        finalCourseName = match.Name;
                        resolvedNames[item.CourseId] = match.Name;
                    }
                    else if (!string.IsNullOrEmpty(item.DetailUrl))
                    {
                        var resolved = await FetchCourseNameFromDetailAsync(item.DetailUrl);
                        if (!string.IsNullOrEmpty(resolved))
                        {
                            finalCourseName = resolved;
                            resolvedNames[item.CourseId] = resolved;
                        }
                    }
                }
            }

            var finalDeadline = item.Deadline;
            var isPrecise = false;

            var saved = savedHomeworks.FirstOrDefault(h => h.Id == item.Id);
            if (saved != null && saved.Status == item.Status && saved.IsPreciseDeadline)
            {
                finalDeadline = saved.Deadline;
                isPrecise = true;
            }

            if (!isPrecise && unfinished.Any(s => item.Status.Contains(s)) && !string.IsNullOrEmpty(item.DetailUrl))
            {
                var exact = await FetchDeadlineAsync(item.DetailUrl);
                if (exact != "暂无截止时间")
                {
                    finalDeadline = exact;
                    isPrecise = true;
                }
            }

            // Fallback: convert "剩余X小时" to approximate date
            if (finalDeadline.Contains("剩余"))
                finalDeadline = ConvertRemainingTextToDate(finalDeadline);

            result.Add(new Homework
            {
                Id = item.Id,
                Name = item.Name,
                Status = item.Status,
                CourseName = finalCourseName,
                CourseId = item.CourseId,
                Deadline = finalDeadline,
                DetailUrl = item.DetailUrl,
                IsPreciseDeadline = isPrecise
            });
        }

        return result;
    }

    public async Task<Homework> UpdateHomeworkDeadlineAsync(Homework hw)
    {
        if (string.IsNullOrEmpty(hw.DetailUrl)) return hw;
        var newDeadline = await FetchDeadlineAsync(hw.DetailUrl);
        var isPrecise = hw.IsPreciseDeadline;

        if (newDeadline != "暂无截止时间")
            isPrecise = true;
        else if (hw.Deadline != "暂无截止时间")
            newDeadline = hw.Deadline;

        return new Homework
        {
            Id = hw.Id, Name = hw.Name, Status = hw.Status,
            CourseName = hw.CourseName, CourseId = hw.CourseId,
            Deadline = newDeadline, DetailUrl = hw.DetailUrl,
            IsPreciseDeadline = isPrecise
        };
    }

    private async Task<string> FetchDeadlineAsync(string workUrl)
    {
        var html = await GetStringAsync(MakeAbsolute(workUrl));
        if (string.IsNullOrEmpty(html)) return "暂无截止时间";

        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var currentYear = DateTime.Now.Year;

        // Check hidden timestamp input
        var endTimeInput = doc.DocumentNode.SelectSingleNode("//input[@id='endTime']");
        if (endTimeInput != null)
        {
            var val = endTimeInput.GetAttributeValue("value", "");
            if (double.TryParse(val, out var ts) && ts > 0)
            {
                var date = DateTimeOffset.FromUnixTimeMilliseconds((long)ts).DateTime;
                return date.ToString("yyyy-MM-dd HH:mm");
            }
        }

        var allText = doc.DocumentNode.InnerText;

        // Patterns with year
        var patterns = new[]
        {
            @"至\s*(\d{4}[-年]\d{1,2}[-月]\d{1,2}[日]?\s+\d{1,2}:\d{2})",
            @"截止时间[:：]?\s*(\d{4}[-年]\d{1,2}[-月]\d{1,2}[日]?\s+\d{1,2}:\d{2})",
            @"截止[:：]?\s*(\d{4}[-年]\d{1,2}[-月]\d{1,2}[日]?\s+\d{1,2}:\d{2})",
            @"结束时间[:：]?\s*(\d{4}[-年]\d{1,2}[-月]\d{1,2}[日]?\s+\d{1,2}:\d{2})",
            @"(\d{4}[-年]\d{1,2}[-月]\d{1,2}[日]?\s+\d{1,2}:\d{2})\s*截止",
            @"至\s*(\d{1,2}[-月]\d{1,2}[日]?\s+\d{1,2}:\d{2})",
            @"截止时间[:：]?\s*(\d{1,2}[-月]\d{1,2}[日]?\s+\d{1,2}:\d{2})",
            @"截止[:：]?\s*(\d{1,2}[-月]\d{1,2}[日]?\s+\d{1,2}:\d{2})",
            @"结束时间[:：]?\s*(\d{1,2}[-月]\d{1,2}[日]?\s+\d{1,2}:\d{2})"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(allText, pattern);
            if (match.Success)
            {
                var d = match.Groups[1].Value
                    .Replace("年", "-").Replace("月", "-").Replace("日", "").Trim();
                if (!d.StartsWith("20"))
                    d = $"{currentYear}-{d}";
                return d;
            }
        }

        return "暂无截止时间";
    }

    // ============ Exams ============

    public async Task<List<Exam>> FetchAllExamsAsync()
    {
        var html = await GetStringAsync("https://mooc1-api.chaoxing.com/exam-ans/exam/phone/examcode");
        if (string.IsNullOrEmpty(html)) return new();

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var items = doc.DocumentNode.SelectNodes("//li[@onclick[starts-with(.,'goTask')] or @data]");
        if (items == null) return new();

        var tempData = new List<(string Id, string Name, string Status, string CourseName, string Deadline, string CourseId, string DetailUrl)>();
        var savedExams = new StorageService().LoadExams();
        var result = new List<Exam>();

        foreach (var li in items)
        {
            var dataUrl = li.GetAttributeValue("data", "");
            if (string.IsNullOrEmpty(dataUrl)) continue;

            var name = HttpUtility.HtmlDecode(
                li.SelectSingleNode(".//dt")?.InnerText.Trim() ?? "未知考试");

            var status = HttpUtility.HtmlDecode(
                li.SelectSingleNode(".//span[contains(@class, 'ks_state')]")?.InnerText.Trim() ?? "");

            if (string.IsNullOrEmpty(status))
            {
                var dl = li.SelectSingleNode(".//dl[@aria-label]");
                if (dl != null)
                {
                    var aria = dl.GetAttributeValue("aria-label", "");
                    var m = Regex.Match(aria, @"考试状态：([^；;]+)");
                    if (m.Success) status = m.Groups[1].Value.Trim();
                }
            }

            var deadline = "暂无截止时间";
            var timeSpan = li.SelectSingleNode(".//span[contains(@class, 'fr') or contains(@class, 'time')]");
            if (timeSpan != null) deadline = timeSpan.InnerText.Trim();

            var cleanUrl = dataUrl.Replace("&amp;", "&");
            var parsed = new Uri(cleanUrl.Contains("://") ? cleanUrl : "https://mooc1.chaoxing.com" + (cleanUrl.StartsWith('/') ? "" : "/") + cleanUrl);
            var qs = HttpUtility.ParseQueryString(parsed.Query);
            var examId = qs["taskrefId"] ?? qs["examId"] ?? Guid.NewGuid().ToString();
            var courseId = qs["courseid"] ?? qs["courseId"] ?? "";

            tempData.Add((examId, name, status, "未知课程", deadline, courseId, cleanUrl));
        }

        // Build course map
        var courseMap = new Dictionary<string, string>();
        var courses = await FetchCoursesAsync();
        foreach (var c in courses)
        {
            var p = new Uri(c.Url);
            var q = HttpUtility.ParseQueryString(p.Query);
            var realId = q["courseid"] ?? "";
            if (!string.IsNullOrEmpty(realId)) courseMap[realId] = c.Name;
        }

        var unfinishedStatuses = new[] { "待考试", "进行中", "未完成", "未开始" };

        foreach (var item in tempData)
        {
            var finalCourseName = courseMap.GetValueOrDefault(item.CourseId) ?? item.CourseName;
            var finalDeadline = item.Deadline;
            var isPrecise = false;

            var saved = savedExams.FirstOrDefault(e => e.Id == item.Id);
            if (saved != null && saved.Status == item.Status && saved.IsPreciseDeadline)
            {
                finalDeadline = saved.Deadline;
                isPrecise = true;
            }

            if (!isPrecise && unfinishedStatuses.Any(s => item.Status.Contains(s)))
            {
                var exact = await FetchExamDeadlineAsync(item.DetailUrl);
                if (exact != "暂无截止时间")
                {
                    finalDeadline = exact;
                    isPrecise = true;
                }
            }

            if (finalDeadline.Contains("剩余"))
                finalDeadline = ConvertRemainingTextToDate(finalDeadline);

            result.Add(new Exam
            {
                Id = item.Id, Name = item.Name, Status = item.Status,
                CourseName = finalCourseName, CourseId = item.CourseId,
                Deadline = finalDeadline, DetailUrl = item.DetailUrl,
                IsPreciseDeadline = isPrecise
            });
        }

        return result;
    }

    public async Task<Exam> UpdateExamDeadlineAsync(Exam exam)
    {
        if (string.IsNullOrEmpty(exam.DetailUrl)) return exam;
        var newDeadline = await FetchExamDeadlineAsync(exam.DetailUrl);
        var isPrecise = exam.IsPreciseDeadline;
        var deadline = newDeadline;

        if (newDeadline != "暂无截止时间")
            isPrecise = true;
        else if (exam.Deadline != "暂无截止时间")
            deadline = exam.Deadline;

        return new Exam
        {
            Id = exam.Id, Name = exam.Name, Status = exam.Status,
            CourseName = exam.CourseName, CourseId = exam.CourseId,
            Deadline = deadline, DetailUrl = exam.DetailUrl,
            IsPreciseDeadline = isPrecise
        };
    }

    private async Task<string> FetchExamDeadlineAsync(string detailUrl)
    {
        var html = await GetStringAsync(MakeAbsolute(detailUrl));
        if (string.IsNullOrEmpty(html)) return "暂无截止时间";

        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var currentYear = DateTime.Now.Year;
        var allText = doc.DocumentNode.InnerText;

        var patterns = new[]
        {
            @"截止时间[:：]?\s*(\d{4}[-年]\d{1,2}[-月]\d{1,2}[日]?\s+\d{1,2}:\d{2})",
            @"结束时间[:：]?\s*(\d{4}[-年]\d{1,2}[-月]\d{1,2}[日]?\s+\d{1,2}:\d{2})",
            @"至\s*(\d{1,2}[-月]\d{1,2}[日]?\s+\d{1,2}:\d{2})",
            @"截止[:：]?\s*(\d{1,2}[-月]\d{1,2}[日]?\s+\d{1,2}:\d{2})"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(allText, pattern);
            if (match.Success)
            {
                var d = match.Groups[1].Value
                    .Replace("年", "-").Replace("月", "-").Replace("日", "").Trim();
                if (!d.StartsWith("20")) d = $"{currentYear}-{d}";
                return d;
            }
        }

        return "暂无截止时间";
    }

    // ============ Helpers ============

    private async Task<string?> GetStringAsync(string url)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Referrer = new Uri("https://mooc2-ans.chaoxing.com/");
            var response = await _client.SendAsync(request);
            return await response.Content.ReadAsStringAsync();
        }
        catch
        {
            return null;
        }
    }

    private static string MakeAbsolute(string url)
    {
        if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return url;
        return "https://mooc1.chaoxing.com" + (url.StartsWith('/') ? "" : "/") + url;
    }

    private async Task<string?> FetchCourseNameFromDetailAsync(string workUrl)
    {
        var html = await GetStringAsync(MakeAbsolute(workUrl));
        if (string.IsNullOrEmpty(html)) return null;

        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var h3 = doc.DocumentNode.SelectSingleNode("//h3[contains(@class, 'f14')]");
        if (h3 != null)
        {
            var text = HttpUtility.HtmlDecode(h3.InnerText);
            var m = Regex.Match(text, @"课程：([^　班级教师]+)");
            if (m.Success) return m.Groups[1].Value.Trim();
        }
        return null;
    }

    internal static string ConvertRemainingTextToDate(string text)
    {
        var daysM = Regex.Match(text, @"(\d+)天");
        var hoursM = Regex.Match(text, @"(\d+)小时");
        var minsM = Regex.Match(text, @"(\d+)分钟?");

        var totalSeconds = 0.0;
        if (daysM.Success) totalSeconds += double.Parse(daysM.Groups[1].Value) * 86400;
        if (hoursM.Success) totalSeconds += double.Parse(hoursM.Groups[1].Value) * 3600;
        if (minsM.Success) totalSeconds += double.Parse(minsM.Groups[1].Value) * 60;

        if (totalSeconds > 0)
        {
            var expireDate = DateTime.Now.AddSeconds(totalSeconds);
            return expireDate.ToString("yyyy-MM-dd HH:mm");
        }
        return text;
    }
}
