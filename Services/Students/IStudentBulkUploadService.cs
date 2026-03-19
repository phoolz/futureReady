using System.IO;
using System.Threading.Tasks;
using Apiary.Models.Students;

namespace Apiary.Services.Students
{
    public interface IStudentBulkUploadService
    {
        Task<BulkUploadResultModel> ProcessCsvAsync(Stream csvStream, Guid? tenantId = null);
    }
}
