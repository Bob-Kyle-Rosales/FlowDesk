using FlowDesk.Core.DTOs.Deliverables;
using FlowDesk.Core.Entities;
using FlowDesk.Core.Enums;
using FlowDesk.Core.Interfaces;
using FlowDesk.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FlowDesk.Tests.Services;

public class DeliverableServiceTests
{
    private readonly Mock<IDeliverableRepository> _repo = new();
    private readonly Mock<IProjectRepository> _projectRepo = new();
    private readonly Mock<IFileStorageService> _fileStorage = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly DeliverableService _sut;

    public DeliverableServiceTests()
    {
        _sut = new DeliverableService(
            _repo.Object,
            _projectRepo.Object,
            _fileStorage.Object,
            _currentUser.Object,
            NullLogger<DeliverableService>.Instance);
    }

    [Fact]
    public async Task ConfirmUploadAsync_PendingStatus_TransitionsToUnderReview()
    {
        var id = Guid.NewGuid();
        var deliverable = new Deliverable { Id = id, Status = DeliverableStatus.Pending };
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(deliverable);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<Deliverable>())).Returns(Task.CompletedTask);

        var result = await _sut.ConfirmUploadAsync(id, "https://cdn.example.com/file.pdf");

        Assert.Equal(DeliverableStatus.UnderReview.ToString(), result.Status);
        Assert.Equal("https://cdn.example.com/file.pdf", result.FileUrl);
    }

    [Fact]
    public async Task ConfirmUploadAsync_RevisionStatus_TransitionsToUnderReview()
    {
        var id = Guid.NewGuid();
        var deliverable = new Deliverable { Id = id, Status = DeliverableStatus.Revision };
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(deliverable);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<Deliverable>())).Returns(Task.CompletedTask);

        var result = await _sut.ConfirmUploadAsync(id, "https://cdn.example.com/v2.pdf");

        Assert.Equal(DeliverableStatus.UnderReview.ToString(), result.Status);
    }

    [Fact]
    public async Task ConfirmUploadAsync_ApprovedStatus_StatusUnchanged()
    {
        var id = Guid.NewGuid();
        var deliverable = new Deliverable { Id = id, Status = DeliverableStatus.Approved };
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(deliverable);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<Deliverable>())).Returns(Task.CompletedTask);

        var result = await _sut.ConfirmUploadAsync(id, "https://cdn.example.com/final.pdf");

        Assert.Equal(DeliverableStatus.Approved.ToString(), result.Status);
    }

    [Fact]
    public async Task ApproveAsync_StatusNotUnderReview_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var deliverable = new Deliverable { Id = id, ProjectId = projectId, Status = DeliverableStatus.Pending };

        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(deliverable);
        _projectRepo.Setup(r => r.GetByIdAsync(projectId))
            .ReturnsAsync(new Project { Id = projectId, ClientId = callerId });
        _currentUser.Setup(u => u.UserId).Returns(callerId);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ApproveAsync(id));
    }

    [Fact]
    public async Task ApproveAsync_WrongClient_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var deliverable = new Deliverable { Id = id, ProjectId = projectId, Status = DeliverableStatus.UnderReview };

        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(deliverable);
        _projectRepo.Setup(r => r.GetByIdAsync(projectId))
            .ReturnsAsync(new Project { Id = projectId, ClientId = Guid.NewGuid() });
        _currentUser.Setup(u => u.UserId).Returns(callerId);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.ApproveAsync(id));
    }

    [Fact]
    public async Task ApproveAsync_ValidClient_SetsApprovedStatusAndApprovedById()
    {
        var id = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var deliverable = new Deliverable { Id = id, ProjectId = projectId, Status = DeliverableStatus.UnderReview };

        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(deliverable);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<Deliverable>())).Returns(Task.CompletedTask);
        _projectRepo.Setup(r => r.GetByIdAsync(projectId))
            .ReturnsAsync(new Project { Id = projectId, ClientId = callerId });
        _currentUser.Setup(u => u.UserId).Returns(callerId);

        var result = await _sut.ApproveAsync(id);

        Assert.Equal(DeliverableStatus.Approved.ToString(), result.Status);
        Assert.Equal(callerId, deliverable.ApprovedById);
        Assert.NotNull(deliverable.ApprovedAt);
    }

    [Fact]
    public async Task RequestRevisionAsync_StatusNotUnderReview_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var deliverable = new Deliverable { Id = id, ProjectId = projectId, Status = DeliverableStatus.Pending };

        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(deliverable);
        _projectRepo.Setup(r => r.GetByIdAsync(projectId))
            .ReturnsAsync(new Project { Id = projectId, ClientId = callerId });
        _currentUser.Setup(u => u.UserId).Returns(callerId);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.RequestRevisionAsync(id, new RevisionRequest("Needs more polish")));
    }

    [Fact]
    public async Task RequestRevisionAsync_ValidClient_SetsRevisionStatusAndIncrementsVersion()
    {
        var id = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var deliverable = new Deliverable
            { Id = id, ProjectId = projectId, Status = DeliverableStatus.UnderReview, Version = 1 };

        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(deliverable);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<Deliverable>())).Returns(Task.CompletedTask);
        _projectRepo.Setup(r => r.GetByIdAsync(projectId))
            .ReturnsAsync(new Project { Id = projectId, ClientId = callerId });
        _currentUser.Setup(u => u.UserId).Returns(callerId);

        var result = await _sut.RequestRevisionAsync(id, new RevisionRequest("Needs more polish"));

        Assert.Equal(DeliverableStatus.Revision.ToString(), result.Status);
        Assert.Equal("Needs more polish", result.RevisionNotes);
        Assert.Equal(2, result.Version);
    }
}
