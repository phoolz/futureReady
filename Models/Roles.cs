namespace FutureReady.Models;

public static class Roles
{
    public const string SiteAdmin = "Site Admin";
    public const string Teacher = "Teacher";
    public const string Student = "Student";

    public static string[] AllRoles => new[] { SiteAdmin, Teacher, Student };
    public static string TeacherOrStudent => $"{Teacher},{Student}";
    public static string AnyRole => $"{SiteAdmin},{Teacher},{Student}";
}
