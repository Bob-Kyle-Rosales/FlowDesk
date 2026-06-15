"use client";

import { useState, useRef, useCallback } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useAuth } from "@/contexts/AuthContext";
import {
  useDeliverables, useCreateDeliverable, useMilestones,
  useGetUploadUrl, useConfirmUpload,
  useApproveDeliverable, useRequestRevision,
} from "@/lib/queries";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Plus, Upload, ExternalLink } from "lucide-react";
import { toast } from "sonner";
import type { Deliverable } from "@/types";

const STATUS_CONFIG: Record<Deliverable["status"], { label: string; classes: string }> = {
  Pending: { label: "Pending", classes: "bg-gray-100 text-gray-600 border-gray-200" },
  UnderReview: { label: "Under Review", classes: "bg-blue-50 text-blue-700 border-blue-200" },
  Approved: { label: "Approved", classes: "bg-emerald-50 text-emerald-700 border-emerald-200" },
  Revision: { label: "Revision", classes: "bg-amber-50 text-amber-700 border-amber-200" },
};

const createSchema = z.object({
  name: z.string().min(1, "Name is required").max(200),
  milestoneId: z.string().optional(),
});

const revisionSchema = z.object({
  notes: z.string().min(1, "Notes are required").max(2000),
});

type CreateForm = z.infer<typeof createSchema>;
type RevisionForm = z.infer<typeof revisionSchema>;

