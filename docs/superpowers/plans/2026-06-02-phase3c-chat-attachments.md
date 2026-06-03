# Phase 3C — Chat File Attachments Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Allow agency users to attach a file when sending a chat message, uploaded directly to Cloudflare R2 via a presigned URL, displayed as a download link in the conversation.

**Architecture:** Pre-upload pattern — file uploads to R2 before Send is clicked; `fileUrl` is held in local state then included in the `POST /messages` body. The R2 upload reuses `IFileStorageService` after generalising its interface from `Guid deliverableId` to `string folderPath`. Text remains required alongside the file.

**Tech Stack:** ASP.NET Core 8, EF Core 8, Cloudflare R2 (AWS S3 SDK), Next.js 16, TanStack Query, XHR for upload progress, lucide-react (Paperclip, X icons)

> **No test infrastructure yet** (Phase 6). Verify backend tasks with `dotnet build FlowDesk.sln`. Verify frontend tasks with `cd flowdesk-web && npx tsc --noEmit`. Do NOT add "Co-Authored-By" lines to commits.

---

## File Map

| Action | Path |
|---|---|
| **Modify** | `FlowDesk.Core/Interfaces/IFileStorageService.cs` |
| **Modify** | `FlowDesk.Infrastructure/Services/FileStorageService.cs` |
| **Modify** | `FlowDesk.Core/Services/DeliverableService.cs` |
| **Modify** | `FlowDesk.Core/DTOs/Messages/CreateMessageRequest.cs` |
| **Create** | `FlowDesk.Core/DTOs/Messages/GetMessageUploadUrlRequest.cs` |
| **Modify** | `FlowDesk.API/Validators/Messages/CreateMessageRequestValidator.cs` |
| **Modify** | `FlowDesk.Core/Services/MessageService.cs` |
| **Modify** | `FlowDesk.API/Controllers/MessagesController.cs` |
| **Modify** | `flowdesk-web/src/types/index.ts` |
| **Modify** | `flowdesk-web/src/lib/queries.ts` |
| **Modify** | `flowdesk-web/src/components/projects/MessagesTab.tsx` |

---

## Task 1: Generalise IFileStorageService

The current interface takes `Guid deliverableId` — too specific. Change it to `string folderPath` so messages can use a different path prefix.

**Files:**
- Modify: `FlowDesk.Core/Interfaces/IFileStorageService.cs`
- Modify: `FlowDesk.Infrastructure/Services/FileStorageService.cs`
- Modify: `FlowDesk.Core/Services/DeliverableService.cs`

- [ ] **Step 1: Update IFileStorageService**

```csharp
// FlowDesk.Core/Interfaces/IFileStorageService.cs
namespace FlowDesk.Core.Interfaces;

public interface IFileStorageService
{
    Task<(string UploadUrl, string FileUrl)> GenerateUploadUrlAsync(
        string folderPath, string fileName, string contentType);
}
```

- [ ] **Step 2: Update FileStorageService**

```csharp
// FlowDesk.Infrastructure/Services/FileStorageService.cs
using Amazon.S3;
using Amazon.S3.Model;
using FlowDesk.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FlowDesk.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private readonly Lazy<(IAmazonS3 S3, string Bucket, string PublicUrl)> _r2;

    public FileStorageService(IConfiguration configuration)
    {
        _r2 = new Lazy<(IAmazonS3, string, string)>(() =>
        {
            var endpoint = configuration["CLOUDFLARE_R2_ENDPOINT"]
                ?? throw new InvalidOperationException("CLOUDFLARE_R2_ENDPOINT is not set.");
            var bucket = configuration["CLOUDFLARE_R2_BUCKET"]
                ?? throw new InvalidOperationException("CLOUDFLARE_R2_BUCKET is not set.");
            var publicUrl = configuration["CLOUDFLARE_R2_PUBLIC_URL"]
                ?? throw new InvalidOperationException("CLOUDFLARE_R2_PUBLIC_URL is not set.");
            var accessKey = configuration["CLOUDFLARE_R2_ACCESS_KEY"]
                ?? throw new InvalidOperationException("CLOUDFLARE_R2_ACCESS_KEY is not set.");
            var secretKey = configuration["CLOUDFLARE_R2_SECRET_KEY"]
                ?? throw new InvalidOperationException("CLOUDFLARE_R2_SECRET_KEY is not set.");

            var s3 = new AmazonS3Client(accessKey, secretKey, new AmazonS3Config
            {
                ServiceURL = endpoint,
                ForcePathStyle = true
            });

            return (s3, bucket, publicUrl);
        });
    }

    public Task<(string UploadUrl, string FileUrl)> GenerateUploadUrlAsync(
        string folderPath, string fileName, string contentType)
    {
        var (s3, bucket, publicUrl) = _r2.Value;

        var safeName = Path.GetFileName(fileName);
        var key = $"{folderPath}/{safeName}";

        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucket,
            Key = key,
            Verb = HttpVerb.PUT,
            ContentType = contentType,
            Expires = DateTime.UtcNow.AddMinutes(15)
        };

        var uploadUrl = s3.GetPreSignedURL(request);
        var fileUrl = $"{publicUrl.TrimEnd('/')}/{key}";

        return Task.FromResult((uploadUrl, fileUrl));
    }
}
```

