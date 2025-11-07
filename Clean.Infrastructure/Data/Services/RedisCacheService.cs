using System.Text.Json;
using Clean.Application.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using IDatabase = Microsoft.EntityFrameworkCore.Storage.IDatabase;

namespace Clean.Infrastructure.Data.Services;

public class RedisCacheService : ICacheService
{
    private readonly StackExchange.Redis.IDatabase _database;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
    {
        _database = redis.GetDatabase();
        _redis = redis;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var value = await _database.StringGetAsync(key);
            if (value.IsNullOrEmpty) return default;
            return JsonSerializer.Deserialize<T>(value!);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogWarning(ex, "Redis connection issue while getting key: {Key}", key);
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while getting Redis key: {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            await _database.StringSetAsync(key, json, expiration ?? TimeSpan.FromMinutes(5));
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogWarning(ex, "Redis connection issue while setting key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while setting Redis key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _database.KeyDeleteAsync(key);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogWarning(ex, "Redis connection issue while removing key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while removing Redis key: {Key}", key);
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            return await _database.KeyExistsAsync(key);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogWarning(ex, "Redis connection issue while checking existence of key: {Key}", key);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while checking existence of Redis key: {Key}", key);
            return false;
        }
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            var endpoints = _redis.GetEndPoints();
            foreach (var endpoint in endpoints)
            {
                var server = _redis.GetServer(endpoint);
                var keys = server.Keys(pattern: pattern + "*").ToArray();
                foreach (var key in keys)
                {
                    await _database.KeyDeleteAsync(key);
                }
            }
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogWarning(ex, "Redis connection issue while removing keys by pattern: {Pattern}", pattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while removing Redis keys by pattern: {Pattern}", pattern);
        }
    }
}
