using EDeals.Api.Settings;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace EDeals.Api.RedisServices
{
    public class JWTRevocationService : IJWTRevocationService
    {
        private readonly IDistributedCache _distributedCache;
        private readonly JwtSettings _jwtSettings;

        public JWTRevocationService(IDistributedCache distributedCache, IOptions<JwtSettings> jwtSettings)
        {
            _distributedCache = distributedCache;
            _jwtSettings = jwtSettings.Value;
        }

        public async Task RevokeToken(string? token = null)
        {
            await _distributedCache.SetStringAsync(token, "Revoked", new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(int.Parse(_jwtSettings.Expiration))
            });
        }

        public async Task<bool> IsTokenRevoked(string? token = null)
        {
            if (token == null) return false;

            var cachedToken = await _distributedCache.GetStringAsync(token);

            return !string.IsNullOrEmpty(cachedToken);
        }
    }
}