- [ ] **Step 3: Fix DeliverableService call site**

Open `FlowDesk.Core/Services/DeliverableService.cs`. Find:
```csharp
        var (uploadUrl, fileUrl) = await _fileStorage.GenerateUploadUrlAsync(
            deliverable.Id, fileName, contentType);
```
Replace with:
```csharp
        var (uploadUrl, fileUrl) = await _fileStorage.GenerateUploadUrlAsync(
            $"deliverables/{deliverable.Id}", fileName, contentType);
```

- [ ] **Step 4: Build**

```bash
dotnet build FlowDesk.sln
```
Expected: Build succeeded, 0 errors.

- [ ] **Step 5: Commit**

```bash
git add FlowDesk.Core/Interfaces/IFileStorageService.cs \
        FlowDesk.Infrastructure/Services/FileStorageService.cs \
        FlowDesk.Core/Services/DeliverableService.cs
git commit -m "refactor(storage): generalise IFileStorageService to use folderPath instead of deliverableId"
```

---

## Task 2: Backend DTOs, validator, MessageService

**Files:**
- Modify: `FlowDesk.Core/DTOs/Messages/CreateMessageRequest.cs`
- Create: `FlowDesk.Core/DTOs/Messages/GetMessageUploadUrlRequest.cs`
- Modify: `FlowDesk.API/Validators/Messages/CreateMessageRequestValidator.cs`
- Modify: `FlowDesk.Core/Services/MessageService.cs`

- [ ] **Step 1: Update CreateMessageRequest**

```csharp
// FlowDesk.Core/DTOs/Messages/CreateMessageRequest.cs
namespace FlowDesk.Core.DTOs.Messages;

public record CreateMessageRequest(string Content, string? FileUrl);
```

- [ ] **Step 2: Create GetMessageUploadUrlRequest**

```csharp
// FlowDesk.Core/DTOs/Messages/GetMessageUploadUrlRequest.cs
namespace FlowDesk.Core.DTOs.Messages;

public record GetMessageUploadUrlRequest(string FileName, string ContentType);
```

- [ ] **Step 3: Update validator**

```csharp
// FlowDesk.API/Validators/Messages/CreateMessageRequestValidator.cs
using FlowDesk.Core.DTOs.Messages;
using FluentValidation;

namespace FlowDesk.API.Validators.Messages;

public class CreateMessageRequestValidator : AbstractValidator<CreateMessageRequest>
{
    public CreateMessageRequestValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.FileUrl).MaximumLength(2000).When(x => x.FileUrl is not null);
    }
}
```

- [ ] **Step 4: Update MessageService.CreateAsync to pass FileUrl**

Open `FlowDesk.Core/Services/MessageService.cs`. Find:
```csharp
        var message = new Message
        {
            Id = Guid.NewGuid(),
            Content = request.Content,
            ProjectId = projectId,
            SenderId = _currentUser.UserId!.Value,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
```
Replace with:
```csharp
        var message = new Message
        {
            Id = Guid.NewGuid(),
            Content = request.Content,
            FileUrl = request.FileUrl,
            ProjectId = projectId,
            SenderId = _currentUser.UserId!.Value,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
```

- [ ] **Step 5: Update MessageResponse to include FileUrl**

