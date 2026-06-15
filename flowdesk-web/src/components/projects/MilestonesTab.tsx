"use client";

import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useAuth } from "@/contexts/AuthContext";
import { useMilestones, useCreateMilestone, useUpdateMilestoneStatus } from "@/lib/queries";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { CheckCircle2, Circle, Clock, Plus } from "lucide-react";
import { toast } from "sonner";
import type { Milestone } from "@/types";

const STATUS_CONFIG: Record<Milestone["status"], { label: string; Icon: React.ElementType; classes: string }> = {
  Pending:   { label: "Pending",     Icon: Circle,       classes: "bg-gray-100 text-gray-600 border-gray-200" },
  InProgress:{ label: "In Progress", Icon: Clock,        classes: "bg-amber-50 text-amber-700 border-amber-200" },
  Completed: { label: "Completed",   Icon: CheckCircle2, classes: "bg-emerald-50 text-emerald-700 border-emerald-200" },
};


const createSchema = z.object({
  title: z.string().min(1, "Title is required").max(200),
  dueDate: z.string().optional(),
});
type CreateForm = z.infer<typeof createSchema>;

export function MilestonesTab({ projectId }: { projectId: string }) {
  const { user } = useAuth();
  const isAgency = user?.role !== "Client";
  const [showForm, setShowForm] = useState(false);

  const { data: milestones, isLoading } = useMilestones(projectId);
  const createMilestone = useCreateMilestone(projectId);
  const updateStatus = useUpdateMilestoneStatus(projectId);

  const { register, handleSubmit, reset, formState: { errors } } = useForm<CreateForm>({
    resolver: zodResolver(createSchema),
  });

  async function onSubmit(data: CreateForm) {
    try {
      await createMilestone.mutateAsync({
        title: data.title,
        description: null,
        order: milestones?.length ?? 0,
        dueDate: data.dueDate || null,
      });
      reset();
      setShowForm(false);
      toast.success("Milestone added");
    } catch {
      toast.error("Failed to add milestone");
    }
  }

  async function changeStatus(milestone: Milestone, status: Milestone["status"]) {
    if (status === milestone.status) return;
    try {
      await updateStatus.mutateAsync({ id: milestone.id, status });
    } catch {
      toast.error("Failed to update status");
    }
  }

  if (isLoading) {
    return <div className="py-8 text-sm text-muted-foreground">Loading...</div>;
  }

  return (
    <div className="space-y-3 pt-4">
      {milestones?.map(milestone => {
        const { label, Icon, classes } = STATUS_CONFIG[milestone.status];
        return (
          <div key={milestone.id} className="flex items-center gap-3 p-3 rounded-lg border bg-card hover:shadow-sm transition-shadow">
            <Icon className={`size-4 shrink-0 ${milestone.status === "Completed" ? "text-emerald-600" : "text-muted-foreground"}`} />
            <div className="flex-1 min-w-0">
              <p className="text-sm font-medium text-foreground">{milestone.title}</p>
              {milestone.dueDate && (
                <p className="text-xs text-muted-foreground">
                  Due {new Date(milestone.dueDate).toLocaleDateString()}
                </p>
              )}
            </div>
            {isAgency ? (
              <select
                value={milestone.status}
                onChange={e => changeStatus(milestone, e.target.value as Milestone["status"])}
                className={`text-xs rounded-full border px-2 py-0.5 font-medium cursor-pointer focus:outline-none ${classes}`}
              >
                <option value="Pending">Pending</option>
                <option value="InProgress">In Progress</option>
                <option value="Completed">Completed</option>
              </select>
            ) : (
              <Badge variant="outline" className={`text-xs rounded-full border shrink-0 ${classes}`}>
                {label}
              </Badge>
            )}
          </div>
        );
      })}

      {milestones?.length === 0 && !showForm && (
        <p className="text-sm text-muted-foreground py-4 text-center">No milestones yet.</p>
      )}

      {isAgency && (
        showForm ? (
          <form onSubmit={handleSubmit(onSubmit)} className="border rounded-lg p-4 space-y-3 bg-card">
            <div className="space-y-1">
              <Label htmlFor="ms-title" className="text-xs">Title</Label>
              <Input id="ms-title" placeholder="e.g. Discovery & Strategy" {...register("title")} />
              {errors.title && <p className="text-xs text-red-500">{errors.title.message}</p>}
            </div>
            <div className="space-y-1">
              <Label htmlFor="ms-due" className="text-xs">Due date (optional)</Label>
              <Input id="ms-due" type="date" {...register("dueDate")} />
            </div>
            <div className="flex gap-2">
              <Button type="submit" size="sm" disabled={createMilestone.isPending}>
                {createMilestone.isPending ? "Adding..." : "Add Milestone"}
              </Button>
              <Button type="button" variant="ghost" size="sm" onClick={() => { setShowForm(false); reset(); }}>
                Cancel
              </Button>
            </div>
          </form>
        ) : (
          <Button variant="outline" size="sm" className="gap-2" onClick={() => setShowForm(true)}>
            <Plus className="size-4" /> Add Milestone
          </Button>
        )
      )}
    </div>
  );
}
