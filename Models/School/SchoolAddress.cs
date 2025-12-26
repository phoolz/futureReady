using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace FutureReady.Models.School
{
    public class SchoolAddress : TenantEntity
    {
        public Guid SchoolId { get; set; }

        [ForeignKey(nameof(SchoolId))]
        public virtual School? School { get; set; }

        public Guid AddressId { get; set; }

        [ForeignKey(nameof(AddressId))]
        public virtual FutureReady.Models.Address.Address? Address { get; set; }

        public string AddressType { get; set; } = string.Empty;
    }
}