namespace Domain.Entities.Tenants
{
    public class Tenant
    {
        public string Id { get; set; }
        public required string Name { get; set; }
        public bool IsActive { get; set; }
    }
}
