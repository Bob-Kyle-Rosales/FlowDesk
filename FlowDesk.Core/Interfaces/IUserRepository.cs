using FlowDesk.Core.Entities;

namespace FlowDesk.Core.Interfaces;

public interface IUserRepository
{
    Task<bool> EmailExistsAsync(string email);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(Guid id);
    Task CreateWithOrganisationAsync(User user, Organisation organisation);
    Task AddRefreshTokenAsync(RefreshToken token);
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task RevokeRefreshTokenAsync(Guid tokenId);
    Task SaveChangesAsync();
    Task<IEnumerable<User>> GetClientUsersAsync();
}
