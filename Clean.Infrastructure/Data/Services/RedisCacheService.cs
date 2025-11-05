using System.Text.Json;
using Clean.Application.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using IDatabase = Microsoft.EntityFrameworkCore.Storage.IDatabase;

namespace Clean.Infrastructure.Data.Services;

public class RedisCacheService : ICacheService
{
    private readonly StackExchange.Redis.IDatabase _database;
    private readonly IConnectionMultiplexer _redis;

    public RedisCacheService(IConnectionMultiplexer redis)
    {
        _database = redis.GetDatabase();
        _redis = redis;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var value = await _database.StringGetAsync(key);
        if (value.IsNullOrEmpty) return default;
        return JsonSerializer.Deserialize<T>(value!);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        var json = JsonSerializer.Serialize(value);
        await _database.StringSetAsync(key, json, expiration ?? TimeSpan.FromMinutes(5));
    }

    public async Task RemoveAsync(string key)
    {
        await _database.KeyDeleteAsync(key);
    }

    public async Task<bool> ExistsAsync(string key)
    {
        return await _database.KeyExistsAsync(key);
    }
    
    public async Task RemoveByPatternAsync(string pattern)
    {
        var endpoints = _redis.GetEndPoints();
        foreach (var endpoint in endpoints)
        {
            var server = _redis.GetServer(endpoint);
            // Use SCAN-like method for safety in production
            var keys = server.Keys(pattern: pattern + "*").ToArray();
            foreach (var key in keys)
            {
                await _database.KeyDeleteAsync(key);
            }
        }
    }
}