namespace CreditCardManagement.Domain.Entities;

public enum CardStatus { Active = 1, Blocked = 2, Cancelled = 3 }

public sealed class CreditCard
{
    private CreditCard() { }
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public string CardholderName { get; private set; } = string.Empty;
    public string CardToken { get; private set; } = string.Empty;
    public string LastFourDigits { get; private set; } = string.Empty;
    public string Network { get; private set; } = string.Empty;
    public decimal CreditLimit { get; private set; }
    public decimal OutstandingBalance { get; private set; }
    public CardStatus Status { get; private set; }
    public DateOnly ExpiryDate { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }

    public static CreditCard Issue(Guid customerId, string cardholderName, string token, string lastFour, string network, decimal limit, DateOnly expiry)
    {
        if (customerId == Guid.Empty) throw new ArgumentException("Customer is required.");
        if (string.IsNullOrWhiteSpace(cardholderName)) throw new ArgumentException("Cardholder name is required.");
        if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException("A payment-provider token is required.");
        if (lastFour.Length != 4 || !lastFour.All(char.IsDigit)) throw new ArgumentException("Last four digits must contain exactly four digits.");
        if (limit <= 0) throw new ArgumentOutOfRangeException(nameof(limit));
        if (expiry <= DateOnly.FromDateTime(DateTime.UtcNow)) throw new ArgumentException("Expiry date must be in the future.");
        var now = DateTime.UtcNow;
        return new CreditCard { Id = Guid.NewGuid(), CustomerId = customerId, CardholderName = cardholderName.Trim(), CardToken = token,
            LastFourDigits = lastFour, Network = network, CreditLimit = limit, OutstandingBalance = 0,
            Status = CardStatus.Active, ExpiryDate = expiry, CreatedUtc = now, UpdatedUtc = now };
    }
    public void Block() { EnsureNotCancelled(); if (Status == CardStatus.Active) { Status = CardStatus.Blocked; UpdatedUtc = DateTime.UtcNow; } }
    public void Unblock() { EnsureNotCancelled(); if (ExpiryDate <= DateOnly.FromDateTime(DateTime.UtcNow)) throw new InvalidOperationException("Expired cards cannot be unblocked."); Status = CardStatus.Active; UpdatedUtc = DateTime.UtcNow; }
    public void Cancel() { EnsureNotCancelled(); Status = CardStatus.Cancelled; UpdatedUtc = DateTime.UtcNow; }
    public void ChangeLimit(decimal limit) { EnsureNotCancelled(); if (limit <= 0 || limit < OutstandingBalance) throw new ArgumentException("Limit must be positive and cannot be below the outstanding balance."); CreditLimit = limit; UpdatedUtc = DateTime.UtcNow; }
    public void RecordPayment(decimal amount) { if (amount <= 0 || amount > OutstandingBalance) throw new ArgumentException("Payment amount is invalid."); OutstandingBalance -= amount; UpdatedUtc = DateTime.UtcNow; }
    private void EnsureNotCancelled() { if (Status == CardStatus.Cancelled) throw new InvalidOperationException("A cancelled card is a terminal state."); }
}
