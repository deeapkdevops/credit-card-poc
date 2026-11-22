using System.Text;
using CreditCardManagement.API.Authorization;
using CreditCardManagement.API.Middleware;
using CreditCardManagement.Application.Abstractions;
using CreditCardManagement.Application.Services;
using CreditCardManagement.Domain.Entities;
using CreditCardManagement.Infrastructure.Caching;
using CreditCardManagement.Infrastructure.Persistence;
using CreditCardManagement.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder=WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((ctx,lc)=>lc.ReadFrom.Configuration(ctx.Configuration).WriteTo.Console());
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
var jwt=builder.Configuration.GetSection("Jwt").Get<JwtOptions>()??throw new InvalidOperationException("Jwt configuration is missing.");
if(string.IsNullOrWhiteSpace(jwt.Secret)||Encoding.UTF8.GetByteCount(jwt.Secret)<32) throw new InvalidOperationException("Jwt:Secret must be configured with at least 32 bytes of cryptographically random secret material.");
builder.Services.AddDbContext<AppDbContext>(o=>o.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));
builder.Services.AddStackExchangeRedisCache(o=>{o.Configuration=builder.Configuration.GetConnectionString("Redis");o.InstanceName="CreditCardPOC:";});
builder.Services.AddScoped<ICreditCardRepository,CreditCardRepository>();builder.Services.AddScoped<IAppUserRepository,AppUserRepository>();builder.Services.AddScoped<ICacheService,RedisCacheService>();builder.Services.AddScoped<ITokenService,JwtTokenService>();
builder.Services.AddScoped<CreditCardService>();builder.Services.AddScoped<AuthService>();builder.Services.AddScoped<IPasswordHasher<AppUser>,PasswordHasher<AppUser>>();
builder.Services.AddSingleton<IAuthorizationHandler,ScopeHandler>();
builder.Services.AddAuthorization(o=>{foreach(var scope in new[]{"cards.read","cards.write","cards.block","cards.cancel","transactions.read","customers.read"})o.AddPolicy(scope,p=>p.RequireAuthenticatedUser().AddRequirements(new ScopeRequirement(scope)));});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o=>{o.TokenValidationParameters=new TokenValidationParameters{ValidateIssuer=true,ValidIssuer=jwt.Issuer,ValidateAudience=true,ValidAudience=jwt.Audience,ValidateIssuerSigningKey=true,IssuerSigningKey=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),ValidateLifetime=true,ClockSkew=TimeSpan.FromSeconds(30)};});
builder.Services.AddControllers();builder.Services.AddEndpointsApiExplorer();builder.Services.AddSwaggerGen();
var app=builder.Build();
// Optional first-run admin bootstrap. Configure via environment variables and remove after provisioning.
var bootstrapEmail=app.Configuration["BootstrapAdmin:Email"];
var bootstrapPassword=app.Configuration["BootstrapAdmin:Password"];
if(!string.IsNullOrWhiteSpace(bootstrapEmail)&&!string.IsNullOrWhiteSpace(bootstrapPassword))
{
 using var scope=app.Services.CreateScope();
 var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
 if(await db.Database.CanConnectAsync() && !await db.Users.AnyAsync(x=>x.Email==bootstrapEmail.ToLowerInvariant()))
 {
  var user=new AppUser { Id=Guid.NewGuid(), Email=bootstrapEmail.ToLowerInvariant(), Role="Admin", Scopes="cards.read cards.write cards.block cards.cancel transactions.read customers.read", IsActive=true };
  user.PasswordHash=scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>().HashPassword(user,bootstrapPassword);
  db.Users.Add(user); await db.SaveChangesAsync();
 }
}
app.UseMiddleware<ExceptionMiddleware>();if(app.Environment.IsDevelopment()){app.UseSwagger();app.UseSwaggerUI();}app.UseSerilogRequestLogging();app.UseHttpsRedirection();app.UseAuthentication();app.UseAuthorization();app.MapControllers();app.MapGet("/health",()=>Results.Ok(new{status="healthy",utc=DateTime.UtcNow})).AllowAnonymous();app.Run();
public partial class Program { }
