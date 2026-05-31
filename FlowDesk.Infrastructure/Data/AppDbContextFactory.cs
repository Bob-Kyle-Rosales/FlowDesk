using FlowDesk.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FlowDesk.Infrastructure.Data;

/// <summary>
/// Used only by the EF Core CLI tooling (dotnet ef migrations add / database update).
/// Never instantiated at runtime — production uses the DI-registered AppDbContext.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? "Host=localhost;Port=5433;Database=flowdesk;Username=flowdesk;Password=flowdesk";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options, new NullCurrentUserService());
    }

    private sealed class NullCurrentUserService : ICurrentUserService
    {
        public Guid? UserId => null;
        public Guid? OrganisationId => null;
        public string? Role => null;
    }
}
