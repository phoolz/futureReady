using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FutureReady.Models.School
{
    public class StudentMedicalCondition : TenantEntity
    {
        [Required]
        public Guid StudentId { get; set; }

        [ForeignKey(nameof(StudentId))]
        public Student? Student { get; set; }

        [Required]
        [MaxLength(50)]
        [Display(Name = "Condition Type")]
        public string ConditionType { get; set; } = null!;

        [MaxLength(2000)]
        public string? Details { get; set; }
    }

    public static class MedicalConditionTypes
    {
        public const string Asthma = "asthma";
        public const string Diabetes = "diabetes";
        public const string Epilepsy = "epilepsy";
        public const string Allergies = "allergies";
        public const string LearningDifficulties = "learning_difficulties";
        public const string Medication = "medication";
        public const string Other = "other";

        public static readonly string[] All = new[]
        {
            Asthma, Diabetes, Epilepsy, Allergies, LearningDifficulties, Medication, Other
        };

        public static string GetDisplayName(string type) => type switch
        {
            Asthma => "Asthma",
            Diabetes => "Diabetes",
            Epilepsy => "Epilepsy",
            Allergies => "Allergies",
            LearningDifficulties => "Learning Difficulties",
            Medication => "Medication",
            Other => "Other",
            _ => type
        };
    }
}
