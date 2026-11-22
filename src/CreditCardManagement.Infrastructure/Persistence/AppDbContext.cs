using CreditCardManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace CreditCardManagement.Infrastructure.Persistence;
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<CreditCard> CreditCards => Set<CreditCard>();
    public DbSet<AppUser> Users => Set<AppUser>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<CreditCard>(e => { e.HasKey(x=>x.Id); e.Property(x=>x.CardholderName).HasMaxLength(120).IsRequired(); e.Property(x=>x.CardToken).HasMaxLength(200).IsRequired(); e.Property(x=>x.LastFourDigits).HasMaxLength(4).IsRequired(); e.Property(x=>x.Network).HasMaxLength(30); e.Property(x=>x.CreditLimit).HasPrecision(18,2); e.Property(x=>x.OutstandingBalance).HasPrecision(18,2); e.HasIndex(x=>x.CustomerId); });
        b.Entity<AppUser>(e => { e.HasKey(x=>x.Id); e.HasIndex(x=>x.Email).IsUnique(); e.Property(x=>x.Email).HasMaxLength(256); e.Property(x=>x.Scopes).HasMaxLength(1000); });
    }
}
