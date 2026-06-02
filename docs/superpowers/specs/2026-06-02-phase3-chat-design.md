# Phase 3A — Real-Time Chat Design Spec

**Date:** 2026-06-02
**Scope:** Message persistence (REST API), SignalR ChatHub, and frontend chat UI. ProjectHub (live milestone/deliverable push) and file attachments are out of scope — separate follow-on tasks.

---

## Architecture

REST handles writes; SignalR handles real-time delivery.

```
POST /api/projects/{id}/messages
  → MessageService.CreateAsync()
    → MessageRepository.CreateAsync()       (persists to DB)
    → IHubContext<ChatHub>.Clients          (broadcasts ReceiveMessage to group)
  → returns MessageResponse

GET /api/projects/{id}/messages
  → last 50 messages, oldest→newest

PATCH /api/projects/{id}/messages/read
  → marks all messages as read for the caller

ChatHub /hubs/chat
  JoinProject(projectId)  → add to group "project-{id}", call MarkReadAsync
  LeaveProject(projectId) → remove from group
  Client receives: ReceiveMessage(MessageResponse)
```

JWT auth applies to both REST and SignalR. The `access_token` cookie is included on the WebSocket upgrade handshake automatically by the browser; the hub is protected via `[Authorize]`.

---

## New Files

### Backend

```
FlowDesk.Core/
  DTOs/Messages/
    MessageResponse.cs
    CreateMessageRequest.cs
  Interfaces/
    IMessageRepository.cs
    IMessageService.cs
  Services/
    MessageService.cs

FlowDesk.Infrastructure/
  Repositories/
    MessageRepository.cs

FlowDesk.API/
  Controllers/
    MessagesController.cs
  Hubs/
    ChatHub.cs
  Validators/Messages/
    CreateMessageRequestValidator.cs
```

### Modified Backend

```
FlowDesk.API/Program.cs    — AddSignalR(), MapHub<ChatHub>("/hubs/chat")
```

### Frontend

```
flowdesk-web/src/
  hooks/
    useChatHub.ts
  components/projects/
    MessagesTab.tsx
```

### Modified Frontend

```
flowdesk-web/src/lib/queries.ts        — add useMessages, useSendMessage, useMarkMessagesRead
flowdesk-web/src/types/index.ts        — add Message interface
flowdesk-web/src/app/(dashboard)/dashboard/projects/[id]/page.tsx
                                       — replace stub with <MessagesTab projectId={id} />
```

---

## DTO Shapes

### MessageResponse
```csharp
record MessageResponse(
    Guid Id,
    string Content,
    Guid SenderId,
    string SenderName,
    bool IsRead,
    DateTime CreatedAt
);
```

### CreateMessageRequest
```csharp
record CreateMessageRequest(string Content);
```

### TypeScript Message type
```ts
export interface Message {
  id: string;
  content: string;
  senderId: string;
  senderName: string;
  isRead: boolean;
  createdAt: string;
}
```

---

## Backend Detail

### IMessageRepository
```csharp
Task<IEnumerable<Message>> GetByProjectAsync(Guid projectId, int limit = 50);
Task<Message> CreateAsync(Message message);
Task MarkReadAsync(Guid projectId, Guid currentUserId);
```

`GetByProjectAsync` is covered by the EF global org query filter via `Project` navigation. It orders by `CreatedAt ASC` and takes the last `limit` rows.

### MessageService
- `GetByProjectAsync(projectId)` — calls repo, maps to `MessageResponse[]`
- `CreateAsync(projectId, request)` — validates project access, creates `Message` entity, calls `IHubContext<ChatHub>` to broadcast `ReceiveMessage` to group `project-{projectId}`
- `MarkReadAsync(projectId)` — delegates to repo

### MessagesController
```
[Authorize]
GET  /api/projects/{projectId}/messages        → AgencyOnly + ClientOnly (any authenticated user on the project)
POST /api/projects/{projectId}/messages        → AgencyOnly + ClientOnly
PATCH /api/projects/{projectId}/messages/read  → AgencyOnly + ClientOnly
```

All three endpoints use `[Authorize]` without a policy — any authenticated user on the project can read and send messages.

### ChatHub
```csharp
[Authorize]
public class ChatHub : Hub
{
    public async Task JoinProject(string projectId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"project-{projectId}");
        // Mark all messages as read for this user
        await _messageService.MarkReadAsync(Guid.Parse(projectId));
    }

    public async Task LeaveProject(string projectId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"project-{projectId}");
    }
}
```

The hub does not have a `SendMessage` method — sending goes through REST. The hub only manages group membership and read receipts.

### Program.cs additions
```csharp
builder.Services.AddSignalR();
// ...after app.Build():
app.MapHub<ChatHub>("/hubs/chat");
```

SignalR JWT: configure the `JwtBearerEvents` to also read the token from the query string for SignalR connections (browsers send the WebSocket upgrade without custom headers):

