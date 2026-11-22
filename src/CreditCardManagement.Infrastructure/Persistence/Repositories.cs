using CreditCardManagement.Application.Abstractions;
using CreditCardManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace CreditCardManagement.Infrastructure.Persistence;
public sealed class CreditCardRepository(AppDbContext db) : ICreditCardRepository
{
 public Task<CreditCard?> GetByIdAsync(Guid id,CancellationToken ct)=>db.CreditCards.SingleOrDefaultAsync(x=>x.Id==id,ct);
 public async Task<IReadOnlyList<CreditCard>> GetByCustomerAsync(Guid id,CancellationToken ct)=>await db.CreditCards.AsNoTracking().Where(x=>x.CustomerId==id).OrderByDescending(x=>x.CreatedUtc).ToListAsync(ct);
 public Task AddAsync(CreditCard card,CancellationToken ct)=>db.CreditCards.AddAsync(card,ct).AsTask();
 public Task SaveChangesAsync(CancellationToken ct)=>db.SaveChangesAsync(ct);
}
public sealed class AppUserRepository(AppDbContext db) : IAppUserRepository
{ public Task<AppUser?> FindByEmailAsync(string email,CancellationToken ct)=>db.Users.AsNoTracking().SingleOrDefaultAsync(x=>x.Email==email.ToLower(),ct); }
