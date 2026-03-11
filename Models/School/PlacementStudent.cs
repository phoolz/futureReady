using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Apiary.Models.School
{
    public class PlacementStudent : TenantEntity
    {
        [Required]
        public Guid PlacementId { get; set; }

        [Required]
        public Guid StudentId { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "pending_parent";  // 'pending_parent', 'confirmed', 'withdrawn'

        [Display(Name = "Parent Submitted At")]
        public DateTime? ParentSubmittedAt { get; set; }

        // Navigation properties
        public Placement? Placement { get; set; }
        public Student? Student { get; set; }

        // Logbook collections (per-student records)
        public ICollection<LogbookEntry> LogbookEntries { get; set; } = [];
        public ICollection<LogbookTask> LogbookTasks { get; set; } = [];
        public ICollection<LogbookEvaluation> LogbookEvaluations { get; set; } = [];
    }
}
