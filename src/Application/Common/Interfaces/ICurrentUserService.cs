using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
