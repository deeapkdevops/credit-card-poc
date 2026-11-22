namespace CreditCardManagement.Infrastructure.Security;
public sealed class JwtOptions { public string Issuer {get;set;}="CreditCardManagement"; public string Audience {get;set;}="CreditCardManagement.Client"; public string Secret {get;set;}=string.Empty; public int AccessTokenMinutes {get;set;}=15; }
