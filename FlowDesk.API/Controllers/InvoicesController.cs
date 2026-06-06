using FlowDesk.Core.DTOs.Invoices;
using FlowDesk.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace FlowDesk.API.Controllers;

[ApiController]
[Route("api/invoices")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _service;
    private readonly IStripeService _stripe;
    private readonly ILogger<InvoicesController> _logger;

    public InvoicesController(
        IInvoiceService service,
        IStripeService stripe,
        ILogger<InvoicesController> logger)
    {
        _service = service;
        _stripe = stripe;
        _logger = logger;
    }

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

    [HttpPost("{id:guid}/send")]
    [Authorize(Policy = "AgencyOnly")]
    public async Task<ActionResult<InvoiceResponse>> Send(Guid id)
        => Ok(await _service.SendAsync(id));

    [HttpPost("{id:guid}/pay")]
    [Authorize(Policy = "ClientOnly")]
    public async Task<ActionResult<PayInvoiceResponse>> Pay(Guid id)
        => Ok(await _service.PayAsync(id));

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook()
    {
        Request.EnableBuffering();
        var payload = await new StreamReader(Request.Body).ReadToEndAsync();
        Request.Body.Position = 0;
        var signature = Request.Headers["Stripe-Signature"].ToString();

        if (!_stripe.TryConstructWebhookEvent(payload, signature, out var rawEvent))
        {
            _logger.LogWarning("Stripe webhook received with invalid signature");
            return BadRequest("Invalid signature.");
        }

        var stripeEvent = (Event)rawEvent!;

        if (stripeEvent.Type == "payment_intent.succeeded" &&
            stripeEvent.Data.Object is PaymentIntent intent)
        {
            try
            {
                await _service.HandlePaymentSucceededAsync(intent.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle payment_intent.succeeded for {IntentId}", intent.Id);
            }
        }

        return Ok();
    }
}
