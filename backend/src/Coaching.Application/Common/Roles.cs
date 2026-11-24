namespace Coaching.Application.Common;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Student = "Student";

    public static readonly IReadOnlyList<string> All = [Admin, Student];
}
