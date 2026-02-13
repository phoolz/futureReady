using System;
using System.Collections.Generic;

namespace FutureReady.Models.LogbookEntries
{
    public class LogbookEntriesListViewModel
    {
        public Guid PlacementId { get; set; }
        public string PlacementInfo { get; set; } = string.Empty;
        public decimal TotalHours { get; set; }
        public int VerifiedCount { get; set; }
        public int TotalEntries { get; set; }
        public List<LogbookEntryViewModel> Entries { get; set; } = new();
    }

    public class LogbookEntryViewModel
    {
        public Guid Id { get; set; }
        public DateOnly Date { get; set; }
        public string? StartTime { get; set; }
        public string? FinishTime { get; set; }
        public decimal TotalHoursWorked { get; set; }
        public decimal CumulativeHours { get; set; }
        public bool SupervisorVerified { get; set; }
    }
}