Open `FlowDesk.Core/DTOs/Messages/MessageResponse.cs`. Replace the entire file:

```csharp
// FlowDesk.Core/DTOs/Messages/MessageResponse.cs
namespace FlowDesk.Core.DTOs.Messages;

public record MessageResponse(
    Guid Id,
    string Content,
    Guid SenderId,
    string SenderName,
    bool IsRead,
    DateTime CreatedAt,
    string? FileUrl);
```

- [ ] **Step 6: Update MessageService.ToResponse to map FileUrl**

Open `FlowDesk.Core/Services/MessageService.cs`. Find:
```csharp
    private static MessageResponse ToResponse(Message m) => new(
        m.Id,
        m.Content,
        m.SenderId,
        m.Sender?.Name ?? string.Empty,
        m.IsRead,
        m.CreatedAt);
```
Replace with:
```csharp
    private static MessageResponse ToResponse(Message m) => new(
        m.Id,
        m.Content,
        m.SenderId,
        m.Sender?.Name ?? string.Empty,
        m.IsRead,
        m.CreatedAt,
        m.FileUrl);
```

- [ ] **Step 7: Build**

```bash
dotnet build FlowDesk.sln
```
Expected: Build succeeded, 0 errors.

- [ ] **Step 8: Commit**

```bash
git add FlowDesk.Core/DTOs/Messages/CreateMessageRequest.cs \
        FlowDesk.Core/DTOs/Messages/GetMessageUploadUrlRequest.cs \
        FlowDesk.Core/DTOs/Messages/MessageResponse.cs \
        FlowDesk.API/Validators/Messages/CreateMessageRequestValidator.cs \
        FlowDesk.Core/Services/MessageService.cs
git commit -m "feat(messages): add FileUrl to CreateMessageRequest and MessageResponse"
```

---

## Task 3: MessagesController — upload-url endpoint

**Files:**
- Modify: `FlowDesk.API/Controllers/MessagesController.cs`

- [ ] **Step 1: Replace the entire file**

```csharp
// FlowDesk.API/Controllers/MessagesController.cs
using FlowDesk.API.Hubs;
using FlowDesk.Core.DTOs.Deliverables;
using FlowDesk.Core.DTOs.Messages;
using FlowDesk.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace FlowDesk.API.Controllers;

[ApiController]
[Route("api/projects/{projectId:guid}/messages")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _service;
    private readonly IHubContext<ChatHub> _hub;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(
        IMessageService service,
        IHubContext<ChatHub> hub,
        IFileStorageService fileStorage,
        ILogger<MessagesController> logger)
    {
        _service = service;
        _hub = hub;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MessageResponse>>> GetAll(Guid projectId)
        => Ok(await _service.GetAllAsync(projectId));

    [HttpPost]
    public async Task<ActionResult<MessageResponse>> Send(
        Guid projectId, [FromBody] CreateMessageRequest request)
    {
        var message = await _service.CreateAsync(projectId, request);
        try
        {
            await _hub.Clients.Group($"project-{projectId}").SendAsync("ReceiveMessage", message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SignalR broadcast failed for project {ProjectId}", projectId);
        }
        return CreatedAtAction(nameof(GetAll), new { projectId }, message);
    }

    [HttpPost("upload-url")]
    [Authorize(Policy = "AgencyOnly")]
    public async Task<ActionResult<UploadUrlResponse>> GetUploadUrl(
        Guid projectId, [FromBody] GetMessageUploadUrlRequest request)
    {
        var (uploadUrl, fileUrl) = await _fileStorage.GenerateUploadUrlAsync(
            $"messages/{projectId}", request.FileName, request.ContentType);
        return Ok(new UploadUrlResponse(uploadUrl, fileUrl));
    }

    [HttpPatch("read")]
    public async Task<IActionResult> MarkRead(Guid projectId)
    {
        await _service.MarkReadAsync(projectId);
        return NoContent();
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
git add FlowDesk.API/Controllers/MessagesController.cs
git commit -m "feat(messages): add upload-url endpoint for chat attachments"
```

---

## Task 4: Frontend — types and query hooks

**Files:**
- Modify: `flowdesk-web/src/types/index.ts`
- Modify: `flowdesk-web/src/lib/queries.ts`

- [ ] **Step 1: Update Message type and related interfaces in types/index.ts**

