using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FutureReady.Models.School
{
    public class StudentWorkHistory : TenantEntity
    {
        [Required]
        public Guid StudentId { get; set; }

        [ForeignKey(nameof(StudentId))]
        public Student? Student { get; set; }

        // JSON string fields for storing arrays of work history data
        [Display(Name = "Current Courses")]
        public string? CurrentCourses { get; set; }

        [Display(Name = "VET Qualifications")]
        public string? VetQualifications { get; set; }

        [Display(Name = "Certificates")]
        public string? Certificates { get; set; }

        [Display(Name = "Part-Time Employment")]
        public string? PartTimeEmployment { get; set; }

        [Display(Name = "Community Service")]
        public string? CommunityService { get; set; }
    }
}
