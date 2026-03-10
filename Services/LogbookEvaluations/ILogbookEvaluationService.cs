using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Apiary.Models.School;

namespace Apiary.Services.LogbookEvaluations
{
    public interface ILogbookEvaluationService
    {
        Task<LogbookEvaluation?> GetByIdAsync(Guid id, Guid? tenantId = null);
        Task<List<LogbookEvaluation>> GetByPlacementStudentIdAsync(Guid placementStudentId, Guid? tenantId = null);
        Task CreateAsync(LogbookEvaluation evaluation, Guid? tenantId = null);
        Task UpdateAsync(LogbookEvaluation evaluation, byte[]? rowVersion = null, Guid? tenantId = null);
        Task DeleteAsync(Guid id, Guid? tenantId = null);
    }
}
