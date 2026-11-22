using CreditCardManagement.Domain.Entities;
namespace CreditCardManagement.Application.Contracts;
public sealed record CreateCardRequest(Guid CustomerId, string CardholderName, string CardToken, string LastFourDigits, string Network, decimal CreditLimit, DateOnly ExpiryDate);
public sealed record CardResponse(Guid Id, Guid CustomerId, string CardholderName, string LastFourDigits, string Network, decimal CreditLimit, decimal OutstandingBalance, decimal AvailableCredit, string Status, DateOnly ExpiryDate)
{
    public static CardResponse From(CreditCard c) => new(c.Id,c.CustomerId,c.CardholderName,c.LastFourDigits,c.Network,c.CreditLimit,c.OutstandingBalance,c.CreditLimit-c.OutstandingBalance,c.Status.ToString(),c.ExpiryDate);
}
public sealed record LoginRequest(string Email, string Password);
public sealed record LoginResponse(string AccessToken, string TokenType, int ExpiresIn);
