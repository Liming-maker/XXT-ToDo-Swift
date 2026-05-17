namespace HomeworkToDo.Core.Models;

public class Course : IEquatable<Course>
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Teacher { get; init; } = "";
    public string Url { get; init; } = "";

    public bool Equals(Course? other) => other?.Id == Id;
    public override bool Equals(object? obj) => obj is Course c && Equals(c);
    public override int GetHashCode() => Id.GetHashCode();
}
