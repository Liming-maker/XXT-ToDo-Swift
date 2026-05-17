namespace HomeworkToDo.Core.Models;

public class CourseFolder
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public List<Course> Courses { get; set; } = new();
}
