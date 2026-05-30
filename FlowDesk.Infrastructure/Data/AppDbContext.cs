using FlowDesk.Core.Entities;
using FlowDesk.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlowDesk.Infrastructure.Data;

public class AppDbContext : DbContext
{
    private readonly ICurrentUserService _currentUserService;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService currentUserService)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<Organisation> Organisations => Set<Organisation>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Milestone> Milestones => Set<Milestone>();
    public DbSet<Deliverable> Deliverables => Set<Deliverable>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Multi-tenancy: EF evaluates these lambdas at query time against the current DbContext
        // instance, so each request sees only its own org's data.
        // Use .IgnoreQueryFilters() at the call site for auth/public endpoints that run before
        // a user is authenticated (e.g. login, register, public status page).
        modelBuilder.Entity<User>()
            .HasQueryFilter(u => _currentUserService.OrganisationId == null ||
                                 u.OrganisationId == _currentUserService.OrganisationId);

        modelBuilder.Entity<Project>()
            .HasQueryFilter(p => _currentUserService.OrganisationId != null &&
                                 p.OrganisationId == _currentUserService.OrganisationId);

        modelBuilder.Entity<Invoice>()
            .HasQueryFilter(i => _currentUserService.OrganisationId != null &&
                                 i.OrganisationId == _currentUserService.OrganisationId);

        // Unique indexes
        modelBuilder.Entity<Organisation>()
            .HasIndex(o => o.Slug).IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email).IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(r => r.Token).IsUnique();

        // Explicit FK behaviour for relationships that would create cascade delete cycles
        modelBuilder.Entity<Project>()
            .HasOne(p => p.Client)
            .WithMany()
            .HasForeignKey(p => p.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Deliverable>()
            .HasOne(d => d.ApprovedBy)
            .WithMany()
            .HasForeignKey(d => d.ApprovedById)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Invoice>()
            .HasOne(i => i.Client)
            .WithMany()
            .HasForeignKey(i => i.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Message>()
            .HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
