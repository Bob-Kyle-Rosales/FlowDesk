# Phase 2 Completion Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wire the "New Project" button to a functional dialog and replace the read-only Agency settings card with an editable form.

**Architecture:** Two isolated frontend changes — a new `CreateProjectDialog` component imported into the projects page, and an updated settings page that loads org data from `useOrganisation()` instead of stale auth context. No backend changes required; all API endpoints and mutations already exist.

**Tech Stack:** Next.js 16, React, react-hook-form, zod, @hookform/resolvers/zod, TanStack Query, shadcn/ui, sonner (toasts)

> **Note:** No test infrastructure exists in this project yet (Phase 6). TDD steps are omitted. Verify each task by running `npm run dev` in `flowdesk-web/` and manually testing in the browser.

---

## File Map

| Action | Path |
|---|---|
| **Create** | `flowdesk-web/src/components/projects/CreateProjectDialog.tsx` |
| **Modify** | `flowdesk-web/src/app/(dashboard)/dashboard/projects/page.tsx` |
| **Modify** | `flowdesk-web/src/app/(dashboard)/dashboard/settings/page.tsx` |

---

## Task 1: CreateProjectDialog component

**Files:**
- Create: `flowdesk-web/src/components/projects/CreateProjectDialog.tsx`

- [ ] **Step 1: Create the file with this exact content**

```tsx
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
            {noClients ? (
              <p className="text-sm text-muted-foreground">
                No clients yet — invite a client first.
              </p>
            ) : (
              <select
                id="proj-client"
                className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
                {...register("clientId")}
              >
                <option value="">Select a client…</option>
                {clients.map((c) => (
                  <option key={c.id} value={c.id}>{c.name}</option>
                ))}
              </select>
            )}
            {errors.clientId && <p className="text-xs text-destructive">{errors.clientId.message}</p>}
          </div>

          <DialogFooter>
            <Button
              type="button"
              variant="ghost"
              onClick={() => { reset(); onOpenChange(false); }}
            >
              Cancel
            </Button>
            <Button type="submit" disabled={isSubmitting || noClients}>
              {isSubmitting ? "Creating…" : "Create project"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
```

- [ ] **Step 2: Verify TypeScript compiles**

```bash
cd flowdesk-web && npx tsc --noEmit
```
Expected: no errors related to `CreateProjectDialog.tsx`

- [ ] **Step 3: Commit**

```bash
git add flowdesk-web/src/components/projects/CreateProjectDialog.tsx
git commit -m "feat(projects): add CreateProjectDialog component"
```

---

## Task 2: Wire CreateProjectDialog into ProjectsPage

**Files:**
- Modify: `flowdesk-web/src/app/(dashboard)/dashboard/projects/page.tsx`

- [ ] **Step 1: Replace the entire file with this content**

