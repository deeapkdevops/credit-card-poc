using System.Net;
using System.Text.Json;
namespace CreditCardManagement.API.Middleware;
public sealed class ExceptionMiddleware(RequestDelegate next,ILogger<ExceptionMiddleware> logger)
{
 public async Task InvokeAsync(HttpContext context)
 {
  try{await next(context);}catch(Exception ex){logger.LogError(ex,"Unhandled API exception");context.Response.ContentType="application/problem+json";context.Response.StatusCode=ex switch{KeyNotFoundException=>404,UnauthorizedAccessException=>401,ArgumentException or InvalidOperationException=>400,_=>500};await context.Response.WriteAsync(JsonSerializer.Serialize(new{type="about:blank",title="Request failed",status=context.Response.StatusCode,detail=context.Response.StatusCode==500?"An unexpected error occurred.":ex.Message,traceId=context.TraceIdentifier}));}
 }
}
