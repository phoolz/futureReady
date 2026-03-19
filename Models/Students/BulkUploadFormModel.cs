using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Apiary.Models.Students
{
    public class BulkUploadFormModel
    {
        [Required(ErrorMessage = "Please select a CSV file to upload")]
        [Display(Name = "CSV File")]
        public IFormFile? CsvFile { get; set; }
    }
}
