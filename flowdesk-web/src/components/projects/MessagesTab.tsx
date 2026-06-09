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
  const xhrRef = useRef<XMLHttpRequest | null>(null);

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

  useEffect(() => {
    return () => { xhrRef.current?.abort(); };
  }, []);

  async function handleFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    if (isUploading) return;
    const file = e.target.files?.[0];
    if (!file) return;

    const MAX_FILE_BYTES = 25 * 1024 * 1024; // 25 MB
    if (file.size > MAX_FILE_BYTES) {
      toast.error("File must be smaller than 25 MB");
      if (fileInputRef.current) fileInputRef.current.value = "";
      return;
    }

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
        xhrRef.current = xhr;
        xhr.upload.onprogress = ev => {
          if (ev.lengthComputable) setUploadProgress(Math.round((ev.loaded / ev.total) * 100));
        };
        xhr.onload = () => (xhr.status === 200 ? resolve() : reject(new Error("Upload failed")));
        xhr.onerror = () => reject(new Error("Upload failed"));
        xhr.onabort = () => reject(new Error("aborted"));
        xhr.open("PUT", uploadUrl);
        xhr.setRequestHeader("Content-Type", file.type || "application/octet-stream");
        xhr.send(file);
      });

      setPendingFileUrl(fileUrl);
    } catch (err) {
      if ((err as Error).message !== "aborted") {
        toast.error("File upload failed");
      }
      setPendingFile(null);
      setPendingFileUrl(null);
    } finally {
      setIsUploading(false);
      if (fileInputRef.current) fileInputRef.current.value = "";
    }
  }

  function clearPendingFile() {
    xhrRef.current?.abort();
    xhrRef.current = null;
    setPendingFile(null);
    setPendingFileUrl(null);
    setUploadProgress(0);
    setIsUploading(false);
    if (fileInputRef.current) fileInputRef.current.value = "";
  }

  async function handleSend() {
    if (isUploading) return;
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
                  <Paperclip className="size-3 inline mr-1" />Download attachment
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
