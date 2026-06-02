"use client";

import { useState, useEffect, useRef } from "react";
import { useAuth } from "@/contexts/AuthContext";
import { useMessages, useSendMessage } from "@/lib/queries";
import { useChatHub } from "@/hooks/useChatHub";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { toast } from "sonner";
import type { Message } from "@/types";

export function MessagesTab({ projectId }: { projectId: string }) {
  const { user } = useAuth();
  const { data: history = [], isLoading, isError } = useMessages(projectId);
  const { liveMessages, connectionState } = useChatHub(projectId);
  const sendMessage = useSendMessage(projectId);

  const [input, setInput] = useState("");
  const bottomRef = useRef<HTMLDivElement>(null);

  // Merge history and real-time arrivals, deduplicated by id, sorted chronologically
  const allMessages: Message[] = [
    ...history,
    ...liveMessages.filter(lm => !history.some(h => h.id === lm.id)),
  ].sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime());

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [allMessages.length]);

  async function handleSend() {
    const content = input.trim();
    if (!content) return;
    setInput("");
    try {
      await sendMessage.mutateAsync(content);
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

      <div className="p-4 border-t flex gap-2">
        <Input
          value={input}
          onChange={e => setInput(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder="Type a message…"
          className="flex-1"
        />
        <Button
          onClick={handleSend}
          disabled={!input.trim() || sendMessage.isPending}
          size="sm"
        >
          Send
        </Button>
      </div>
    </div>
  );
}
