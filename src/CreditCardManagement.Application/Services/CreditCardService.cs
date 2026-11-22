using CreditCardManagement.Application.Abstractions;
using CreditCardManagement.Application.Contracts;
using CreditCardManagement.Domain.Entities;
namespace CreditCardManagement.Application.Services;
public sealed class CreditCardService(ICreditCardRepository repository, ICacheService cache)
{
    public async Task<CardResponse> IssueAsync(CreateCardRequest request, CancellationToken ct)
    {
        var card = CreditCard.Issue(request.CustomerId, request.CardholderName, request.CardToken, request.LastFourDigits, request.Network, request.CreditLimit, request.ExpiryDate);
        await repository.AddAsync(card, ct); await repository.SaveChangesAsync(ct);
        return CardResponse.From(card);
    }
    public async Task<CardResponse?> GetAsync(Guid id, CancellationToken ct)
    {
        var key = $"cards:{id}";
        var cached = await cache.GetAsync<CardResponse>(key, ct); if (cached is not null) return cached;
        var card = await repository.GetByIdAsync(id, ct); if (card is null) return null;
        var response = CardResponse.From(card); await cache.SetAsync(key, response, TimeSpan.FromMinutes(5), ct); return response;
    }
    public async Task<IReadOnlyList<CardResponse>> GetCustomerCardsAsync(Guid customerId, CancellationToken ct)
    {
        var cards = await repository.GetByCustomerAsync(customerId, ct); return cards.Select(CardResponse.From).ToList();
    }
    public async Task ChangeStatusAsync(Guid id, bool block, CancellationToken ct)
    {
        var card = await repository.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("Card not found.");
        if (block) card.Block(); else card.Unblock(); await repository.SaveChangesAsync(ct); await cache.RemoveAsync($"cards:{id}", ct);
    }
    public async Task CancelAsync(Guid id, CancellationToken ct)
    {
        var card = await repository.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("Card not found.");
        card.Cancel(); await repository.SaveChangesAsync(ct); await cache.RemoveAsync($"cards:{id}", ct);
    }
}
