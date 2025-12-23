using System;
using System.ComponentModel.DataAnnotations;

namespace FutureReady.Models
{
    public abstract class TenantEntity : BaseEntity
    {
        [Required]
        public Guid TenantId { get; set; }
    }
}