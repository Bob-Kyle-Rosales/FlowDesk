namespace FlowDesk.Core.DTOs.Projects;

public record ProjectStatsResponse(
    int ProgressPercent,
    int MilestoneCount,
    int CompletedMilestones,
    int DeliverableCount,
    int ApprovedDeliverables);