```tsx
"use client";

import { useState } from "react";
import { useProjects } from "@/lib/queries";
import { Project } from "@/types";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Plus, FolderKanban } from "lucide-react";
import Link from "next/link";
import { CreateProjectDialog } from "@/components/projects/CreateProjectDialog";

const statusStyles: Record<Project["status"], string> = {
  Active: "bg-emerald-50 text-emerald-700 border-emerald-200",
  Paused: "bg-amber-50 text-amber-700 border-amber-200",
  Completed: "bg-gray-100 text-gray-600 border-gray-200",
};

export default function ProjectsPage() {
  const { data: projects, isLoading, isError } = useProjects();
  const [dialogOpen, setDialogOpen] = useState(false);

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-bold text-gray-900">Projects</h1>
          <p className="text-muted-foreground text-sm mt-0.5">Manage all your client projects</p>
        </div>
        <Button size="sm" className="gap-2 h-9" onClick={() => setDialogOpen(true)}>
          <Plus className="size-4" /> New Project
        </Button>
      </div>

      {isLoading && (
        <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {Array.from({ length: 6 }).map((_, i) => (
            <Card key={i} className="animate-pulse border-0 shadow-sm">
              <CardHeader><div className="h-5 bg-muted rounded w-3/4" /></CardHeader>
              <CardContent><div className="h-4 bg-muted rounded w-1/2" /></CardContent>
            </Card>
          ))}
        </div>
      )}

      {isError && (
        <Card className="border-0 shadow-sm">
          <CardContent className="py-14 text-center">
            <div className="size-12 rounded-full bg-violet-50 flex items-center justify-center mx-auto mb-3">
              <FolderKanban className="size-5 text-violet-400" />
            </div>
            <p className="text-sm font-medium text-gray-700">Could not load projects</p>
            <p className="text-xs text-muted-foreground mt-1">Check your connection and try again.</p>
          </CardContent>
        </Card>
      )}

      {projects && projects.length === 0 && (
        <Card className="border-0 shadow-sm">
          <CardContent className="py-14 text-center">
            <div className="size-12 rounded-full bg-violet-50 flex items-center justify-center mx-auto mb-3">
              <FolderKanban className="size-5 text-violet-400" />
            </div>
            <p className="text-sm font-medium text-gray-700">No projects yet</p>
            <p className="text-xs text-muted-foreground mt-1">Create your first project to get started.</p>
          </CardContent>
        </Card>
      )}

      {projects && projects.length > 0 && (
        <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {projects.map((project) => (
            <Link key={project.id} href={`/dashboard/projects/${project.id}`}>
              <Card className="border-0 shadow-sm hover:shadow-md hover:-translate-y-0.5 transition-all duration-200 cursor-pointer border-t-2 border-t-violet-400">
                <CardHeader className="pb-2">
                  <div className="flex items-start justify-between gap-2">
                    <CardTitle className="text-sm font-semibold">{project.name}</CardTitle>
                    <Badge className={`text-xs shrink-0 rounded-full border ${statusStyles[project.status]}`} variant="outline">
                      {project.status}
                    </Badge>
                  </div>
                </CardHeader>
                <CardContent>
                  <p className="text-sm text-muted-foreground line-clamp-2">{project.description}</p>
                  <p className="text-xs text-muted-foreground mt-3 font-medium">{project.clientName}</p>
                </CardContent>
              </Card>
            </Link>
          ))}
        </div>
      )}

      <CreateProjectDialog open={dialogOpen} onOpenChange={setDialogOpen} />
    </div>
  );
}
```

- [ ] **Step 2: Verify TypeScript compiles**

```bash
cd flowdesk-web && npx tsc --noEmit
```
Expected: no errors

- [ ] **Step 3: Manual test**

Start the dev server (`npm run dev` in `flowdesk-web/`). Navigate to `/dashboard/projects`. Click "New Project" — dialog should open. Fill in name, select a client, submit. Project should appear in the list without page reload. Submit with empty name — validation error should appear inline. Close with Cancel or X — form resets.

- [ ] **Step 4: Commit**

```bash
git add flowdesk-web/src/app/(dashboard)/dashboard/projects/page.tsx
git commit -m "feat(projects): wire New Project button to CreateProjectDialog"
```

---

## Task 3: Settings update form

**Files:**
- Modify: `flowdesk-web/src/app/(dashboard)/dashboard/settings/page.tsx`

- [ ] **Step 1: Replace the entire file with this content**

