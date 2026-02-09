using System;
using System.Collections.Generic;
using System.Linq;

namespace FutureReady.Models.StudentPortal
{
    public class StudentPortalViewModel
    {
        public Guid StudentId { get; set; }
        public string StudentDisplayName { get; set; } = string.Empty;
        public string StudentFullName { get; set; } = string.Empty;

        public List<StudentPlacementViewModel> ConfirmedPlacements { get; set; } = new();
        public List<StudentPlacementViewModel> PendingPlacements { get; set; } = new();

        // Computed properties
        public bool HasConfirmedPlacements => ConfirmedPlacements.Any();
        public bool HasPendingPlacements => PendingPlacements.Any();
        public bool HasAnyPlacements => HasConfirmedPlacements || HasPendingPlacements;
    }
}