In `flowdesk-web/src/types/index.ts`, find the Messages section:
```ts
// ── Messages ──────────────────────────────────────────────────────────────────
export interface Message {
  id: string;
  content: string;
  senderId: string;
  senderName: string;
  isRead: boolean;
  createdAt: string;
}

export interface CreateMessageRequest {
  content: string;
}
```
Replace with:
```ts
// ── Messages ──────────────────────────────────────────────────────────────────
export interface Message {
  id: string;
  content: string;
  senderId: string;
  senderName: string;
  isRead: boolean;
  createdAt: string;
  fileUrl: string | null;
}

export interface CreateMessageRequest {
  content: string;
  fileUrl?: string | null;
}

export interface GetMessageUploadUrlRequest {
  fileName: string;
  contentType: string;
}
```

- [ ] **Step 2: Update useSendMessage and add useMessageUploadUrl in queries.ts**

In `flowdesk-web/src/lib/queries.ts`, find:
```ts
export function useSendMessage(projectId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (content: string) =>
      api.post<Message>(`/api/projects/${projectId}/messages`, { content }).then(r => r.data),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: ["projects", projectId, "messages"] }),
  });
}
```
Replace with:
```ts
export function useSendMessage(projectId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ content, fileUrl }: { content: string; fileUrl?: string | null }) =>
      api.post<Message>(`/api/projects/${projectId}/messages`, { content, fileUrl }).then(r => r.data),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: ["projects", projectId, "messages"] }),
  });
}

export function useMessageUploadUrl(projectId: string) {
  return useMutation({
    mutationFn: ({ fileName, contentType }: { fileName: string; contentType: string }) =>
      api.post<UploadUrlResponse>(
        `/api/projects/${projectId}/messages/upload-url`,
        { fileName, contentType }
      ).then(r => r.data),
  });
}
```

