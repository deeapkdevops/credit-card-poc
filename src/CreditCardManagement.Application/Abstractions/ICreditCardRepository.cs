using CreditCardManagement.Domain.Entities;
namespace CreditCardManagement.Application.Abstractions;
public interface ICreditCardRepository
{
    Task<CreditCard?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<CreditCard>> GetByCustomerAsync(Guid customerId, CancellationToken ct);
    Task AddAsync(CreditCard card, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
public interface IAppUserRepository { Task<AppUser?> FindByEmailAsync(string email, CancellationToken ct); }
public interface ICacheService { Task<T?> GetAsync<T>(string key, CancellationToken ct); Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct); Task RemoveAsync(string key, CancellationToken ct); }
public interface ITokenService { string CreateAccessToken(AppUser user); }
