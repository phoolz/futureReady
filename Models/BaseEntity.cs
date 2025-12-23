using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FutureReady.Models
{
    public abstract class BaseEntity
    {
        [Key]
        public Guid Id { get; set; }

        [MaxLength(256)]
        public string? CreatedBy { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        [MaxLength(256)]
        public string? UpdatedBy { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }

        [MaxLength(256)]
        public string? DeletedBy { get; set; }

        public DateTimeOffset? DeletedAt { get; set; }

        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}

