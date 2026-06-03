# Phase 3B — ProjectHub Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a receive-only SignalR ProjectHub that pushes `OnMilestoneUpdated` and `OnDeliverableUpdated` events to all clients on the project detail page whenever state-changing actions occur.

**Architecture:** `ProjectHub` manages group membership only (no send methods). The three controllers that mutate milestone/deliverable state inject `IHubContext<ProjectHub>` and broadcast after each successful mutation. The frontend `useProjectHub` hook connects on mount, joins the project room, and reacts to events by invalidating TanStack Query caches — triggering automatic refetches of milestones, deliverables, and stats.

**Tech Stack:** ASP.NET Core 8 (SignalR built-in), Next.js 16, TanStack Query, `@microsoft/signalr` (already installed in Phase 3A)

> **No test infrastructure yet** (Phase 6). Verify backend tasks with `dotnet build`. Verify frontend tasks with `npx tsc --noEmit`. Do NOT add "Co-Authored-By" lines to commits.

---

## File Map

| Action | Path |
|---|---|
| **Create** | `FlowDesk.API/Hubs/ProjectHub.cs` |
| **Modify** | `FlowDesk.API/Program.cs` |
| **Modify** | `FlowDesk.API/Controllers/MilestonesController.cs` |
| **Modify** | `FlowDesk.API/Controllers/DeliverablesController.cs` |
| **Create** | `flowdesk-web/src/hooks/useProjectHub.ts` |
| **Modify** | `flowdesk-web/src/app/(dashboard)/dashboard/projects/[id]/page.tsx` |

---

## Task 1: ProjectHub + Program.cs

**Files:**
- Create: `FlowDesk.API/Hubs/ProjectHub.cs`
- Modify: `FlowDesk.API/Program.cs`

- [ ] **Step 1: Create ProjectHub**

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

- [ ] **Step 2: Add MapHub to Program.cs**

Open `FlowDesk.API/Program.cs`. Find:
```csharp
app.MapHub<ChatHub>("/hubs/chat");
```
Replace with:
```csharp
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<ProjectHub>("/hubs/project");
```

- [ ] **Step 3: Build**

```bash
dotnet build FlowDesk.sln
```
Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add FlowDesk.API/Hubs/ProjectHub.cs FlowDesk.API/Program.cs
git commit -m "feat(projecthub): add ProjectHub and map /hubs/project"
```

---

## Task 2: MilestonesController — broadcast on status update

**Files:**
- Modify: `FlowDesk.API/Controllers/MilestonesController.cs`

- [ ] **Step 1: Replace the entire file**

```csharp
// FlowDesk.API/Controllers/MilestonesController.cs
using FlowDesk.API.Hubs;
using FlowDesk.Core.DTOs.Milestones;
using FlowDesk.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace FlowDesk.API.Controllers;

[ApiController]
[Authorize]
public class MilestonesController : ControllerBase
{
    private readonly IMilestoneService _service;
    private readonly IHubContext<ProjectHub> _hub;
    private readonly ILogger<MilestonesController> _logger;

    public MilestonesController(
        IMilestoneService service,
        IHubContext<ProjectHub> hub,
        ILogger<MilestonesController> logger)
    {
        _service = service;
        _hub = hub;
        _logger = logger;
    }

    [HttpGet("api/projects/{projectId:guid}/milestones")]
    public async Task<ActionResult<IEnumerable<MilestoneResponse>>> GetAll(Guid projectId)
        => Ok(await _service.GetAllByProjectAsync(projectId));

    [HttpPost("api/projects/{projectId:guid}/milestones")]
    [Authorize(Policy = "AgencyOnly")]
    public async Task<ActionResult<MilestoneResponse>> Create(
        Guid projectId, [FromBody] CreateMilestoneRequest request)
    {
        var result = await _service.CreateAsync(projectId, request);
        return StatusCode(201, result);
    }

    [HttpPut("api/milestones/{id:guid}")]
    [Authorize(Policy = "AgencyOnly")]
    public async Task<ActionResult<MilestoneResponse>> Update(
        Guid id, [FromBody] UpdateMilestoneRequest request)
        => Ok(await _service.UpdateAsync(id, request));