```tsx
"use client";

import { useEffect } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { toast } from "sonner";
import { useAuth } from "@/contexts/AuthContext";
import { useOrganisation, useUpdateOrganisation } from "@/lib/queries";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { Badge } from "@/components/ui/badge";

const orgSchema = z.object({
  name: z.string().min(1, "Agency name is required"),
  primaryColor: z.string().nullable(),
});
type OrgFormData = z.infer<typeof orgSchema>;

export default function SettingsPage() {
  const { user } = useAuth();
  const { data: org } = useOrganisation();
  const updateOrg = useUpdateOrganisation();

  const { register, handleSubmit, reset, formState: { errors, isDirty, isSubmitting } } = useForm<OrgFormData>({
    resolver: zodResolver(orgSchema),
    defaultValues: { name: "", primaryColor: "#7c3aed" },
  });

  useEffect(() => {
    if (org) {
      reset({ name: org.name, primaryColor: org.primaryColor ?? "#7c3aed" });
    }
  }, [org, reset]);

  async function onSubmit(data: OrgFormData) {
    try {
      const updated = await updateOrg.mutateAsync(data);
      reset({ name: updated.name, primaryColor: updated.primaryColor ?? "#7c3aed" });
      toast.success("Agency settings saved");
    } catch {
      toast.error("Failed to save settings");
    }
  }

  return (
    <div className="p-6 max-w-2xl space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Settings</h1>
        <p className="text-muted-foreground text-sm mt-1">Manage your account and agency settings</p>
      </div>

      <Card className="border-0 shadow-sm">
        <CardHeader>
          <CardTitle className="text-base">Profile</CardTitle>
          <CardDescription>Your personal account details</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-2">
            <Label>Name</Label>
            <Input defaultValue={user?.name} disabled />
          </div>
          <div className="space-y-2">
            <Label>Email</Label>
            <Input defaultValue={user?.email} disabled />
          </div>
          <div className="space-y-2">
            <Label>Role</Label>
            <div><Badge variant="secondary">{user?.role}</Badge></div>
          </div>
        </CardContent>
      </Card>

      <Separator />

      <Card className="border-0 shadow-sm">
        <CardHeader>
          <CardTitle className="text-base">Agency</CardTitle>
          <CardDescription>Your organisation settings</CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="org-name">Agency name</Label>
              <Input id="org-name" {...register("name")} />
              {errors.name && <p className="text-xs text-destructive">{errors.name.message}</p>}
            </div>
            <div className="space-y-2">
              <Label htmlFor="primaryColor">Brand color</Label>
              <div className="flex items-center gap-3">
                <input
                  id="primaryColor"
                  type="color"
                  className="h-9 w-16 cursor-pointer rounded-md border border-input p-1 bg-background"
                  {...register("primaryColor")}
                />
                <span className="text-sm text-muted-foreground">Used across your client portal</span>
              </div>
            </div>
            <div className="flex gap-2 pt-2">
              <Button type="submit" size="sm" disabled={!isDirty || isSubmitting}>
                {isSubmitting ? "Saving…" : "Save changes"}
              </Button>
              <Button
                type="button"
                size="sm"
                variant="ghost"
                disabled={!isDirty}
                onClick={() => reset()}
              >
                Cancel
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>

      <Card className="border border-destructive/30 shadow-sm">
        <CardHeader>
          <CardTitle className="text-base text-destructive">Danger Zone</CardTitle>
          <CardDescription>Irreversible account actions</CardDescription>
        </CardHeader>
        <CardContent>
          <Button variant="destructive" disabled>Delete account</Button>
        </CardContent>
      </Card>
    </div>
  );
}
```

- [ ] **Step 2: Verify TypeScript compiles**

```bash
cd flowdesk-web && npx tsc --noEmit
```
Expected: no errors

- [ ] **Step 3: Manual test**

Navigate to `/dashboard/settings`. Agency name and brand color should load from the API (matching org data). Save/Cancel buttons should be disabled initially. Change the agency name — buttons become active. Click Save — toast appears, buttons go disabled again. Click Cancel on a dirty form — fields reset to last-saved values.

- [ ] **Step 4: Commit**

```bash
git add flowdesk-web/src/app/(dashboard)/dashboard/settings/page.tsx
git commit -m "feat(settings): add editable agency name and brand color form"
```

---

## Self-Review Checklist (completed)

- [x] Spec section 1 (CreateProjectDialog): covered by Task 1 + Task 2
- [x] Spec section 2 (Settings form): covered by Task 3
- [x] No-clients guard: present in Task 1 Step 1
- [x] Form dirty-state disabling Save/Cancel: present in Task 3 Step 1
- [x] `reset()` called on dialog close: present in `handleClose` in Task 1 Step 1
- [x] Org data loaded from `useOrganisation()` not `user.organisationName`: confirmed in Task 3
- [x] Placeholder text removed from settings page: confirmed, not present in Task 3 content
- [x] Projects page migrated from inline `useQuery` to `useProjects()`: confirmed in Task 2
