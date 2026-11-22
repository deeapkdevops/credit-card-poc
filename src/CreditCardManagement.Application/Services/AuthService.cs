using CreditCardManagement.Application.Abstractions;
using CreditCardManagement.Application.Contracts;
using Microsoft.AspNetCore.Identity;
using CreditCardManagement.Domain.Entities;
namespace CreditCardManagement.Application.Services;
public sealed class AuthService(IAppUserRepository users, ITokenService tokens, IPasswordHasher<AppUser> hasher)
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(request.Email, ct);
        if (user is null || !user.IsActive || hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Invalid email or password.");
        return new LoginResponse(tokens.CreateAccessToken(user), "Bearer", 900);
    }
}
