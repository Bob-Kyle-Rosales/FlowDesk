"use client";

import { use, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { useAuth } from "@/contexts/AuthContext";
import {
  useProject,
  useMilestones,
  useDeliverables,
  useApproveDeliverable,
  useRequestRevision,
} from "@/lib/queries";
import { MessagesTab } from "@/components/projects/MessagesTab";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { toast } from "sonner";
import { ArrowLeft, Download } from "lucide-react";
import type { Deliverable, Milestone } from "@/types";

const milestoneColor: Record<string, string> = {
  Pending: "bg-gray-100 text-gray-600",
  InProgress: "bg-blue-100 text-blue-700",
  Completed: "bg-green-100 text-green-700",
};

const deliverableColor: Record<string, string> = {
  Pending: "bg-gray-100 text-gray-600",
  UnderReview: "bg-yellow-100 text-yellow-700",
  Approved: "bg-green-100 text-green-700",
  Revision: "bg-red-100 text-red-700",
};

type Tab = "milestones" | "deliverables" | "messages";

function MilestonesTab({ milestones }: { milestones: Milestone[] }) {
  if (!milestones.length) {
    return <p className="text-sm text-muted-foreground text-center py-8">No milestones yet.</p>;
  }
  return (
    <div className="space-y-3">
      {milestones.map((m) => (
        <div key={m.id} className="bg-white rounded-xl border shadow-sm p-4 flex items-center justify-between">
          <div>
            <p className="font-medium text-sm">{m.title}</p>
            {m.dueDate && (
              <p className="text-xs text-muted-foreground mt-0.5">
                Due {new Date(m.dueDate).toLocaleDateString()}
              </p>
            )}
          </div>
          <Badge className={milestoneColor[m.status] ?? "bg-gray-100 text-gray-600"}>
            {m.status}
          </Badge>
        </div>
      ))}
    </div>
  );
}

function DeliverablesTab({
  deliverables,
  projectId,
}: {
  deliverables: Deliverable[];
  projectId: string;
}) {
  const approve = useApproveDeliverable(projectId);
  const requestRevision = useRequestRevision(projectId);
  const [revisionId, setRevisionId] = useState<string | null>(null);
  const [revisionNotes, setRevisionNotes] = useState("");

  if (!deliverables.length) {
    return <p className="text-sm text-muted-foreground text-center py-8">No deliverables yet.</p>;
  }

  async function handleApprove(id: string) {
    try {
      await approve.mutateAsync(id);
      toast.success("Deliverable approved");
    } catch {
      toast.error("Failed to approve");
    }
  }

  async function handleRevision(id: string) {
    if (!revisionNotes.trim()) {
      toast.error("Please enter revision notes");
      return;
    }
    try {
      await requestRevision.mutateAsync({ id, notes: revisionNotes });
      toast.success("Revision requested");
      setRevisionId(null);
      setRevisionNotes("");
    } catch {
      toast.error("Failed to request revision");
    }
  }

  return (
    <div className="space-y-3">
      {deliverables.map((d) => (
        <div key={d.id} className="bg-white rounded-xl border shadow-sm p-4 space-y-3">
          <div className="flex items-start justify-between">
            <div>
              <p className="font-medium text-sm">{d.name}</p>
              {d.description && (
                <p className="text-xs text-muted-foreground mt-0.5">{d.description}</p>
              )}
            </div>
            <Badge className={deliverableColor[d.status] ?? "bg-gray-100 text-gray-600"}>
              {d.status}
            </Badge>
          </div>

          {d.fileUrl && (
            <a
              href={d.fileUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="inline-flex items-center gap-1.5 text-xs text-violet-600 hover:underline"
            >
              <Download className="size-3.5" />
              Download file
            </a>
          )}

          {d.status === "UnderReview" && (
            <div className="flex gap-2 pt-1">
              <Button
                size="sm"
                onClick={() => handleApprove(d.id)}
                disabled={approve.isPending}
              >
                Approve
              </Button>
              <Button
                size="sm"
                variant="outline"
                onClick={() => {
                  setRevisionId(revisionId === d.id ? null : d.id);
                  setRevisionNotes("");
                }}
              >
                Request Revision
              </Button>
            </div>
          )}

          {revisionId === d.id && (
            <div className="space-y-2 pt-1">
              <textarea
                value={revisionNotes}
                onChange={(e) => setRevisionNotes(e.target.value)}
                placeholder="Describe what needs to be revised…"
                rows={3}
                className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm resize-none focus:outline-none focus:ring-2 focus:ring-ring"
              />
              <div className="flex gap-2">
                <Button
                  size="sm"
                  onClick={() => handleRevision(d.id)}
                  disabled={requestRevision.isPending}
                >
                  Send
                </Button>
                <Button
                  size="sm"
                  variant="ghost"
                  onClick={() => { setRevisionId(null); setRevisionNotes(""); }}
                >
                  Cancel
                </Button>
              </div>
            </div>
          )}
        </div>
      ))}
    </div>
  );
}

export default function ProjectDetailPage({
  params,
}: {
  params: Promise<{ slug: string; id: string }>;
}) {
  const { slug, id } = use(params);
  const router = useRouter();
  const { user, loading } = useAuth();
  const { data: project, isLoading: projectLoading } = useProject(id);
  const { data: milestones = [] } = useMilestones(id);
  const { data: deliverables = [] } = useDeliverables(id);
  const [activeTab, setActiveTab] = useState<Tab>("milestones");

  useEffect(() => {
    if (!loading && !user) router.push("/login");
    else if (!loading && user && user.organisationSlug !== slug) {
      router.push(`/portal/${user.organisationSlug}`);
    }
  }, [user, loading, router, slug]);

  if (loading || projectLoading) {
    return <p className="text-sm text-muted-foreground animate-pulse py-8 text-center">Loading…</p>;
  }

  if (!project) {
    return <p className="text-sm text-destructive py-8 text-center">Project not found.</p>;
  }

  const tabs: { key: Tab; label: string }[] = [
    { key: "milestones", label: "Milestones" },
    { key: "deliverables", label: "Deliverables" },
    { key: "messages", label: "Messages" },
  ];

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <Link
          href={`/portal/${slug}`}
          className="text-muted-foreground hover:text-foreground transition-colors"
        >
          <ArrowLeft className="size-4" />
        </Link>
        <h1 className="text-xl font-semibold">{project.name}</h1>
      </div>

      <div className="flex gap-1 border-b">
        {tabs.map(({ key, label }) => (
          <button
            key={key}
            onClick={() => setActiveTab(key)}
            className={`px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors ${
              activeTab === key
                ? "border-violet-600 text-violet-600"
                : "border-transparent text-muted-foreground hover:text-foreground"
            }`}
          >
            {label}
          </button>
        ))}
      </div>

      {activeTab === "milestones" && <MilestonesTab milestones={milestones} />}
      {activeTab === "deliverables" && (
        <DeliverablesTab deliverables={deliverables} projectId={id} />
      )}
      {activeTab === "messages" && <MessagesTab projectId={id} />}
    </div>
  );
}
