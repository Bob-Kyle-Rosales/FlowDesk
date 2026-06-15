"use client";

import { useState } from "react";
import { useProjects } from "@/lib/queries";
import { Project } from "@/types";
import { Button } from "@/components/ui/button";
import { Plus, FolderKanban } from "lucide-react";
import Link from "next/link";
import { CreateProjectDialog } from "@/components/projects/CreateProjectDialog";

const statusStyles: Record<Project["status"], string> = {
  Active:    "bg-emerald-50 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-400",
  Paused:    "bg-amber-50 text-amber-700 dark:bg-amber-500/10 dark:text-amber-400",
  Completed: "bg-gray-100 text-gray-500 dark:bg-white/5 dark:text-gray-500",
};

const progressColor: Record<Project["status"], string> = {
  Active:    "from-[#E05A2B] to-[#F5855A]",
  Paused:    "from-amber-400 to-amber-300",
  Completed: "from-emerald-500 to-emerald-400",
};

export default function ProjectsPage() {
  const { data: projects, isLoading, isError } = useProjects();
  const [dialogOpen, setDialogOpen] = useState(false);

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-bold text-gray-900 dark:text-gray-100 tracking-tight">Projects</h1>
          <p className="text-sm text-gray-400 dark:text-gray-600 mt-0.5">Manage all your client projects</p>
        </div>
        <Button
          size="sm"
          className="gap-1.5 bg-[#E05A2B] hover:bg-[#C94E22] text-white border-none shadow-sm"
          onClick={() => setDialogOpen(true)}
        >
          <Plus className="size-3.5" /> New Project
        </Button>
      </div>

      {isLoading && (
        <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {Array.from({ length: 6 }).map((_, i) => (
            <div key={i} className="bg-white dark:bg-[#1A1A1A] rounded-2xl border border-gray-100 dark:border-white/[0.06] p-5 animate-pulse">
              <div className="h-4 bg-gray-100 dark:bg-white/5 rounded w-2/3 mb-3" />
              <div className="h-3 bg-gray-100 dark:bg-white/5 rounded w-1/3 mb-5" />
              <div className="h-1.5 bg-gray-100 dark:bg-white/5 rounded-full" />
            </div>
          ))}
        </div>
      )}

      {(isError || (projects && projects.length === 0)) && (
        <div className="bg-white dark:bg-[#1A1A1A] rounded-2xl border border-gray-100 dark:border-white/[0.06] py-16 text-center">
          <div className="size-12 rounded-2xl bg-[#FFF4EF] dark:bg-[#E05A2B]/10 flex items-center justify-center mx-auto mb-3">
            <FolderKanban className="size-5 text-[#E05A2B]" />
          </div>
          <p className="text-sm font-medium text-gray-700 dark:text-gray-300">
            {isError ? "Could not load projects" : "No projects yet"}
          </p>
          <p className="text-xs text-gray-400 dark:text-gray-600 mt-1">
            {isError ? "Check your connection and try again." : "Create your first project to get started."}
          </p>
        </div>
      )}

      {projects && projects.length > 0 && (
        <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {projects.map((project) => (
            <Link key={project.id} href={`/dashboard/projects/${project.id}`}>
              <div className="group bg-white dark:bg-[#1A1A1A] rounded-2xl border border-gray-100 dark:border-white/[0.06] p-5 hover:shadow-md dark:hover:shadow-black/30 hover:-translate-y-0.5 transition-all duration-200 cursor-pointer">
                <div className="flex items-start justify-between gap-2 mb-1.5">
                  <p className="text-[14px] font-semibold text-gray-900 dark:text-gray-100 leading-snug">
                    {project.name}
                  </p>
                  <span className={`shrink-0 text-[11px] font-medium px-2.5 py-0.5 rounded-full ${statusStyles[project.status]}`}>
                    {project.status}
                  </span>
                </div>
                <p className="text-[12px] text-gray-400 dark:text-gray-600 mb-4">{project.clientName}</p>
                {project.description && (
                  <p className="text-[12.5px] text-gray-500 dark:text-gray-500 line-clamp-2 mb-4">{project.description}</p>
                )}
                <div className="h-1.5 bg-gray-100 dark:bg-white/[0.06] rounded-full overflow-hidden">
                  <div
                    className={`h-full rounded-full bg-gradient-to-r ${progressColor[project.status]}`}
                    style={{ width: "40%" }}
                  />
                </div>
              </div>
            </Link>
          ))}
        </div>
      )}

      <CreateProjectDialog open={dialogOpen} onOpenChange={setDialogOpen} />
    </div>
  );
}
