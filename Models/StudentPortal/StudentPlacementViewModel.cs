using System;

namespace Apiary.Models.StudentPortal
{
    public class StudentPlacementViewModel
    {
        public Guid PlacementId { get; set; }
        public int? Year { get; set; }
        public string Status { get; set; } = "draft";

        // Company info
        public string? CompanyName { get; set; }
        public string? CompanyIndustry { get; set; }

        // Supervisor info
        public string? SupervisorFullName { get; set; }
        public string? SupervisorJobTitle { get; set; }
        public string? SupervisorEmail { get; set; }
        public string? SupervisorPhone { get; set; }

        // Work details
        public string? WorkStartTime { get; set; }
        public string? WorkEndTime { get; set; }
        public string? DressRequirement { get; set; }

        // Hours
        public decimal TotalHoursWorked { get; set; }

        // Computed properties
        public bool IsConfirmed => Status == "confirmed";

        public string StatusDisplayClass => Status switch
        {
            "confirmed" => "bg-success",
            "pending_employer" => "bg-warning text-dark",
            "pending_parent" => "bg-info",
            _ => "bg-secondary"
        };

        public string StatusDisplayText => Status switch
        {
            "confirmed" => "Confirmed",
            "pending_employer" => "Pending Employer",
            "pending_parent" => "Pending Parent",
            "draft" => "Draft",
            _ => Status
        };
    }
}
