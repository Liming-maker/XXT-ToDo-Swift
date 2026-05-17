namespace HomeworkToDo.Core.Models;

public class AppSettings
{
    public string Phone { get; set; } = "";
    public string Password { get; set; } = "";
    public double RefreshInterval { get; set; } = 30; // Minutes
    public string NotificationThresholds { get; set; } = "60,1440";
    public string SelectedCourseIds { get; set; } = "";
    public bool AutoStart { get; set; }
}
