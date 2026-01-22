using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FutureReady.Models.School
{
    public class LogbookTask : TenantEntity
    {
        [Required]
        public Guid PlacementId { get; set; }

        [ForeignKey(nameof(PlacementId))]
        public Placement? Placement { get; set; }

        [Required]
        [MaxLength(2000)]
        [Display(Name = "Description")]
        public string Description { get; set; } = null!;

        [Required]
        [Display(Name = "Date Performed")]
        public DateOnly DatePerformed { get; set; }
    }
}
