namespace HomeworkToDo.Core.Models;

public class SelectableFolder
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public List<SelectableCourse> Courses { get; init; } = new();
}
