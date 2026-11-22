using CreditCardManagement.Application.Contracts;
using CreditCardManagement.Application.Services;
using Microsoft.AspNetCore.Mvc;
namespace CreditCardManagement.API.Controllers;
[ApiController,Route("api/v1/auth")]
public sealed class AuthController(AuthService auth) : ControllerBase
{
 [HttpPost("login")]
 public async Task<ActionResult<LoginResponse>> Login(LoginRequest request,CancellationToken ct)=>Ok(await auth.LoginAsync(request,ct));
}
