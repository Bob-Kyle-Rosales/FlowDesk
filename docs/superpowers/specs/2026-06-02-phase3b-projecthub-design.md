# Phase 3B — ProjectHub Design Spec

**Date:** 2026-06-02
**Scope:** Real-time status push for milestones and deliverables. No new REST endpoints, no DTOs, no database changes.

---

## Architecture

A receive-only SignalR hub. Clients join a project room on mount; the server broadcasts whenever a milestone or deliverable changes state. The frontend reacts by invalidating TanStack Query caches — no client-side cache patching.

```
PATCH /milestones/{id}/status
  → MilestoneService.UpdateStatusAsync()   (existing)
  → MilestonesController broadcasts OnMilestoneUpdated(MilestoneResponse) to "proj-{projectId}"

PATCH /deliverables/{id}          (confirm upload)
PATCH /deliverables/{id}/approve
PATCH /deliverables/{id}/revision
  → DeliverableService.*          (existing)
  → DeliverablesController broadcasts OnDeliverableUpdated(DeliverableResponse) to "proj-{projectId}"

ProjectHub /hubs/project
  JoinProject(projectId)  → add to group "proj-{projectId}"
  LeaveProject(projectId) → remove from group
  (no hub send methods — receive only)
```

`projectId` for the broadcast comes from `result.ProjectId` on the already-returned DTO — no extra query needed.

---

## New Files

```
FlowDesk.API/Hubs/ProjectHub.cs
flowdesk-web/src/hooks/useProjectHub.ts
```

## Modified Files

```
FlowDesk.API/Controllers/MilestonesController.cs    — inject IHubContext<ProjectHub>, broadcast on UpdateStatus
FlowDesk.API/Controllers/DeliverablesController.cs  — inject IHubContext<ProjectHub>, broadcast on ConfirmUpload/Approve/Revision
FlowDesk.API/Program.cs                             — MapHub<ProjectHub>("/hubs/project")
flowdesk-web/src/app/(dashboard)/dashboard/projects/[id]/page.tsx  — call useProjectHub(id)
```

---

## Backend Detail

### ProjectHub

```csharp
// FlowDesk.API/Hubs/ProjectHub.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FlowDesk.API.Hubs;

[Authorize]
public class ProjectHub : Hub
{
    public async Task JoinProject(string projectId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"proj-{projectId}");

    public async Task LeaveProject(string projectId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"proj-{projectId}");
}
```

No membership gate needed here — the project detail page is already auth-guarded by `proxy.ts`, and the hub sits behind `[Authorize]`. If finer-grained access control is needed it can be added later; for now the org-scoped REST data is the security boundary.

### MilestonesController changes

Add `IHubContext<ProjectHub>` and `ILogger<MilestonesController>` to the constructor. Modify `UpdateStatus`:

```csharp
[HttpPatch("api/milestones/{id:guid}/status")]
[Authorize(Policy = "AgencyOnly")]
public async Task<ActionResult<MilestoneResponse>> UpdateStatus(
    Guid id, [FromBody] UpdateMilestoneStatusRequest request)
{
    var result = await _service.UpdateStatusAsync(id, request);
    try
    {
        await _hub.Clients.Group($"proj-{result.ProjectId}")
            .SendAsync("OnMilestoneUpdated", result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "ProjectHub broadcast failed for project {ProjectId}", result.ProjectId);
    }
    return Ok(result);
}
```

### DeliverablesController changes

Add `IHubContext<ProjectHub>` and `ILogger<DeliverablesController>` to the constructor. Modify three actions:

- `ConfirmUpload` — broadcast `OnDeliverableUpdated` after `ConfirmUploadAsync`
- `Approve` — broadcast `OnDeliverableUpdated` after `ApproveAsync`
- `RequestRevision` — broadcast `OnDeliverableUpdated` after `RequestRevisionAsync`

All broadcasts use `$"proj-{result.ProjectId}"` and are wrapped in try/catch with logging.

### Program.cs

Add after the existing `MapHub<ChatHub>`:
```csharp
app.MapHub<ProjectHub>("/hubs/project");
```

---

## Frontend Detail

### useProjectHub hook

```ts
// flowdesk-web/src/hooks/useProjectHub.ts
// Manages ProjectHub connection. On mount: connects, JoinProject.
// OnMilestoneUpdated: invalidates milestones + stats queries.
// OnDeliverableUpdated: invalidates deliverables + stats queries.
// On unmount: LeaveProject + stop.
```

Same lifecycle as `useChatHub` — `HubConnectionBuilder`, `withCredentials: true`, `withAutomaticReconnect()`, useEffect with `[projectId]` dependency.

URL: `NEXT_PUBLIC_SIGNALR_URL + "/hubs/project"`

No state returned — the hook's side effect is query invalidation only.

### Project detail page

Call `useProjectHub(id)` at the top of the component so invalidation fires regardless of which tab is active:

```tsx
// Add after existing hook calls:
useProjectHub(id);
```

Import: `import { useProjectHub } from "@/hooks/useProjectHub";`

---

## Error Handling

- Hub connect fails → `withAutomaticReconnect()` retries silently; UI shows stale data until reconnect
- Broadcast fails in controller → try/catch logs error, REST response still returns 200
- No user-visible error state — the hub is a background enhancement

## Out of Scope

- `OnProgressUpdated` as a separate push event (stats are refetched automatically when milestones/deliverables queries are invalidated)
- Membership gate on `JoinProject` (handled by auth + data-layer org scoping)
- Create/delete notifications for milestones and deliverables (those mutations already invalidate queries locally for the acting user; cross-user push is a later enhancement)
