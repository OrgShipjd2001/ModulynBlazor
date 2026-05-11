using System.ComponentModel.DataAnnotations;

namespace Modulyn.Server.Bl
{
    public class PersonalAccessToken
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public ApplicationUser? User { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        // Random per-token salt for hashing.
        [Required]
        [MaxLength(200)]
        public string Salt { get; set; } = string.Empty;

        // Base64-encoded SHA256(token + ":" + salt)
        [Required]
        [MaxLength(200)]
        public string TokenHash { get; set; } = string.Empty;

        public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? ExpiresUtc { get; set; }

        public DateTimeOffset? RevokedUtc { get; set; }

        public bool IsRevoked => RevokedUtc.HasValue;

        public bool IsExpired => ExpiresUtc.HasValue && ExpiresUtc.Value <= DateTimeOffset.UtcNow;
    }
}
