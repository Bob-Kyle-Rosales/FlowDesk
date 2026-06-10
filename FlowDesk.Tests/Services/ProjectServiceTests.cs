using FlowDesk.Core.DTOs.Projects;
using FlowDesk.Core.Entities;
using FlowDesk.Core.Interfaces;
using FlowDesk.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FlowDesk.Tests.Services;

public class ProjectServiceTests
{
    private readonly Mock<IProjectRepository> _repo = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly ProjectService _sut;

    public ProjectServiceTests()
    {
        _sut = new ProjectService(
            _repo.Object,
            _currentUser.Object,
            NullLogger<ProjectService>.Instance);
    }

    [Fact]
    public async Task GetAllAsync_AgencyUser_PassesNullClientFilter()
    {
        _currentUser.Setup(u => u.Role).Returns("AgencyOwner");
        _repo.Setup(r => r.GetAllAsync(null)).ReturnsAsync(Array.Empty<Project>());

        await _sut.GetAllAsync();

        _repo.Verify(r => r.GetAllAsync(null), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ClientUser_PassesUserIdFilter()
    {
        var clientId = Guid.NewGuid();
        _currentUser.Setup(u => u.Role).Returns("Client");
        _currentUser.Setup(u => u.UserId).Returns(clientId);
        _repo.Setup(r => r.GetAllAsync(clientId)).ReturnsAsync(Array.Empty<Project>());

        await _sut.GetAllAsync();

        _repo.Verify(r => r.GetAllAsync(clientId), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ProjectNotFound_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        _currentUser.Setup(u => u.Role).Returns("AgencyOwner");
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Project?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.GetByIdAsync(id));
    }

    [Fact]
    public async Task GetByIdAsync_ClientAccessingWrongProject_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var project = new Project { Id = id, ClientId = Guid.NewGuid() };

        _currentUser.Setup(u => u.Role).Returns("Client");
        _currentUser.Setup(u => u.UserId).Returns(callerId);
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(project);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.GetByIdAsync(id));
    }

    [Fact]
    public async Task GetStatsAsync_WithMilestones_CalculatesProgressPercent()
    {
        var id = Guid.NewGuid();
        _currentUser.Setup(u => u.Role).Returns("AgencyOwner");
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new Project { Id = id });
        _repo.Setup(r => r.GetStatsAsync(id)).ReturnsAsync((4, 2, 3, 1));

        var result = await _sut.GetStatsAsync(id);

        Assert.Equal(50, result.ProgressPercent);
        Assert.Equal(4, result.MilestoneCount);
        Assert.Equal(2, result.CompletedMilestones);
        Assert.Equal(3, result.DeliverableCount);
        Assert.Equal(1, result.ApprovedDeliverables);
    }

    [Fact]
    public async Task GetStatsAsync_ZeroMilestones_ReturnsZeroPercent()
    {
        var id = Guid.NewGuid();
        _currentUser.Setup(u => u.Role).Returns("AgencyOwner");
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new Project { Id = id });
        _repo.Setup(r => r.GetStatsAsync(id)).ReturnsAsync((0, 0, 0, 0));

        var result = await _sut.GetStatsAsync(id);

        Assert.Equal(0, result.ProgressPercent);
    }

    [Fact]
    public async Task UpdateAsync_InvalidStatus_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        _currentUser.Setup(u => u.Role).Returns("AgencyOwner");
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(new Project { Id = id });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.UpdateAsync(id, new UpdateProjectRequest("Name", null, "NotAStatus")));
    }
}
