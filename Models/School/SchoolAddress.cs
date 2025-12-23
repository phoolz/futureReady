namespace DefaultNamespace;

public class FutureReady.Models.School
{
    public class SchoolAddress : TenantEntity
    {
        public Guid SchoolId { get; set; }
        [ForeignKey(nameof(SchoolId))]
        public virtual School? School { get; set; }

        public Guid AddressId { get; set; }
        [ForeignKey(nameof(AddressId))]
        public virtual Address? Address { get; set; }

        
        public string AddressType { get; set; }
    }
}