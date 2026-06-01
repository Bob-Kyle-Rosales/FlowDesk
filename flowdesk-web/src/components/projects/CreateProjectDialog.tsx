"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { toast } from "sonner";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useCreateProject, useClients } from "@/lib/queries";

const schema = z.object({
  name: z.string().min(1, "Name is required"),
  description: z.string().optional(),
  clientId: z.string().min(1, "Client is required"),
});
type FormData = z.infer<typeof schema>;

interface CreateProjectDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function CreateProjectDialog({ open, onOpenChange }: CreateProjectDialogProps) {
  const { data: clients = [], isLoading: clientsLoading } = useClients();
  const createProject = useCreateProject();

  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { name: "", description: "", clientId: "" },
  });

  function handleClose(v: boolean) {
    if (!v) reset();
    onOpenChange(v);
  }

  async function onSubmit(data: FormData) {
    try {
      await createProject.mutateAsync({
        name: data.name,
        description: data.description || null,
        clientId: data.clientId,
      });
      toast.success("Project created");
      reset();
      onOpenChange(false);
    } catch {
      toast.error("Failed to create project");
    }
  }

  const noClients = !clientsLoading && clients.length === 0;

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>New Project</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="proj-name">Project name</Label>
            <Input id="proj-name" placeholder="Website redesign" {...register("name")} />
            {errors.name && <p className="text-xs text-destructive">{errors.name.message}</p>}
          </div>

          <div className="space-y-2">
            <Label htmlFor="proj-desc">
              Description <span className="text-muted-foreground">(optional)</span>
            </Label>
            <textarea
              id="proj-desc"
              placeholder="Brief project description..."
              className="w-full text-sm border rounded-md p-2 min-h-[80px] resize-none focus:outline-none focus:ring-2 focus:ring-primary/20 bg-background"
              {...register("description")}
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="proj-client">Client</Label>
            <select
              id="proj-client"
              className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
              disabled={noClients || clientsLoading}
              {...register("clientId")}
            >
              {noClients ? (
                <option value="">No clients yet — invite a client first</option>
              ) : (
                <>
                  <option value="">Select a client…</option>
                  {clients.map((c) => (
                    <option key={c.id} value={c.id}>{c.name}</option>
                  ))}
                </>
              )}
            </select>
            {errors.clientId && <p className="text-xs text-destructive">{errors.clientId.message}</p>}
          </div>

          <DialogFooter>
            <Button
              type="button"
              variant="ghost"
              onClick={() => handleClose(false)}
            >
              Cancel
            </Button>
            <Button type="submit" disabled={isSubmitting || noClients || clientsLoading}>
              {isSubmitting ? "Creating…" : "Create project"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
