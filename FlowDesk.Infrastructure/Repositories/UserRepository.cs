using FlowDesk.Core.Entities;
using FlowDesk.Core.Enums;
using FlowDesk.Core.Interfaces;
using FlowDesk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FlowDesk.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Checks email uniqueness across all organisations.
    /// IgnoreQueryFilters bypasses the org-scoped global filter — emails must be globally unique.
    /// </summary>
    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == email.ToLower());
    }

    /// <summary>
    /// Looks up a user by email for login. IgnoreQueryFilters is required because no org claim
    /// exists yet — the JWT hasn't been issued, so ICurrentUserService.OrganisationId is null.
    /// Organisation is eagerly loaded because AuthService needs it to build the AuthResponse.
    /// </summary>
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .IgnoreQueryFilters()
            .Include(u => u.Organisation)
            .FirstOrDefaultAsync(u => u.Email == email.ToLower());
    }

    /// <summary>
    /// Looks up a user by ID for token refresh. IgnoreQueryFilters for the same reason as
    /// GetByEmailAsync — the refresh endpoint runs before a new access token is issued.
    /// </summary>
    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users
            .IgnoreQueryFilters()
            .Include(u => u.Organisation)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    /// <summary>
    /// Persists the Organisation first, then the User in the same transaction via SaveChangesAsync.
    /// Order matters — User has a FK to Organisation so Organisation must exist first.
    /// </summary>
    public async Task CreateWithOrganisationAsync(User user, Organisation organisation)
    {
        await _context.Organisations.AddAsync(organisation);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Persists a new user into an existing organisation (invite acceptance).
    /// Unlike CreateWithOrganisationAsync, no new Organisation is created.
    /// </summary>
    public async Task CreateAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Persists a new refresh token after issuing tokens. Each login/refresh creates a new record.
    /// Old tokens are revoked rather than deleted so there is an audit trail.
    /// </summary>
    public async Task AddRefreshTokenAsync(RefreshToken token)
    {
        await _context.RefreshTokens.AddAsync(token);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Loads the refresh token with its User and Organisation so AuthService can build
    /// a full AuthResponse without a second DB round-trip.
    /// </summary>
    public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
    {
        return await _context.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.Organisation)
            .FirstOrDefaultAsync(rt => rt.Token == token);
    }

    /// <summary>
    /// Marks the token as revoked rather than deleting it — supports token rotation and
    /// allows detection of reuse attempts (a revoked token being used again signals a stolen token).
    /// </summary>
    public async Task RevokeRefreshTokenAsync(Guid tokenId)
    {
        var token = await _context.RefreshTokens.FindAsync(tokenId);
        if (token is not null)
        {
            token.IsRevoked = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Retrieves all Client users in the current organisation, sorted by name.
    /// The EF global query filter on Users already scopes to the current org — no additional filter needed.
    /// </summary>
    public async Task<IEnumerable<User>> GetClientUsersAsync()
        => await _context.Users
            .Where(u => u.Role == UserRole.Client)
            .OrderBy(u => u.Name)
            .ToListAsync();
}
