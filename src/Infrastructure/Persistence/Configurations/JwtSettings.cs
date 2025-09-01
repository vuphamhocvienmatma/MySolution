using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Configurations
{
    public class JwtSettings
    {
        public const string SectionName = "JwtSettings";

        [Required]
        public required string Issuer { get; init; }
        [Required]
        public required string Audience { get; init; }
        [Required]
        public required string Key { get; init; }
        [Required]
        public int TokenLifetimeMinutes { get; init; }
    }
}
