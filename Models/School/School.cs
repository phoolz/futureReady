using System.ComponentModel.DataAnnotations;

namespace FutureReady.Models.School
{
    public class School : BaseEntity
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string TenantKey { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string Timezone { get; set; } = null!;
    }
}
