using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Apiary.Models.School
{
    public class LogbookTask : TenantEntity
    {
        [Required]
        public Guid PlacementStudentId { get; set; }

        [ForeignKey(nameof(PlacementStudentId))]
        public PlacementStudent? PlacementStudent { get; set; }

        [Required]
        [MaxLength(2000)]
        [Display(Name = "Description")]
        public string Description { get; set; } = null!;

        [Required]
        [Display(Name = "Date Performed")]
        public DateOnly DatePerformed { get; set; }
    }
}
