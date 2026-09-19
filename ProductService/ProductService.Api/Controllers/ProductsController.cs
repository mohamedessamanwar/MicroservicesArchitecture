using Microsoft.AspNetCore.Mvc;
using MediatR;
using ProductService.Application.Commands;

namespace ProductService.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> AddProduct([FromBody] AddProductCommand command, CancellationToken ct)
    {
        var productId = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(AddProduct), new { id = productId }, new { Message = "Product created successfully.", ProductId = productId });
    }

    [HttpPost("{id}/decrease-count")]
    public async Task<IActionResult> DecreaseCount(Guid id, [FromQuery] int amount, CancellationToken ct)
    {
        var result = await _mediator.Send(new DecreaseStockCommand(id, amount), ct);
        if (!result)
        {
            return BadRequest(new { Message = "Failed to decrease stock. Either product not found or insufficient stock." });
        }
        return Ok(new { Message = "Stock decreased successfully." });
    }

    [HttpPost("{id}/increase-count")]
    public async Task<IActionResult> IncreaseCount(Guid id, [FromQuery] int amount, CancellationToken ct)
    {
        var result = await _mediator.Send(new IncreaseStockCommand(id, amount), ct);
        if (!result)
        {
            return BadRequest(new { Message = "Failed to increase stock. Product not found." });
        }
        return Ok(new { Message = "Stock increased successfully." });
    }

    [HttpPost("decrease-bulk")]
    public async Task<IActionResult> DecreaseCountBulk([FromBody] List<BulkProductQuantity> items, CancellationToken ct)
    {
        var result = await _mediator.Send(new BulkDecreaseStockCommand(items), ct);
        if (!result)
        {
            return BadRequest(new { Message = "Failed to bulk decrease stock. Some products may not exist, have insufficient stock, or a concurrency conflict occurred." });
        }
        return Ok(new { Message = "Bulk stock decreased successfully." });
    }

    [HttpPost("increase-bulk")]
    public async Task<IActionResult> IncreaseCountBulk([FromBody] List<BulkProductQuantity> items, CancellationToken ct)
    {
        var result = await _mediator.Send(new BulkIncreaseStockCommand(items), ct);
        if (!result)
        {
            return BadRequest(new { Message = "Failed to bulk increase stock. A concurrency conflict occurred or products were not found." });
        }
        return Ok(new { Message = "Bulk stock increased successfully." });
    }
}
