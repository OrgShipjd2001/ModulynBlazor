using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Modulyn.Server.Bl;

namespace ModulynServer.Services
{
    public sealed class PersonalAccessTokenService
    {
        private readonly ApplicationDbContext _db;

        public PersonalAccessTokenService(ApplicationDbContext db)
        {
            _db = db;
        }

        public sealed record CreateResult(PersonalAccessToken TokenRecord, string PlainTextToken);

        public async Task<List<PersonalAccessToken>> GetTokensForUserAsync(string userId, CancellationToken ct = default)
        {
            return await _db.PersonalAccessTokens
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedUtc)
                .ToListAsync(ct);
        }

        public async Task<CreateResult> CreateAsync(string userId, string name, DateTimeOffset? expiresUtc, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId is required", nameof(userId));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name is required", nameof(name));

            var salt = CreateUrlSafeRandom(16);
            var plain = CreateUrlSafeRandom(32);
            var hash = ComputeHashBase64(plain, salt);

            var record = new PersonalAccessToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = name.Trim(),
                Salt = salt,
                TokenHash = hash,
                CreatedUtc = DateTimeOffset.UtcNow,
                ExpiresUtc = expiresUtc
            };

            _db.PersonalAccessTokens.Add(record);
            await _db.SaveChangesAsync(ct);

            // Return plaintext only once to the caller (UI).
            return new CreateResult(record, plain);
        }

        public async Task<bool> RevokeAsync(string userId, Guid tokenId, CancellationToken ct = default)
        {
            var token = await _db.PersonalAccessTokens.FirstOrDefaultAsync(t => t.Id == tokenId && t.UserId == userId, ct);
            if (token is null)
                return false;

            if (!token.RevokedUtc.HasValue)
            {
                token.RevokedUtc = DateTimeOffset.UtcNow;
                await _db.SaveChangesAsync(ct);
            }

            return true;
        }

        public async Task<ApplicationUser?> ValidateAndGetUserAsync(string plainTextToken, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(plainTextToken))
                return null;

            // We store (salt, hash). To validate, we must recompute hash for each candidate.
            // This is acceptable for moderate token counts. If you expect very large token counts,
            // store a keyed hash prefix index or use JWTs minted from PATs.
            var candidates = await _db.PersonalAccessTokens
                .Where(t => t.RevokedUtc == null)
                .ToListAsync(ct);

            foreach (var t in candidates)
            {
                if (t.ExpiresUtc.HasValue && t.ExpiresUtc.Value <= DateTimeOffset.UtcNow)
                    continue;

                var computed = ComputeHashBase64(plainTextToken, t.Salt);
                if (CryptographicOperations.FixedTimeEquals(
                        Convert.FromBase64String(computed),
                        Convert.FromBase64String(t.TokenHash)))
                {
                    return await _db.Users.FirstOrDefaultAsync(u => u.Id == t.UserId, ct);
                }
            }

            return null;
        }

        private static string ComputeHashBase64(string token, string salt)
        {
            var bytes = Encoding.UTF8.GetBytes(token + ":" + salt);
            var hash = SHA256.HashData(bytes);
            return Convert.ToBase64String(hash);
        }

        private static string CreateUrlSafeRandom(int bytes)
        {
            var data = RandomNumberGenerator.GetBytes(bytes);
            return Base64UrlEncode(data);
        }

        private static string Base64UrlEncode(byte[] data)
        {
            var s = Convert.ToBase64String(data);
            s = s.TrimEnd('=');
            s = s.Replace('+', '-');
            s = s.Replace('/', '_');
            return s;
        }
    }
}
