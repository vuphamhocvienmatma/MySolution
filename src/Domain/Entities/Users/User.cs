using Domain.Common;
namespace Domain.Entities.Users;

public class User : BaseAuditableEntity, IMustHaveTenant
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string TenantId { get; set; }
    public int GetAge()
    {
        var today = DateTime.Today;
        var age = today.Year - DateOfBirth.Year;
        if (DateOfBirth.Date > today.AddYears(-age))
        {
            age--;
        }
        return age;
    }
}