Also add `UploadUrlResponse` to the import at the top of queries.ts. Find:
```ts
import type {
  Project, ProjectStatsResponse,
  Milestone, CreateMilestoneRequest,
  Deliverable, CreateDeliverableRequest, UploadUrlResponse,
  OrganisationResponse, UpdateOrganisationRequest,
  UserSummary, CreateProjectRequest,
  Message,
} from "@/types";
```
Add `GetMessageUploadUrlRequest,` to that import list (even though it's not used directly in queries.ts, confirming the type is exported is sufficient — actually `useMessageUploadUrl` uses inline types, so this is optional. Do NOT add it if it would cause a "declared but never read" lint error).

- [ ] **Step 3: TypeScript check**

```bash
cd flowdesk-web && npx tsc --noEmit
```
Expected: no errors.

- [ ] **Step 4: Commit**

```bash
git add flowdesk-web/src/types/index.ts flowdesk-web/src/lib/queries.ts
git commit -m "feat(messages): add fileUrl to Message type, update useSendMessage, add useMessageUploadUrl"
```

---

## Task 5: MessagesTab — file picker, upload, chip, link rendering

**Files:**
- Modify: `flowdesk-web/src/components/projects/MessagesTab.tsx`

- [ ] **Step 1: Replace the entire file**

```tsx
// flowdesk-web/src/components/projects/MessagesTab.tsx
"use client";

import { useState, useEffect, useRef } from "react";
import { useAuth } from "@/contexts/AuthContext";
import { useMessages, useSendMessage, useMarkMessagesRead, useMessageUploadUrl } from "@/lib/queries";
import { useChatHub } from "@/hooks/useChatHub";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { toast } from "sonner";
import { Paperclip, X } from "lucide-react";
import type { Message } from "@/types";

export function MessagesTab({ projectId }: { projectId: string }) {
  const { user } = useAuth();
  const { data: history = [], isLoading, isError } = useMessages(projectId);
  const { liveMessages, connectionState } = useChatHub(projectId);
  const sendMessage = useSendMessage(projectId);
  const markRead = useMarkMessagesRead(projectId);
  const getUploadUrl = useMessageUploadUrl(projectId);

  const [input, setInput] = useState("");
  const [pendingFile, setPendingFile] = useState<File | null>(null);
  const [pendingFileUrl, setPendingFileUrl] = useState<string | null>(null);
  const [uploadProgress, setUploadProgress] = useState(0);
  const [isUploading, setIsUploading] = useState(false);
  const bottomRef = useRef<HTMLDivElement>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const allMessages: Message[] = [
    ...history,
    ...liveMessages.filter(lm => !history.some(h => h.id === lm.id)),
  ].sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime());

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [allMessages.length]);

  useEffect(() => {
    if (liveMessages.length > 0) {
      markRead.mutate();
    }
  }, [liveMessages.length]);

  async function handleFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file) return;

    setPendingFile(file);
    setIsUploading(true);
    setUploadProgress(0);

    try {
      const { uploadUrl, fileUrl } = await getUploadUrl.mutateAsync({
        fileName: file.name,
        contentType: file.type || "application/octet-stream",
      });

      await new Promise<void>((resolve, reject) => {
        const xhr = new XMLHttpRequest();
        xhr.upload.onprogress = ev => {
          if (ev.lengthComputable) setUploadProgress(Math.round((ev.loaded / ev.total) * 100));
        };
        xhr.onload = () => (xhr.status === 200 ? resolve() : reject(new Error("Upload failed")));
        xhr.onerror = () => reject(new Error("Upload failed"));
        xhr.open("PUT", uploadUrl);
        xhr.setRequestHeader("Content-Type", file.type || "application/octet-stream");
        xhr.send(file);
      });

      setPendingFileUrl(fileUrl);
    } catch {
      toast.error("File upload failed");
      setPendingFile(null);
      setPendingFileUrl(null);
    } finally {
      setIsUploading(false);
      if (fileInputRef.current) fileInputRef.current.value = "";
    }
  }

  function clearPendingFile() {
    setPendingFile(null);
    setPendingFileUrl(null);
    setUploadProgress(0);
    if (fileInputRef.current) fileInputRef.current.value = "";
  }

  async function handleSend() {
    const content = input.trim();
    if (!content) return;
    setInput("");
    const fileUrl = pendingFileUrl;
    clearPendingFile();
    try {
      await sendMessage.mutateAsync({ content, fileUrl });
    } catch {
      toast.error("Failed to send message");
      setInput(content);
    }
  }

  function handleKeyDown(e: React.KeyboardEvent<HTMLInputElement>) {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  }

  const isAgency = user?.role !== "Client";

  if (isLoading) {
    return (
      <div className="py-16 text-center">
        <p className="text-sm text-muted-foreground animate-pulse">Loading messages…</p>
      </div>
    );
  }

  if (isError) {
    return (
      <div className="py-16 text-center">
        <p className="text-sm text-destructive">Could not load messages.</p>
      </div>
    );
  }

  return (
    <div className="flex flex-col h-[500px]">
      {connectionState !== "connected" && (
        <div className="px-4 py-1.5 text-xs text-muted-foreground bg-muted text-center">
          {connectionState === "connecting" ? "Connecting…" : "Disconnected — reconnecting…"}
        </div>
      )}

      <div className="flex-1 overflow-y-auto p-4 space-y-3">
        {allMessages.length === 0 && (
          <p className="text-sm text-muted-foreground text-center py-12">
            No messages yet. Start the conversation.
          </p>
        )}
        {allMessages.map(message => {
          const isOwn = message.senderId === user?.userId;
          return (
            <div
              key={message.id}
              className={`flex flex-col gap-0.5 ${isOwn ? "items-end" : "items-start"}`}
            >
              <span className="text-xs text-muted-foreground px-1">{message.senderName}</span>
              <div className={`max-w-[75%] rounded-2xl px-3 py-2 text-sm ${
                isOwn
                  ? "bg-violet-600 text-white rounded-br-sm"
                  : "bg-muted text-foreground rounded-bl-sm"
              }`}>
                {message.content}
              </div>
              {message.fileUrl && (
                <a
                  href={message.fileUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="text-xs underline text-muted-foreground hover:text-foreground px-1"
                >
                  📎 Download attachment
                </a>
              )}
              <span className="text-xs text-muted-foreground px-1">
                {new Date(message.createdAt).toLocaleTimeString([], {
                  hour: "2-digit",
                  minute: "2-digit",
                })}
              </span>
            </div>
          );
        })}
        <div ref={bottomRef} />
      </div>

      {pendingFile && (
        <div className="px-4 py-2 border-t bg-muted/50 space-y-1.5">
          <div className="flex items-center gap-2">
            <Paperclip className="size-3.5 text-muted-foreground shrink-0" />
            <span className="text-xs text-muted-foreground truncate flex-1">{pendingFile.name}</span>
            {isUploading ? (
              <span className="text-xs text-muted-foreground shrink-0">{uploadProgress}%</span>
            ) : (
              <button
                type="button"
                onClick={clearPendingFile}
                className="text-muted-foreground hover:text-foreground shrink-0"
                aria-label="Remove attachment"
              >
                <X className="size-3.5" />
              </button>
            )}
          </div>
          {isUploading && (
            <div className="h-1 bg-muted rounded-full overflow-hidden">
              <div
                className="h-full bg-violet-500 transition-all duration-200"
                style={{ width: `${uploadProgress}%` }}
              />
            </div>
          )}
        </div>
      )}

      <div className="p-4 border-t flex gap-2">
        {isAgency && (
          <>
            <input
              type="file"
              ref={fileInputRef}
              className="hidden"
              onChange={handleFileChange}
            />
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={() => fileInputRef.current?.click()}
              disabled={isUploading || !!pendingFile}
              className="px-2"
              aria-label="Attach file"
            >
              <Paperclip className="size-4" />
            </Button>
          </>
        )}
        <Input
          value={input}
          onChange={e => setInput(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder="Type a message…"
          className="flex-1"
        />
        <Button
          onClick={handleSend}
          disabled={!input.trim() || sendMessage.isPending || isUploading}
          size="sm"
        >
          Send
        </Button>
      </div>
    </div>
  );
}
```

- [ ] **Step 2: TypeScript check**

```bash
cd flowdesk-web && npx tsc --noEmit
```
Expected: no errors.

- [ ] **Step 3: Manual test**

With the API running and frontend running:
1. Open a project's Messages tab as an agency user — paperclip icon appears in the input bar
2. Click paperclip, pick a file — filename chip appears with a progress bar, then ✕ button after upload
3. Type a message, click Send — message appears with "📎 Download attachment" link below the bubble
4. Click the link — file opens/downloads in a new tab
5. Click ✕ on the chip before sending — chip clears, no file attached
6. Log in as a Client user — paperclip icon should NOT appear

- [ ] **Step 4: Commit**

```bash
git add flowdesk-web/src/components/projects/MessagesTab.tsx
git commit -m "feat(messages): add file attachment UI with upload progress and download links"
```

---

## Self-Review (completed)

- [x] **IFileStorageService generalized** to `string folderPath`: Task 1 ✅
- [x] **FileStorageService** uses `{folderPath}/{safeName}` as key: Task 1 ✅
- [x] **DeliverableService** call site updated to `"deliverables/{id}"`: Task 1 ✅
- [x] **CreateMessageRequest** has optional `string? FileUrl`: Task 2 ✅
- [x] **GetMessageUploadUrlRequest** DTO created: Task 2 ✅
- [x] **Validator** adds `FileUrl` max-length rule when not null: Task 2 ✅
- [x] **MessageService.CreateAsync** passes `FileUrl` to entity: Task 2 ✅
- [x] **MessageResponse** includes `string? FileUrl`: Task 2 ✅
- [x] **MessagesController** has `POST upload-url` (AgencyOnly), injects `IFileStorageService`: Task 3 ✅
- [x] **`Message` TypeScript type** has `fileUrl: string | null`: Task 4 ✅
- [x] **`useSendMessage`** accepts `{ content, fileUrl }`: Task 4 ✅
- [x] **`useMessageUploadUrl`** calls `POST .../messages/upload-url`: Task 4 ✅
- [x] **MessagesTab** — paperclip button (agency only), XHR upload, progress bar, chip, ✕ cancel: Task 5 ✅
- [x] **File link rendered** below bubble when `message.fileUrl` is set: Task 5 ✅
- [x] **Send button** disabled while `isUploading`: Task 5 ✅
- [x] **Text required** — `handleSend` guards on `input.trim()`, Content validator stays `NotEmpty`: Task 2 + 5 ✅
- [x] **Type consistency** — `pendingFileUrl` (state) → `fileUrl` (passed to mutate) → `fileUrl` in request body → `FileUrl` on `CreateMessageRequest` record: consistent ✅