    [HttpDelete("api/milestones/{id:guid}")]
    [Authorize(Policy = "AgencyOnly")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

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
}
```

- [ ] **Step 2: Build**

```bash
dotnet build FlowDesk.sln
```
Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add FlowDesk.API/Controllers/MilestonesController.cs
git commit -m "feat(projecthub): broadcast OnMilestoneUpdated on status change"
```

---

## Task 3: DeliverablesController — broadcast on confirm/approve/revision

**Files:**
- Modify: `FlowDesk.API/Controllers/DeliverablesController.cs`

- [ ] **Step 1: Replace the entire file**

```csharp
// FlowDesk.API/Controllers/DeliverablesController.cs
using FlowDesk.API.Hubs;
using FlowDesk.Core.DTOs.Deliverables;
using FlowDesk.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace FlowDesk.API.Controllers;

[ApiController]
[Authorize]
public class DeliverablesController : ControllerBase
{
    private readonly IDeliverableService _service;
    private readonly IHubContext<ProjectHub> _hub;
    private readonly ILogger<DeliverablesController> _logger;

    public DeliverablesController(
        IDeliverableService service,
        IHubContext<ProjectHub> hub,
        ILogger<DeliverablesController> logger)
    {
        _service = service;
        _hub = hub;
        _logger = logger;
    }

    [HttpGet("api/projects/{projectId:guid}/deliverables")]
    public async Task<ActionResult<IEnumerable<DeliverableResponse>>> GetAll(Guid projectId)
        => Ok(await _service.GetAllByProjectAsync(projectId));

    [HttpPost("api/projects/{projectId:guid}/deliverables")]
    [Authorize(Policy = "AgencyOnly")]
    public async Task<ActionResult<DeliverableResponse>> Create(
        Guid projectId, [FromBody] CreateDeliverableRequest request)
    {
        var result = await _service.CreateAsync(projectId, request);
        return StatusCode(201, result);
    }

    [HttpPut("api/deliverables/{id:guid}")]
    [Authorize(Policy = "AgencyOnly")]
    public async Task<ActionResult<DeliverableResponse>> Update(
        Guid id, [FromBody] UpdateDeliverableRequest request)
        => Ok(await _service.UpdateAsync(id, request));

    [HttpPost("api/deliverables/{id:guid}/upload-url")]
    [Authorize(Policy = "AgencyOnly")]
    public async Task<ActionResult<UploadUrlResponse>> GetUploadUrl(
        Guid id, [FromBody] GetUploadUrlRequest request)
        => Ok(await _service.GetUploadUrlAsync(id, request.FileName, request.ContentType));

    [HttpPatch("api/deliverables/{id:guid}")]
    [Authorize(Policy = "AgencyOnly")]
    public async Task<ActionResult<DeliverableResponse>> ConfirmUpload(
        Guid id, [FromBody] ConfirmUploadRequest request)
    {
        var result = await _service.ConfirmUploadAsync(id, request.FileUrl);
        await BroadcastDeliverableUpdated(result);
        return Ok(result);
    }

    [HttpPatch("api/deliverables/{id:guid}/approve")]
    [Authorize(Policy = "ClientOnly")]
    public async Task<ActionResult<DeliverableResponse>> Approve(Guid id)
    {
        var result = await _service.ApproveAsync(id);
        await BroadcastDeliverableUpdated(result);
        return Ok(result);
    }

    [HttpPatch("api/deliverables/{id:guid}/revision")]
    [Authorize(Policy = "ClientOnly")]
    public async Task<ActionResult<DeliverableResponse>> RequestRevision(
        Guid id, [FromBody] RevisionRequest request)
    {
        var result = await _service.RequestRevisionAsync(id, request);
        await BroadcastDeliverableUpdated(result);
        return Ok(result);
    }

    private async Task BroadcastDeliverableUpdated(DeliverableResponse result)
    {
        try
        {
            await _hub.Clients.Group($"proj-{result.ProjectId}")
                .SendAsync("OnDeliverableUpdated", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ProjectHub broadcast failed for project {ProjectId}", result.ProjectId);
        }
    }
}
```

- [ ] **Step 2: Build**

```bash
dotnet build FlowDesk.sln
```
Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add FlowDesk.API/Controllers/DeliverablesController.cs
git commit -m "feat(projecthub): broadcast OnDeliverableUpdated on confirm/approve/revision"
```

---

## Task 4: useProjectHub hook

**Files:**
- Create: `flowdesk-web/src/hooks/useProjectHub.ts`

- [ ] **Step 1: Create the hook**

```ts
// flowdesk-web/src/hooks/useProjectHub.ts
"use client";

import { useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import * as signalR from "@microsoft/signalr";

export function useProjectHub(projectId: string) {
  const queryClient = useQueryClient();

  useEffect(() => {
    const url = `${process.env.NEXT_PUBLIC_SIGNALR_URL ?? "http://localhost:5269"}/hubs/project`;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(url, { withCredentials: true })
      .withAutomaticReconnect()
      .build();

    connection.on("OnMilestoneUpdated", () => {
      queryClient.invalidateQueries({ queryKey: ["projects", projectId, "milestones"] });
      queryClient.invalidateQueries({ queryKey: ["projects", projectId, "stats"] });
    });

    connection.on("OnDeliverableUpdated", () => {
      queryClient.invalidateQueries({ queryKey: ["projects", projectId, "deliverables"] });
      queryClient.invalidateQueries({ queryKey: ["projects", projectId, "stats"] });
    });

    connection
      .start()
      .then(() => connection.invoke("JoinProject", projectId))
      .catch(() => {});

    return () => {
      connection.invoke("LeaveProject", projectId).catch(() => {});
      connection.stop();
    };
  }, [projectId, queryClient]);
}
```

- [ ] **Step 2: TypeScript check**

```bash
cd flowdesk-web && npx tsc --noEmit
```
Expected: no errors.

- [ ] **Step 3: Commit**

```bash
git add flowdesk-web/src/hooks/useProjectHub.ts
git commit -m "feat(projecthub): add useProjectHub hook with query invalidation"
```

---

## Task 5: Wire useProjectHub into project detail page

**Files:**
- Modify: `flowdesk-web/src/app/(dashboard)/dashboard/projects/[id]/page.tsx`

- [ ] **Step 1: Add import**

Open `flowdesk-web/src/app/(dashboard)/dashboard/projects/[id]/page.tsx`.

Add to imports (after the existing hook/component imports):
```ts
import { useProjectHub } from "@/hooks/useProjectHub";
```

- [ ] **Step 2: Call the hook**

Inside `ProjectDetailPage`, after the existing hook calls (`useProject`, `useProjectStats`), add:

```ts
useProjectHub(id);
```

The hook has no return value — it just sets up the connection as a side effect.

- [ ] **Step 3: TypeScript check**

```bash
cd flowdesk-web && npx tsc --noEmit
```
Expected: no errors.

- [ ] **Step 4: Manual test**

With the API running (`cd FlowDesk.API && dotnet run`) and frontend running (`cd flowdesk-web && npm run dev`):

1. Open a project detail page in two browser windows
2. In window 1, go to the Milestones tab and click a milestone status badge to cycle it
3. In window 2 (on the same project), the milestone list should update automatically without a page refresh
4. Switch to the Deliverables tab in window 1 and approve a deliverable
5. In window 2, the deliverable status badge should update live

- [ ] **Step 5: Commit**

```bash
git add flowdesk-web/src/app/(dashboard)/dashboard/projects/[id]/page.tsx
git commit -m "feat(projecthub): wire useProjectHub into project detail page"
```

---

## Self-Review (completed)

- [x] **ProjectHub** with `JoinProject`/`LeaveProject`, `[Authorize]`, group name `proj-{projectId}`: Task 1 ✅
- [x] **`app.MapHub<ProjectHub>("/hubs/project")`** in Program.cs: Task 1 ✅
- [x] **MilestonesController.UpdateStatus** broadcasts `OnMilestoneUpdated` to `proj-{result.ProjectId}`: Task 2 ✅
- [x] **DeliverablesController** broadcasts `OnDeliverableUpdated` on ConfirmUpload, Approve, RequestRevision: Task 3 ✅
- [x] **All broadcasts** wrapped in try/catch with logger: Tasks 2 + 3 ✅
- [x] **`useProjectHub`** invalidates milestones+stats on `OnMilestoneUpdated`, deliverables+stats on `OnDeliverableUpdated`: Task 4 ✅
- [x] **Project detail page** calls `useProjectHub(id)`: Task 5 ✅
- [x] **Group name consistency**: `proj-{projectId}` used in hub, both controllers, and hook ✅
- [x] **Event name consistency**: `OnMilestoneUpdated` / `OnDeliverableUpdated` match between server `SendAsync` and client `connection.on` ✅
