using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Tenants
{
    public class Tenant
    {
        public string Id { get; set; } 
        public required string Name { get; set; }
        public bool IsActive { get; set; }
    }
}
