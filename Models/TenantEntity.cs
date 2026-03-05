using System;
using System.ComponentModel.DataAnnotations;

namespace Apiary.Models
{
    public abstract class TenantEntity : BaseEntity
    {
        [Required]
        public Guid TenantId { get; set; }
    }
}