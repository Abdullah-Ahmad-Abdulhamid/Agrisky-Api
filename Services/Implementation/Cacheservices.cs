using Microsoft.Extensions.Caching.Memory;

namespace AgriSky.API.Services;

public interface ICacheService
{
    T? Get<T>(string key);
    void Set<T>(string key, T value, TimeSpan? expiry = null);
    void Remove(string key);
    void RemoveByPrefix(string prefix);
}

public class CacheService(IMemoryCache cache, ILogger<CacheService> logger) : ICacheService
{
    // Track keys so we can remove by prefix without Redis SCAN
    private readonly HashSet<string> _keys = [];
    private readonly object _lock = new();

    private static readonly MemoryCacheEntryOptions DefaultOptions = new MemoryCacheEntryOptions()
        .SetAbsoluteExpiration(TimeSpan.FromMinutes(10))
        .SetSlidingExpiration(TimeSpan.FromMinutes(5))
        .SetSize(1);

    public T? Get<T>(string key)
    {
        if (cache.TryGetValue(key, out T? value))
        {
            logger.LogDebug("Cache HIT  → {Key}", key);
            return value;
        }
        logger.LogDebug("Cache MISS → {Key}", key);
        return default;
    }

    public void Set<T>(string key, T value, TimeSpan? expiry = null)
    {
        var opts = expiry.HasValue
            ? new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(expiry.Value)
                .SetSize(1)
            : DefaultOptions;

        cache.Set(key, value, opts);

        lock (_lock) { _keys.Add(key); }
        logger.LogDebug("Cache SET  → {Key}", key);
    }

    public void Remove(string key)
    {
        cache.Remove(key);
        lock (_lock) { _keys.Remove(key); }
        logger.LogDebug("Cache REMOVE → {Key}", key);
    }

    public void RemoveByPrefix(string prefix)
    {
        List<string> toRemove;
        lock (_lock)
        {
            toRemove = _keys.Where(k => k.StartsWith(prefix)).ToList();
            foreach (var k in toRemove) _keys.Remove(k);
        }
        foreach (var k in toRemove) cache.Remove(k);
        logger.LogDebug("Cache REMOVE_PREFIX → {Prefix} ({Count} keys)", prefix, toRemove.Count);
    }
}

// ─── Cache Key Helpers ─────────────────────────────────────────────────────
public static class CacheKeys
{
    public static string UserProfile(int userId) => $"user_profile_{userId}";
    public static string UserPrefix(int userId) => $"user_profile_{userId}";
}