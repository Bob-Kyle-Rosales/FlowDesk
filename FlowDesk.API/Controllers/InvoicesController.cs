using FlowDesk.Core.DTOs.Invoices;
using FlowDesk.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowDesk.API.Controllers;

[ApiController]
[Route("api/invoices")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _service;

    public InvoicesController(IInvoiceService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<InvoiceResponse>>> GetAll()
        => Ok(await _service.GetAllAsync());

    [HttpPost]
    [Authorize(Policy = "AgencyOnly")]
    public async Task<ActionResult<InvoiceResponse>> Create([FromBody] CreateInvoiceRequest request)
    {
        var result = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InvoiceResponse>> GetById(Guid id)
        => Ok(await _service.GetByIdAsync(id));

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "AgencyOnly")]
    public async Task<ActionResult<InvoiceResponse>> Update(
        Guid id, [FromBody] UpdateInvoiceRequest request)
        => Ok(await _service.UpdateAsync(id, request));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "AgencyOnly")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
