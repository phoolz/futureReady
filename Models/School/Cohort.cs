using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FutureReady.Models;

namespace FutureReady.Models.School
{
    public class Cohort : TenantEntity
    {
        [Required]
        public Guid SchoolId { get; set; }

        [Required]
        public int GraduationYear { get; set; }

        [Required]
        public int GraduationMonth { get; set; }

        [ForeignKey(nameof(SchoolId))]
        public School? School { get; set; }
    }
}

