using System.Text.Json;
using CreditCardManagement.Application.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
namespace CreditCardManagement.Infrastructure.Caching;
public sealed class RedisCacheService(IDistributedCache cache) : ICacheService
{
 public async Task<T?> GetAsync<T>(string key,CancellationToken ct){var json=await cache.GetStringAsync(key,ct);return json is null?default:JsonSerializer.Deserialize<T>(json);}
 public Task SetAsync<T>(string key,T value,TimeSpan ttl,CancellationToken ct)=>cache.SetStringAsync(key,JsonSerializer.Serialize(value),new DistributedCacheEntryOptions{AbsoluteExpirationRelativeToNow=ttl},ct);
 public Task RemoveAsync(string key,CancellationToken ct)=>cache.RemoveAsync(key,ct);
}
