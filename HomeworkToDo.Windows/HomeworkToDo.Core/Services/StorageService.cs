using System.Text.Json;

namespace HomeworkToDo.Core.Services;

public class StorageService
{
    private readonly string _basePath;

    public StorageService(string? basePath = null)
    {
        _basePath = basePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HomeworkToDo");
        Directory.CreateDirectory(_basePath);
    }

    // === Settings ===

    public void SaveSettings(Models.AppSettings settings)
    {
        var path = Path.Combine(_basePath, "settings.json");
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(path, json);
    }

    public Models.AppSettings LoadSettings()
    {
        var path = Path.Combine(_basePath, "settings.json");
        if (!File.Exists(path)) return new Models.AppSettings();
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Models.AppSettings>(json, JsonOptions) ?? new Models.AppSettings();
    }

    // === Homework ===

    public void SaveHomeworks(List<Models.Homework> homeworks)
    {
        var path = Path.Combine(_basePath, "saved_homeworks.json");
        var json = JsonSerializer.Serialize(homeworks, JsonOptions);
        File.WriteAllText(path, json);
    }

    public List<Models.Homework> LoadHomeworks()
    {
        var path = Path.Combine(_basePath, "saved_homeworks.json");
        if (!File.Exists(path)) return new();
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<Models.Homework>>(json, JsonOptions) ?? new();
    }

    // === Exams ===

    public void SaveExams(List<Models.Exam> exams)
    {
        var path = Path.Combine(_basePath, "saved_exams.json");
        var json = JsonSerializer.Serialize(exams, JsonOptions);
        File.WriteAllText(path, json);
    }

    public List<Models.Exam> LoadExams()
    {
        var path = Path.Combine(_basePath, "saved_exams.json");
        if (!File.Exists(path)) return new();
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<Models.Exam>>(json, JsonOptions) ?? new();
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
