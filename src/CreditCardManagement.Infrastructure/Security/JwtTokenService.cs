using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CreditCardManagement.Application.Abstractions;
using CreditCardManagement.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
namespace CreditCardManagement.Infrastructure.Security;
public sealed class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
 public string CreateAccessToken(AppUser user)
 {
  var o=options.Value; var key=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(o.Secret));
  var claims=new[]{new Claim(JwtRegisteredClaimNames.Sub,user.Id.ToString()),new Claim(JwtRegisteredClaimNames.Email,user.Email),new Claim(ClaimTypes.Role,user.Role),new Claim("scope",user.Scopes),new Claim("customer_id",user.CustomerId?.ToString()??string.Empty)};
  var token=new JwtSecurityToken(o.Issuer,o.Audience,claims,DateTime.UtcNow,DateTime.UtcNow.AddMinutes(o.AccessTokenMinutes),new SigningCredentials(key,SecurityAlgorithms.HmacSha256));
  return new JwtSecurityTokenHandler().WriteToken(token);
 }
}