export function DeliverablesTab({ projectId }: { projectId: string }) {
  const { user } = useAuth();
  const isAgency = user?.role !== "Client";
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [uploadProgress, setUploadProgress] = useState<Record<string, number>>({});
  const [revisionFormId, setRevisionFormId] = useState<string | null>(null);
  const fileInputRefs = useRef<Record<string, HTMLInputElement | null>>({});

  const { data: deliverables, isLoading } = useDeliverables(projectId);
  const { data: milestones } = useMilestones(projectId);
  const createDeliverable = useCreateDeliverable(projectId);
  const getUploadUrl = useGetUploadUrl();
  const confirmUpload = useConfirmUpload(projectId);
  const approveDeliverable = useApproveDeliverable(projectId);
  const requestRevision = useRequestRevision(projectId);

  const createForm = useForm<CreateForm>({ resolver: zodResolver(createSchema) });
  const revisionForm = useForm<RevisionForm>({ resolver: zodResolver(revisionSchema) });

  async function onCreateSubmit(data: CreateForm) {
    try {
      await createDeliverable.mutateAsync({
        name: data.name,
        description: null,
        milestoneId: data.milestoneId || null,
      });
      createForm.reset();
      setShowCreateForm(false);
      toast.success("Deliverable added");
    } catch {
      toast.error("Failed to add deliverable");
    }
  }

  const handleFileChange = useCallback(async (deliverableId: string, file: File) => {
    let uploadUrl = "";
    let fileUrl = "";
    try {
      ({ uploadUrl, fileUrl } = await getUploadUrl.mutateAsync({
        id: deliverableId,
        fileName: file.name,
        contentType: file.type || "application/octet-stream",
      }));
    } catch (err) {
      console.error("Failed to get upload URL", err);
      toast.error("Upload failed — could not get upload URL from server");
      return;
    }

    try {
      // XHR (not fetch) because XHR.upload.onprogress enables the progress bar.
      // The PUT goes directly to R2 — no auth headers, different domain from the API.
      await new Promise<void>((resolve, reject) => {
        const xhr = new XMLHttpRequest();
        xhr.upload.onprogress = (e) => {
          if (e.lengthComputable) {
            setUploadProgress(prev => ({ ...prev, [deliverableId]: Math.round(e.loaded / e.total * 100) }));
          }
        };
        xhr.onload = () => (xhr.status >= 200 && xhr.status < 300) ? resolve() : reject(new Error(`R2 PUT returned ${xhr.status}`));
        xhr.onerror = () => reject(new Error("R2 PUT network error (possible CORS issue)"));
        xhr.open("PUT", uploadUrl);
        xhr.setRequestHeader("Content-Type", file.type || "application/octet-stream");
        xhr.send(file);
      });
    } catch (err) {
      console.error("R2 upload failed", err);
      setUploadProgress(prev => { const n = { ...prev }; delete n[deliverableId]; return n; });
      toast.error(`Upload failed — ${err instanceof Error ? err.message : "R2 error"}`);
      return;
    }

    try {
      await confirmUpload.mutateAsync({ id: deliverableId, fileUrl });
      setUploadProgress(prev => { const n = { ...prev }; delete n[deliverableId]; return n; });
      toast.success("File uploaded");
    } catch (err) {
      console.error("Failed to confirm upload", err);
      setUploadProgress(prev => { const n = { ...prev }; delete n[deliverableId]; return n; });
      toast.error("Upload failed — could not save file URL");
    }
  }, [getUploadUrl, confirmUpload]);

  async function onApprove(id: string) {
    try {
      await approveDeliverable.mutateAsync(id);
      toast.success("Deliverable approved");
    } catch {
      toast.error("Failed to approve");
    }
  }

  async function onRevisionSubmit(data: RevisionForm, deliverableId: string) {
    try {
      await requestRevision.mutateAsync({ id: deliverableId, notes: data.notes });
      revisionForm.reset();
      setRevisionFormId(null);
      toast.success("Revision requested");
    } catch {
      toast.error("Failed to request revision");
    }
  }

  if (isLoading) {
    return <div className="py-8 text-sm text-muted-foreground">Loading...</div>;
  }

  return (
    <div className="space-y-3 pt-4">
      {deliverables?.map(deliverable => {
        const { label, classes } = STATUS_CONFIG[deliverable.status];
        const progress = uploadProgress[deliverable.id];
        const isUploading = progress !== undefined;

        return (
          <div key={deliverable.id} className="border rounded-lg p-4 bg-card space-y-3">
            <div className="flex items-start justify-between gap-3">
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2">
                  <p className="text-sm font-medium text-foreground">{deliverable.name}</p>
                  <span className="text-xs text-muted-foreground bg-muted px-1.5 py-0.5 rounded">
                    v{deliverable.version}
                  </span>
                </div>
                {deliverable.fileUrl && (
                  <a
                    href={deliverable.fileUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="flex items-center gap-1 text-xs text-primary hover:underline mt-0.5 w-fit"
                  >
                    View file <ExternalLink className="size-3" />
                  </a>
                )}
              </div>
              <Badge variant="outline" className={`text-xs rounded-full border shrink-0 ${classes}`}>
                {label}
              </Badge>
            </div>

            {deliverable.status === "Revision" && deliverable.revisionNotes && (
              <div className="text-xs text-amber-700 bg-amber-50 border border-amber-200 rounded p-2">
                <span className="font-semibold">Revision requested: </span>
                {deliverable.revisionNotes}
              </div>
            )}

            {isAgency && (
              <>
                <input
                  type="file"
                  className="hidden"
                  ref={el => { fileInputRefs.current[deliverable.id] = el; }}
                  onChange={e => {
                    const file = e.target.files?.[0];
                    if (file) handleFileChange(deliverable.id, file);
                    e.target.value = "";
                  }}
                />
                {isUploading ? (
                  <div className="space-y-1">
                    <div className="h-1.5 bg-muted rounded-full overflow-hidden">
                      <div
                        className="h-full bg-primary rounded-full transition-all"
                        style={{ width: `${progress}%` }}
                      />
                    </div>
                    <p className="text-xs text-muted-foreground">{progress}% uploaded</p>
                  </div>
                ) : (
                  <button
                    onClick={() => fileInputRefs.current[deliverable.id]?.click()}
                    className="w-full border-2 border-dashed border-primary/30 rounded-md py-2 px-3 text-xs text-primary/70 hover:border-primary/60 hover:text-primary transition-colors text-center"
                  >
                    <Upload className="size-3 inline mr-1" />
                    {deliverable.fileUrl ? "Replace file" : "Upload file"}
                  </button>
                )}
              </>
            )}

            {!isAgency && deliverable.status === "UnderReview" && (
              <div className="space-y-2">
                <div className="flex gap-2">
                  <Button size="sm" onClick={() => onApprove(deliverable.id)} disabled={approveDeliverable.isPending}>
                    Approve
                  </Button>
                  <Button
                    size="sm"
                    variant="outline"
                    onClick={() => setRevisionFormId(revisionFormId === deliverable.id ? null : deliverable.id)}
                  >
                    Request Revision
                  </Button>
                </div>
                {revisionFormId === deliverable.id && (
                  <form
                    onSubmit={revisionForm.handleSubmit(data => onRevisionSubmit(data, deliverable.id))}
                    className="space-y-2"
                  >
                    <textarea
                      {...revisionForm.register("notes")}
                      placeholder="Describe what needs to change..."
                      className="w-full text-sm border rounded-md p-2 min-h-[80px] resize-none focus:outline-none focus:ring-2 focus:ring-primary/20 bg-background"
                    />
                    {revisionForm.formState.errors.notes && (
                      <p className="text-xs text-red-500">{revisionForm.formState.errors.notes.message}</p>
                    )}
                    <div className="flex gap-2">
                      <Button type="submit" size="sm" variant="destructive" disabled={requestRevision.isPending}>
                        Submit
                      </Button>
                      <Button type="button" size="sm" variant="ghost" onClick={() => { setRevisionFormId(null); revisionForm.reset(); }}>
                        Cancel
                      </Button>
                    </div>
                  </form>
                )}
              </div>
            )}
          </div>
        );
      })}

      {deliverables?.length === 0 && !showCreateForm && (
        <p className="text-sm text-muted-foreground py-4 text-center">No deliverables yet.</p>
      )}

      {isAgency && (
        showCreateForm ? (
          <form onSubmit={createForm.handleSubmit(onCreateSubmit)} className="border rounded-lg p-4 space-y-3 bg-card">
            <div className="space-y-1">
              <Label htmlFor="dlv-name" className="text-xs">Name</Label>
              <Input id="dlv-name" placeholder="e.g. Logo suite" {...createForm.register("name")} />
              {createForm.formState.errors.name && (
                <p className="text-xs text-red-500">{createForm.formState.errors.name.message}</p>
              )}
            </div>
            {milestones && milestones.length > 0 && (
              <div className="space-y-1">
                <Label htmlFor="dlv-ms" className="text-xs">Milestone (optional)</Label>
                <select
                  id="dlv-ms"
                  {...createForm.register("milestoneId")}
                  className="w-full text-sm border rounded-md px-3 py-1.5 bg-background focus:outline-none focus:ring-2 focus:ring-primary/20"
                >
                  <option value="">No milestone</option>
                  {milestones.map(m => (
                    <option key={m.id} value={m.id}>{m.title}</option>
                  ))}
                </select>
              </div>
            )}
            <div className="flex gap-2">
              <Button type="submit" size="sm" disabled={createDeliverable.isPending}>
                {createDeliverable.isPending ? "Adding..." : "Add Deliverable"}
              </Button>
              <Button type="button" variant="ghost" size="sm" onClick={() => { setShowCreateForm(false); createForm.reset(); }}>
                Cancel
              </Button>
            </div>
          </form>
        ) : (
          <Button variant="outline" size="sm" className="gap-2" onClick={() => setShowCreateForm(true)}>
            <Plus className="size-4" /> Add Deliverable
          </Button>
        )
      )}
    </div>
  );
}
