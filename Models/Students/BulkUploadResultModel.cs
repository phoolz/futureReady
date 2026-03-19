using System.Collections.Generic;

namespace Apiary.Models.Students
{
    public class BulkUploadResultModel
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int SkippedCount { get; set; }
        public int ErrorCount { get; set; }
        public List<BulkUploadRowResult> RowResults { get; set; } = new();
    }

    public class BulkUploadRowResult
    {
        public int LineNumber { get; set; }
        public string Email { get; set; } = string.Empty;
        public BulkUploadRowStatus Status { get; set; }
        public string? Message { get; set; }
    }

    public enum BulkUploadRowStatus
    {
        Success,
        Skipped,
        Error
    }
}
