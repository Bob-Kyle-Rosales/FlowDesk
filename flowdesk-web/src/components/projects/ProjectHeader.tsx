"use client";

import type { Project, ProjectStatsResponse } from "@/types";

const statusStyles: Record<Project["status"], string> = {
  Active:    "bg-emerald-50 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-400",
  Paused:    "bg-amber-50 text-amber-700 dark:bg-amber-500/10 dark:text-amber-400",
  Completed: "bg-gray-100 text-gray-500 dark:bg-white/5 dark:text-gray-500",
};

interface Props {
  project: Project;
  stats?: ProjectStatsResponse;
}

export function ProjectHeader({ project, stats }: Props) {
  const percent = stats?.progressPercent ?? 0;
  const milestoneText = stats
    ? `${stats.completedMilestones} of ${stats.milestoneCount} milestones complete`
    : "";

  return (
    <div className="bg-white dark:bg-[#1A1A1A] rounded-2xl border border-gray-100 dark:border-white/[0.06] p-5 space-y-4">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h1 className="text-xl font-bold text-gray-900 dark:text-gray-100 tracking-tight">
            {project.name}
          </h1>
          {project.description && (
            <p className="text-sm text-gray-500 dark:text-gray-500 mt-1">{project.description}</p>
          )}
          <p className="text-xs text-gray-400 dark:text-gray-600 mt-1">{project.clientName}</p>
        </div>
        <span className={`shrink-0 text-[11px] font-medium px-2.5 py-0.5 rounded-full ${statusStyles[project.status]}`}>
          {project.status}
        </span>
      </div>

      <div className="space-y-1.5">
        <div className="flex items-center justify-between">
          <span className="text-xs text-gray-400 dark:text-gray-600">{milestoneText}</span>
          <span className="text-xs font-semibold text-gray-700 dark:text-gray-400">{percent}%</span>
        </div>
        <div className="h-1.5 bg-gray-100 dark:bg-white/[0.06] rounded-full overflow-hidden">
          <div
            className="h-full rounded-full bg-gradient-to-r from-[#E05A2B] to-[#F5855A] transition-all duration-500"
            style={{ width: `${percent}%` }}
          />
        </div>
      </div>
    </div>
  );
}
