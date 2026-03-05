namespace Apiary.Models;

public static class Roles
{
    public const string SiteAdmin = "Site Admin";
    public const string Teacher = "Teacher";
    public const string Student = "Student";

    public static string[] AllRoles => new[] { SiteAdmin, Teacher, Student };
    public const string TeacherOrStudent = Teacher + "," + Student;
    public const string AnyRole = SiteAdmin + "," + Teacher + "," + Student;
}
