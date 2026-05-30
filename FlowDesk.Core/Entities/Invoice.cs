using FlowDesk.Core.Enums;

namespace FlowDesk.Core.Entities;

/// <summary>
/// An invoice sent from the agency to a client. Paid via Stripe Connect —
/// payments go directly to the agency's Stripe account (Phase 4).
/// </summary>
public class Invoice
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    // Populated when the client initiates payment; used to confirm payment via webhook
    public string? StripePaymentIntentId { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid OrganisationId { get; set; }
    public Organisation Organisation { get; set; } = null!;

    // Optional — invoices can be standalone or tied to a specific project
    public Guid? ProjectId { get; set; }
    public Project? Project { get; set; }

    public Guid ClientId { get; set; }
    public User Client { get; set; } = null!;

    public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
}
