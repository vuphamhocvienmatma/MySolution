namespace Application.Common.Interfaces
{
    public interface ICurrentUserService
    {
        string? UserId { get; }
        string? TenantId { get; }
        string? Email { get; }
        bool IsAuthenticated { get; }
        IEnumerable<KeyValuePair<string, string>>? Claims { get; }
    }
}
