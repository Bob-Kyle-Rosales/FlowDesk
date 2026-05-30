namespace FlowDesk.Core.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    Guid? OrganisationId { get; }
    string? Role { get; }
}
