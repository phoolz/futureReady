using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FutureReady.Models.School
{
    public class LogbookEntry : TenantEntity
    {
        [Required]
        public Guid PlacementId { get; set; }

        [ForeignKey(nameof(PlacementId))]
        public Placement? Placement { get; set; }

        [Required]
        [Display(Name = "Date")]
        public DateOnly Date { get; set; }

        [MaxLength(10)]
        [Display(Name = "Start Time")]
        public string? StartTime { get; set; }

        [MaxLength(10)]
        [Display(Name = "Lunch Start Time")]
        public string? LunchStartTime { get; set; }

        [MaxLength(10)]
        [Display(Name = "Lunch End Time")]
        public string? LunchEndTime { get; set; }

        [MaxLength(10)]
        [Display(Name = "Finish Time")]
        public string? FinishTime { get; set; }

        [Display(Name = "Total Hours Worked")]
        public decimal TotalHoursWorked { get; set; }

        [Display(Name = "Cumulative Hours")]
        public decimal CumulativeHours { get; set; }

        [Display(Name = "Supervisor Verified")]
        public bool SupervisorVerified { get; set; }

        [Display(Name = "Supervisor Verified At")]
        public DateTimeOffset? SupervisorVerifiedAt { get; set; }
    }
}
