"use client";

import { Badge } from "@/components/ui/badge";
import type { Project, ProjectStatsResponse } from "@/types";

const statusStyles: Record<Project["status"], string> = {
  Active: "bg-emerald-50 text-emerald-700 border-emerald-200",
  Paused: "bg-amber-50 text-amber-700 border-amber-200",
  Completed: "bg-gray-100 text-gray-600 border-gray-200",
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
    <div className="space-y-4">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-foreground" style={{ fontFamily: "var(--font-fraunces)" }}>
            {project.name}
          </h1>
          {project.description && (
            <p className="text-sm text-muted-foreground mt-1">{project.description}</p>
          )}
          <p className="text-xs text-muted-foreground mt-1 font-medium">{project.clientName}</p>
        </div>
        <Badge variant="outline" className={`shrink-0 rounded-full border text-xs ${statusStyles[project.status]}`}>
          {project.status}
        </Badge>
      </div>

      <div className="space-y-1">
        <div className="flex items-center justify-between">
          <span className="text-xs text-muted-foreground">{milestoneText}</span>
          <span className="text-xs font-semibold text-foreground">{percent}%</span>
        </div>
        <div className="h-1.5 bg-muted rounded-full overflow-hidden">
          <div
            className="h-full bg-primary rounded-full transition-all duration-500"
            style={{ width: `${percent}%` }}
          />
        </div>
      </div>
    </div>
  );
}
