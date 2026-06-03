# Phase 3C — Chat File Attachments Design Spec

**Date:** 2026-06-02
**Scope:** Add optional file attachments to chat messages. Text remains required. Upload flow mirrors the existing deliverables pattern.

---

## Architecture

Pre-upload pattern: file is uploaded to R2 before the message is sent. The `fileUrl` is held in frontend state until the user clicks Send, at which point text + fileUrl are posted together.

```
1. User picks a file (paperclip button → hidden <input type="file">)

2. POST /api/projects/{id}/messages/upload-url
   body: { fileName, contentType }
   → MessagesController calls IFileStorageService.GenerateUploadUrlAsync
   → returns { uploadUrl, fileUrl }

3. Browser XHR PUT file bytes → R2 uploadUrl (progress tracked)

4. fileUrl stored in MessagesTab local state; filename chip shown above input

5. POST /api/projects/{id}/messages
   body: { content, fileUrl }   ← fileUrl optional
   → MessagesController.Send (existing) — passes fileUrl to service
   → Persists to DB, broadcasts ReceiveMessage via ChatHub

6. Received messages: if message.fileUrl is set, render a download link
   below the message bubble
```

---

## New Files

None — all changes are to existing files.

---

## Modified Files

### Backend

```
FlowDesk.Core/DTOs/Messages/CreateMessageRequest.cs  — add optional FileUrl
FlowDesk.API/Controllers/MessagesController.cs       — add upload-url endpoint
FlowDesk.API/Validators/Messages/CreateMessageRequestValidator.cs  — add FileUrl rule
FlowDesk.Core/Services/MessageService.cs             — pass FileUrl to entity
```

### Frontend

```
flowdesk-web/src/types/index.ts                      — add fileUrl to Message, update CreateMessageRequest
flowdesk-web/src/lib/queries.ts                      — update useSendMessage to accept { content, fileUrl }
                                                       add useMessageUploadUrl hook
flowdesk-web/src/components/projects/MessagesTab.tsx — add file picker, upload flow, file chip, link rendering
```

---

## Backend Detail

### CreateMessageRequest (updated)

```csharp
// FlowDesk.Core/DTOs/Messages/CreateMessageRequest.cs
namespace FlowDesk.Core.DTOs.Messages;

public record CreateMessageRequest(string Content, string? FileUrl);
```

### MessagesController — new upload-url endpoint

Add alongside the existing endpoints:

```csharp
[HttpPost("upload-url")]
[Authorize(Policy = "AgencyOnly")]
public async Task<ActionResult<UploadUrlResponse>> GetUploadUrl(
    Guid projectId, [FromBody] GetMessageUploadUrlRequest request)
{
    var result = await _fileStorage.GenerateUploadUrlAsync(
        $"messages/{projectId}", request.FileName, request.ContentType);
    return Ok(result);
}
```

`MessagesController` gains `IFileStorageService _fileStorage` injected via constructor.

`GetMessageUploadUrlRequest` is a new minimal DTO (same shape as deliverables):
```csharp
// FlowDesk.Core/DTOs/Messages/GetMessageUploadUrlRequest.cs
namespace FlowDesk.Core.DTOs.Messages;

public record GetMessageUploadUrlRequest(string FileName, string ContentType);
```

`UploadUrlResponse` is already defined in `FlowDesk.Core/DTOs/Deliverables/` — import and reuse it (no new type needed).

The `Send` endpoint already accepts `CreateMessageRequest`; with the updated record it automatically accepts the optional `FileUrl`.

### MessageService.CreateAsync (updated)

```csharp
var message = new Message
{
    Id = Guid.NewGuid(),
    Content = request.Content,
    FileUrl = request.FileUrl,      // ← new line
    ProjectId = projectId,
    SenderId = _currentUser.UserId!.Value,
    IsRead = false,
    CreatedAt = DateTime.UtcNow
};
```

### Validator (updated)

```csharp
RuleFor(x => x.Content).NotEmpty().MaximumLength(4000);
RuleFor(x => x.FileUrl).MaximumLength(2000).When(x => x.FileUrl is not null);
```

### Authorization

Upload URL is `AgencyOnly` — in Phase 3C only agency staff can attach files. Clients can receive and view attachments. This can be relaxed later.

---

## Frontend Detail

### Types (updated)

```ts
// Message — add fileUrl
export interface Message {
  id: string;
  content: string;
  senderId: string;
  senderName: string;
  isRead: boolean;
  createdAt: string;
  fileUrl: string | null;     // ← new
}

// CreateMessageRequest — add fileUrl
export interface CreateMessageRequest {
  content: string;
  fileUrl?: string | null;    // ← new
}

// New request type for upload URL
export interface GetMessageUploadUrlRequest {
  fileName: string;
  contentType: string;
}
```

### New query hook

```ts
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

### useSendMessage (updated signature)

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
```

### MessagesTab local state additions

```ts
const [pendingFile, setPendingFile] = useState<File | null>(null);
const [pendingFileUrl, setPendingFileUrl] = useState<string | null>(null);
const [uploadProgress, setUploadProgress] = useState<number>(0);
const [isUploading, setIsUploading] = useState(false);
const fileInputRef = useRef<HTMLInputElement>(null);
```

### File pick and upload flow

```ts
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
      xhr.upload.onprogress = e => {
        if (e.lengthComputable) setUploadProgress(Math.round((e.loaded / e.total) * 100));
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
    // Reset input so the same file can be re-selected after error
    if (fileInputRef.current) fileInputRef.current.value = "";
  }
}

function clearPendingFile() {
  setPendingFile(null);
  setPendingFileUrl(null);
  setUploadProgress(0);
  if (fileInputRef.current) fileInputRef.current.value = "";
}
```

### Send (updated)

```ts
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
```

Send button disabled when: `!input.trim() || sendMessage.isPending || isUploading`

### Input area UI additions

Below the file chip (shown when `pendingFile` is set):
```
┌─────────────────────────────────────────────┐
│ 📎 filename.pdf  [✕]    ████████░░ 80%      │  ← shown during/after upload
├─────────────────────────────────────────────┤
│ [📎] [text input                  ] [Send]  │
└─────────────────────────────────────────────┘
```

- Progress bar only shown while `isUploading`
- Chip shown after upload completes (until sent or cancelled)
- ✕ button on chip calls `clearPendingFile()`
- Paperclip button calls `fileInputRef.current?.click()`; hidden `<input type="file" ref={fileInputRef}>`

### Message rendering (file link)

For each rendered message, below the text bubble:
```tsx
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
```

---

## Error Handling

| Scenario | Behaviour |
|---|---|
| Upload URL fetch fails | Toast error, file cleared, user can retry |
| R2 PUT fails | Toast error, file cleared, user can retry |
| Send fails (with pending file) | Toast error; text restored to input, fileUrl cleared (file was already uploaded — user must re-attach) |
| File too large | No server-side size limit in Phase 3C; R2 enforces bucket policy |

---

## Out of Scope

- Clients attaching files (AgencyOnly for now)
- Image previews (rendered as download links only)
- File size validation on the client
- Multiple attachments per message
