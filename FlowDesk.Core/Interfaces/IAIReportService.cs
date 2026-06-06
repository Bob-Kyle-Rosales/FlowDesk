namespace FlowDesk.Core.Interfaces;

public interface IAIReportService
{
    IAsyncEnumerable<string> StreamReportAsync(Guid projectId, CancellationToken ct = default);
}