```csharp
options.Events = new JwtBearerEvents
{
    OnMessageReceived = ctx =>
    {
        // existing cookie read
        ctx.Token = ctx.Request.Cookies["access_token"];
        // SignalR WebSocket fallback
        if (string.IsNullOrEmpty(ctx.Token))
        {
            var accessToken = ctx.Request.Query["access_token"];
            var path = ctx.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                ctx.Token = accessToken;
        }
        return Task.CompletedTask;
    }
};
```

### FluentValidation
```csharp
// CreateMessageRequestValidator
RuleFor(x => x.Content).NotEmpty().MaximumLength(4000);
```

### NuGet — no new packages needed
`Microsoft.AspNetCore.SignalR` is included in the ASP.NET Core 8 shared framework — no additional NuGet reference required.

---

## Frontend Detail

### npm dependency
```
@microsoft/signalr
```

### useMessages (TanStack Query)
```ts
export function useMessages(projectId: string) {
  return useQuery<Message[]>({
    queryKey: ["projects", projectId, "messages"],
    queryFn: () => api.get(`/api/projects/${projectId}/messages`).then(r => r.data),
  });
}

export function useSendMessage(projectId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (content: string) =>
      api.post<Message>(`/api/projects/${projectId}/messages`, { content }).then(r => r.data),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: ["projects", projectId, "messages"] }),
  });
}

export function useMarkMessagesRead(projectId: string) {
  return useMutation({
    mutationFn: () => api.patch(`/api/projects/${projectId}/messages/read`),
  });
}
```

### useChatHub hook
```ts
// src/hooks/useChatHub.ts
// Manages HubConnection lifecycle.
// Returns: { liveMessages, connectionState }
// liveMessages: MessageResponse[] received in real-time since mount (prepended to history)
// connectionState: "connecting" | "connected" | "disconnected"
```

Behaviour:
- Creates a `HubConnectionBuilder` pointing at `NEXT_PUBLIC_SIGNALR_URL + "/hubs/chat"`
- Uses `withAutomaticReconnect()` 
- On connect: calls `JoinProject(projectId)`
- On unmount (or tab change): calls `LeaveProject(projectId)`, then `.stop()`
- Listens for `ReceiveMessage` and appends incoming messages to local `liveMessages` state
- History is loaded separately via `useMessages()` — `useChatHub` only handles new arrivals

### MessagesTab component
Props: `{ projectId: string }`

Layout:
```
┌─────────────────────────────────┐
│  [connection status indicator]  │
├─────────────────────────────────┤
│                                 │
│   [message list — scrollable]   │
│                                 │
│   Other: "Hey, how's it going?" │
│              "Almost done!" :You│
│                                 │
├─────────────────────────────────┤
│  [text input]       [Send]      │
└─────────────────────────────────┘
```

- Fetches history via `useMessages(projectId)` on mount
- Real-time additions via `useChatHub(projectId).liveMessages`
- Displayed list = history + liveMessages (deduped by `id`)
- Own messages (senderId === user.id) right-aligned, others left-aligned
- Auto-scroll to bottom on new message
- Send button disabled while input is empty or `useSendMessage` is pending
- On send: call `useSendMessage`, clear input, invalidate messages query
- "Connecting…" indicator shown when `connectionState !== "connected"`
- Empty state: "No messages yet. Start the conversation."

### Connection to project detail page

Replace in `projects/[id]/page.tsx`:
```tsx
// before:
<TabsContent value="messages">
  <div className="py-16 text-center">
    <p className="text-sm text-muted-foreground">Real-time messaging coming in Phase 3.</p>
  </div>
</TabsContent>

// after:
<TabsContent value="messages">
  <MessagesTab projectId={id} />
</TabsContent>
```

---

## Read Receipts

`IsRead` is marked automatically when a user **joins the project room** (calls `JoinProject` on the hub). No explicit user action required. This maps to opening the Messages tab.

For the frontend, unread counts are not surfaced in Phase 3 (no badge on the tab). The `IsRead` field is persisted and available for Phase 5 (client portal) to use for notification dots.

---

## Error Handling

| Scenario | Behaviour |
|---|---|
| Hub connection fails | Retry via `withAutomaticReconnect()`; show "Connecting…" indicator |
| Send fails (REST) | Toast error; input text preserved |
| Load history fails | Inline error state in MessagesTab |
| User not on project | `MessageService` throws `KeyNotFoundException` → 404 |

---

## Message Pagination

Last 50 messages loaded on initial fetch. No pagination in Phase 3.

---

## Out of Scope

- File attachments in messages (Phase 3C)
- ProjectHub — live milestone/deliverable status push (Phase 3B)
- Unread message badges/counts on tabs or sidebar (Phase 5)
- Message editing or deletion
- Typing indicators
