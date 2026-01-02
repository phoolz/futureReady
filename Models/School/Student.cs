using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FutureReady.Models;

namespace FutureReady.Models.School
{
    public class Student : TenantEntity
    {
        [Required]
        public Guid CohortId { get; set; }

        [ForeignKey(nameof(CohortId))]
        public Cohort? Cohort { get; set; }

        [Required]
        [MaxLength(100)]
        public string MedicareNumber { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string StudentType { get; set; } = null!;
    }
}

