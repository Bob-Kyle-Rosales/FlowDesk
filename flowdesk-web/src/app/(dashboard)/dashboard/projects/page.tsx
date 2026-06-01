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

      {isError && !projects && (
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
                  {project.description && (
                    <p className="text-sm text-muted-foreground line-clamp-2">{project.description}</p>
                  )}
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
