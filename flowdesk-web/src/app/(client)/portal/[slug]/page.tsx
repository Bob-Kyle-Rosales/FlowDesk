"use client";

import { use, useEffect } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { useAuth } from "@/contexts/AuthContext";
import { useProjects, useProjectStats } from "@/lib/queries";
import { Badge } from "@/components/ui/badge";
import type { Project } from "@/types";

const statusColor: Record<string, string> = {
  Active: "bg-green-100 text-green-700",
  Paused: "bg-yellow-100 text-yellow-700",
  Completed: "bg-blue-100 text-blue-700",
};

function ProjectCard({ project, slug }: { project: Project; slug: string }) {
  const { data: stats } = useProjectStats(project.id);

  return (
    <Link href={`/portal/${slug}/projects/${project.id}`} className="block group">
      <div className="bg-white rounded-xl border shadow-sm p-5 group-hover:shadow-md transition-shadow">
        <div className="flex items-start justify-between mb-3">
          <h3 className="font-semibold text-base">{project.name}</h3>
          <Badge className={statusColor[project.status] ?? "bg-gray-100 text-gray-600"}>
            {project.status}
          </Badge>
        </div>
        {project.description && (
          <p className="text-sm text-muted-foreground mb-3 line-clamp-2">{project.description}</p>
        )}
        {stats && (
          <div className="space-y-1">
            <div className="h-1.5 bg-muted rounded-full overflow-hidden">
              <div
                className="h-full bg-violet-500 transition-all"
                style={{ width: `${stats.progressPercent}%` }}
              />
            </div>
            <p className="text-xs text-muted-foreground">
              {stats.completedMilestones}/{stats.milestoneCount} milestones complete
            </p>
          </div>
        )}
      </div>
    </Link>
  );
}

export default function PortalPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = use(params);
  const router = useRouter();
  const { user, loading } = useAuth();
  const { data: projects = [], isLoading } = useProjects();

  useEffect(() => {
    if (!loading && !user) router.push("/login");
    else if (!loading && user && user.organisationSlug !== slug) {
      router.push(`/portal/${user.organisationSlug}`);
    }
  }, [user, loading, router, slug]);

  if (loading || isLoading) {
    return (
      <p className="text-sm text-muted-foreground animate-pulse py-8 text-center">
        Loading projects…
      </p>
    );
  }

  if (!projects.length) {
    return (
      <div className="text-center py-16">
        <p className="text-muted-foreground text-sm">No projects yet.</p>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {projects.map((project) => (
        <ProjectCard key={project.id} project={project} slug={slug} />
      ))}
    </div>
  );
}
