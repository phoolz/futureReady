using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FutureReady.Models;

namespace FutureReady.Models.School
{
    public class Cohort : TenantEntity
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = null!;

        [Required]
        public Guid SchoolId { get; set; }

        [Required]
        [Display(Name = "Graduation Year")]
        public int GraduationYear { get; set; }

        [Required]
        [MaxLength(20)]
        [Display(Name = "Graduation Month")]
        public string GraduationMonth { get; set; } = null!;

        [ForeignKey(nameof(SchoolId))]
        public School? School { get; set; }

    }
}
