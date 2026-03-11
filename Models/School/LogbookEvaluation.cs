using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Apiary.Models.School.Enums;

namespace Apiary.Models.School
{
    public class LogbookEvaluation : TenantEntity
    {
        [Required]
        public Guid PlacementStudentId { get; set; }

        [ForeignKey(nameof(PlacementStudentId))]
        public PlacementStudent? PlacementStudent { get; set; }

        // Performance ratings (8 fields using PerformanceRating enum)
        [Required]
        [Display(Name = "Attendance & Punctuality")]
        public PerformanceRating AttendancePunctuality { get; set; }

        [Required]
        [Display(Name = "Appearance")]
        public PerformanceRating Appearance { get; set; }

        [Required]
        [Display(Name = "Communication Skills")]
        public PerformanceRating CommunicationSkills { get; set; }

        [Required]
        [Display(Name = "Initiative")]
        public PerformanceRating Initiative { get; set; }

        [Required]
        [Display(Name = "Work Quality")]
        public PerformanceRating WorkQuality { get; set; }

        [Required]
        [Display(Name = "Teamwork")]
        public PerformanceRating Teamwork { get; set; }

        [Required]
        [Display(Name = "Safety Awareness")]
        public PerformanceRating SafetyAwareness { get; set; }

        [Required]
        [Display(Name = "Overall Performance")]
        public PerformanceRating OverallPerformance { get; set; }

        // Supervisor information
        [MaxLength(200)]
        [Display(Name = "Supervisor Name")]
        public string? SupervisorName { get; set; }

        [Display(Name = "Comments")]
        public string? Comments { get; set; }

        [Display(Name = "Supervisor Signed At")]
        public DateTimeOffset? SupervisorSignedAt { get; set; }
    }
}
