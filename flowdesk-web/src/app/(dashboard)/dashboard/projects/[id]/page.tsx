"use client";

import { use, useState, useRef, useEffect } from "react";
import Link from "next/link";
import { ArrowLeft, Loader2 } from "lucide-react";
import { useProject, useProjectStats } from "@/lib/queries";
import { useProjectHub } from "@/hooks/useProjectHub";
import { useAuth } from "@/contexts/AuthContext";
import { ProjectHeader } from "@/components/projects/ProjectHeader";
import { MilestonesTab } from "@/components/projects/MilestonesTab";
import { DeliverablesTab } from "@/components/projects/DeliverablesTab";
import { MessagesTab } from "@/components/projects/MessagesTab";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Button } from "@/components/ui/button";
import api from "@/lib/api";

export default function ProjectDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const { data: project, isLoading, isError } = useProject(id);
  const { data: stats } = useProjectStats(id);
  const { user } = useAuth();
  useProjectHub(id);

  const [reportText, setReportText] = useState("");
  const [isGenerating, setIsGenerating] = useState(false);

  const abortControllerRef = useRef<AbortController | null>(null);

  useEffect(() => {
    return () => {
      abortControllerRef.current?.abort();
    };
  }, []);

  async function generateReport() {
    if (isGenerating) return;
    setIsGenerating(true);
    setReportText("");

    let reader: ReadableStreamDefaultReader<Uint8Array> | null = null;

    try {
      const controller = new AbortController();
      abortControllerRef.current = controller;

      const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5269";
      let res = await fetch(`${apiUrl}/api/projects/${id}/report`, {
        method: "POST",
        credentials: "include",
        signal: controller.signal,
      });

      if (res.status === 401) {
        try {
          await api.post("/api/auth/refresh");
        } catch {
          setReportText("[Session expired. Please log in again.]");
          return;
        }
        res = await fetch(`${apiUrl}/api/projects/${id}/report`, {
          method: "POST",
          credentials: "include",
          signal: controller.signal,
        });
      }

      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      if (!res.body) throw new Error("No response body");

      reader = res.body.getReader();
      const decoder = new TextDecoder();
      let buffer = "";

      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        buffer += decoder.decode(value, { stream: true });
        const lines = buffer.split("\n");
        buffer = lines.pop() ?? "";

        for (const line of lines) {
          if (line.startsWith("data: ")) {
            const token = line.slice("data: ".length);
            setReportText((prev) => prev + token);
          }
        }
      }

      // Flush any remaining buffered content
      buffer += decoder.decode();
      if (buffer.startsWith("data: ")) {
        const remaining = buffer.slice("data: ".length);
        if (remaining) setReportText((prev) => prev + remaining);
      }
    } catch (err) {
      if (err instanceof DOMException && err.name === "AbortError") return;
      console.error("Report generation failed", err);
      setReportText((prev) => prev + "\n\n[Generation failed. Please try again.]");
    } finally {
      await reader?.cancel();
      setIsGenerating(false);
    }
  }

  if (isLoading) {
    return <div className="p-8 text-sm text-muted-foreground animate-pulse">Loading project...</div>;
  }

  if (isError || !project) {
    return (
      <div className="p-8 space-y-4">
        <Link href="/dashboard/projects" className="flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground transition-colors w-fit">
          <ArrowLeft className="size-4" /> Projects
        </Link>
        <p className="text-foreground font-medium">Project not found.</p>
      </div>
    );
  }

  const isAgency = user?.role !== "Client";

  return (
    <div className="p-6 space-y-6">
      <Link href="/dashboard/projects" className="flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground transition-colors w-fit">
        <ArrowLeft className="size-4" /> Projects
      </Link>

      <ProjectHeader project={project} stats={stats} />

      <Tabs defaultValue="milestones">
        <TabsList>
          <TabsTrigger value="milestones">Milestones</TabsTrigger>
          <TabsTrigger value="deliverables">Deliverables</TabsTrigger>
          <TabsTrigger value="messages">Messages</TabsTrigger>
        </TabsList>

        <TabsContent value="milestones">
          <MilestonesTab projectId={id} />
        </TabsContent>

        <TabsContent value="deliverables">
          <DeliverablesTab projectId={id} />
        </TabsContent>

        <TabsContent value="messages">
          <MessagesTab projectId={id} />
        </TabsContent>
      </Tabs>

      {isAgency && (
        <div className="space-y-3 pt-2">
          <div className="flex items-center justify-between">
            <h2 className="text-sm font-semibold text-foreground">AI Status Report</h2>
            <Button
              size="sm"
              variant="outline"
              onClick={generateReport}
              disabled={isGenerating}
            >
              {isGenerating ? (
                <>
                  <Loader2 className="size-3.5 mr-1.5 animate-spin" />
                  Generating…
                </>
              ) : reportText ? (
                "Regenerate"
              ) : (
                "Generate Report"
              )}
            </Button>
          </div>

          {(reportText || isGenerating) && (
            <div className="bg-muted/50 rounded-xl border p-4 text-sm text-foreground whitespace-pre-wrap leading-relaxed">
              {reportText}
              {isGenerating && (
                <span className="inline-block w-2 h-4 bg-foreground/60 animate-pulse ml-0.5 align-text-bottom" />
              )}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
