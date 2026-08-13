using AxiInstallConsumerPackage.Configuration;
using AxiInstallConsumerPackage.Models;
using AxiInstallConsumerPackage.Services.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;

namespace AxiInstallConsumerPackage.Services;

/// <summary>
/// Redis-backed token store.
///
///   GetAsync  → read sessionId from request cookie
///             → lookup JWT in Redis (returns null if missing or expired)
/// </summary>
public sealed class RedisTokenStore(
    IDistributedCache redis,
    IOptions<RedisSettings> redisOpts,
    ILogger<RedisTokenStore> logger) : ITokenStore
{
    private static readonly JsonSerializerOptions _json =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly TimeSpan _ttl =
        TimeSpan.FromMinutes(redisOpts.Value.AbsoluteTimeoutMinutes);

    // ── Write ─────────────────────────────────────────────────────────────────
    public async Task SetAsync(string key, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key must not be empty.", nameof(key));

        // Store JWT in Redis with sliding expiry
        await redis.SetStringAsync(key, "", new DistributedCacheEntryOptions
        {
            SlidingExpiration = _ttl
        }, ct);

        

        logger.LogInformation("Redis Key created for {key}. TTL={TTL}min",key, _ttl.TotalMinutes);
    }

    // ── Read ──────────────────────────────────────────────────────────────────
    public async Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        var raw = await redis.GetStringAsync(key, ct);

        if (raw is null)
        {
            logger.LogWarning(" Key not found in Redis.",
                key);
            return null;
        }

        return raw;
    }

    // ── Patch (add/edit fields without replacing the whole object) ────────────
    /*public async Task<bool> UpdateAsync(Action<SessionData> mutate, string sessionId, CancellationToken ct = default)
    {
        //if (!Ctx.Request.Cookies.TryGetValue(sessionOpts.Value.CookieName, out var sessionId)
        //    || string.IsNullOrWhiteSpace(sessionId))
        //    return false;

        var redisKey = redisOpts.Value.RedisPrefix + sessionId;
        var raw = await redis.GetStringAsync(redisKey, ct);
        if (raw is null) return false;

        var session = JsonSerializer.Deserialize<SessionData>(raw, _json)!;
        mutate(session);                                        // caller mutates in place

        await redis.SetStringAsync(redisKey,
            JsonSerializer.Serialize(session, _json), ct);

        return true;
    }*/

    // ── Delete ────────────────────────────────────────────────────────────────
    public async Task DeleteAsync(string key,CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            await redis.RemoveAsync(key, ct);
            logger.LogInformation($"Key {key} removed from Redis.");
        }
    }

    /// <summary>Reads request cookie; throws 401 if none exists (SECURE endpoints).</summary>
    //public string GetSessionIdAsync(CancellationToken ct = default)
    //{
    //    if (!Ctx.Request.Cookies.TryGetValue(sessionOpts.Value.CookieName, out var sessionId)
    //|| string.IsNullOrWhiteSpace(sessionId))
    //    {
    //        logger.LogDebug("No session cookie present.");
    //        throw new UnauthorizedAccessException("No active session. Unauthorized error.");
    //    }
    //    return sessionId;
    //}

    //public async Task<bool> HasTokenAsync(CancellationToken ct = default) =>
    //    !string.IsNullOrWhiteSpace(await GetAsync(ct));
}
