namespace EDeals.Api.RedisServices
{
    public interface IJWTRevocationService
    {
        Task RevokeToken(string? token = null);
        Task<bool> IsTokenRevoked(string? token = null);
    }
}
