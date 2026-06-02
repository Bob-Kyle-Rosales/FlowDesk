"use client";

import { use } from "react";
import Link from "next/link";
import { ArrowLeft } from "lucide-react";
import { useProject, useProjectStats } from "@/lib/queries";
import { ProjectHeader } from "@/components/projects/ProjectHeader";
import { MilestonesTab } from "@/components/projects/MilestonesTab";
import { DeliverablesTab } from "@/components/projects/DeliverablesTab";
import { MessagesTab } from "@/components/projects/MessagesTab";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";

export default function ProjectDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const { data: project, isLoading, isError } = useProject(id);
  const { data: stats } = useProjectStats(id);

  if (isLoading) {
    return <div className="p-8 text-sm text-muted-foreground animate-pulse">Loading project...</div>;
  }

  if (isError || !project) {
    return (
      <div className="p-8 space-y-4">
        <Link href="/dashboard/projects" className="flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground transition-colors w-fit">
          <ArrowLeft className="size-4" /> Projects
        </Link>
        <p className="text-foreground font-medium">Project not found.</p>
      </div>
    );
  }

  return (
    <div className="p-6 space-y-6">
      <Link href="/dashboard/projects" className="flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground transition-colors w-fit">
        <ArrowLeft className="size-4" /> Projects
      </Link>

      <ProjectHeader project={project} stats={stats} />

      <Tabs defaultValue="milestones">
        <TabsList>
          <TabsTrigger value="milestones">Milestones</TabsTrigger>
          <TabsTrigger value="deliverables">Deliverables</TabsTrigger>
          <TabsTrigger value="messages">Messages</TabsTrigger>
        </TabsList>

        <TabsContent value="milestones">
          <MilestonesTab projectId={id} />
        </TabsContent>

        <TabsContent value="deliverables">
          <DeliverablesTab projectId={id} />
        </TabsContent>

        <TabsContent value="messages">
          <MessagesTab projectId={id} />
        </TabsContent>
      </Tabs>
    </div>
  );
}
