namespace CreditCardManagement.Domain.Entities;
public sealed class AppUser
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Customer";
    public string Scopes { get; set; } = "cards.read";
    public Guid? CustomerId { get; set; }
    public bool IsActive { get; set; } = true;
}
