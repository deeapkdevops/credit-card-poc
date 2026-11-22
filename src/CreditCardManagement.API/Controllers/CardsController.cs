using System.Security.Claims;
using CreditCardManagement.Application.Contracts;
using CreditCardManagement.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace CreditCardManagement.API.Controllers;
[ApiController,Route("api/v1/cards"),Authorize]
public sealed class CardsController(CreditCardService cards) : ControllerBase
{
 [HttpPost,Authorize(Policy="cards.write")]
 public async Task<ActionResult<CardResponse>> Create(CreateCardRequest request,CancellationToken ct){if(!CanAccess(request.CustomerId))return Forbid();var result=await cards.IssueAsync(request,ct);return CreatedAtAction(nameof(Get),new{id=result.Id},result);}
 [HttpGet("{id:guid}"),Authorize(Policy="cards.read")]
 public async Task<ActionResult<CardResponse>> Get(Guid id,CancellationToken ct){var card=await cards.GetAsync(id,ct);if(card is null)return NotFound();if(!CanAccess(card.CustomerId))return Forbid();return Ok(card);}
 [HttpGet("customer/{customerId:guid}"),Authorize(Policy="cards.read")]
 public async Task<ActionResult<IReadOnlyList<CardResponse>>> GetCustomer(Guid customerId,CancellationToken ct){if(!CanAccess(customerId))return Forbid();return Ok(await cards.GetCustomerCardsAsync(customerId,ct));}
 [HttpPost("{id:guid}/block"),Authorize(Policy="cards.block")]
 public async Task<IActionResult> Block(Guid id,CancellationToken ct){var existing=await cards.GetAsync(id,ct);if(existing is null)return NotFound();if(!CanAccess(existing.CustomerId))return Forbid();await cards.ChangeStatusAsync(id,true,ct);return NoContent();}
 [HttpPost("{id:guid}/unblock"),Authorize(Policy="cards.block")]
 public async Task<IActionResult> Unblock(Guid id,CancellationToken ct){var existing=await cards.GetAsync(id,ct);if(existing is null)return NotFound();if(!CanAccess(existing.CustomerId))return Forbid();await cards.ChangeStatusAsync(id,false,ct);return NoContent();}
 [HttpPost("{id:guid}/cancel"),Authorize(Policy="cards.cancel")]
 public async Task<IActionResult> Cancel(Guid id,CancellationToken ct){var existing=await cards.GetAsync(id,ct);if(existing is null)return NotFound();if(!CanAccess(existing.CustomerId))return Forbid();await cards.CancelAsync(id,ct);return NoContent();}
 private bool CanAccess(Guid customerId)=>User.IsInRole("Admin")||User.IsInRole("Operations")||User.FindFirstValue("customer_id")==customerId.ToString();
}
