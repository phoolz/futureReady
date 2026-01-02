using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FutureReady.Models.School;
using FutureReady.Models;

namespace FutureReady.Models.School
{
    public class CohortTeacher : TenantEntity
    {
        [Required]
        public Guid TeacherId { get; set; }

        [Required]
        public Guid CohortId { get; set; }

        // The academic year this assignment covers (redundant with SchoolCohort but convenient)
        public int? Year { get; set; }

        // Whether this teacher is a substitute for the cohort
        public bool IsSubstitute { get; set; }

        [MaxLength(100)]
        public string? Role { get; set; }

        [ForeignKey(nameof(TeacherId))]
        public Teacher? Teacher { get; set; }

        [ForeignKey(nameof(CohortId))]
        public Cohort? Cohort { get; set; }
    }
}
