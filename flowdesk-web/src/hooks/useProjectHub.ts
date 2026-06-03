"use client";

import { useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import * as signalR from "@microsoft/signalr";

export function useProjectHub(projectId: string) {
  const queryClient = useQueryClient();

  useEffect(() => {
    const url = `${process.env.NEXT_PUBLIC_SIGNALR_URL ?? "http://localhost:5269"}/hubs/project`;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(url, { withCredentials: true })
      .withAutomaticReconnect()
      .build();

    connection.on("OnMilestoneUpdated", () => {
      queryClient.invalidateQueries({ queryKey: ["projects", projectId, "milestones"] });
      queryClient.invalidateQueries({ queryKey: ["projects", projectId, "stats"] });
    });

    connection.on("OnDeliverableUpdated", () => {
      queryClient.invalidateQueries({ queryKey: ["projects", projectId, "deliverables"] });
      queryClient.invalidateQueries({ queryKey: ["projects", projectId, "stats"] });
    });

    connection
      .start()
      .then(() => connection.invoke("JoinProject", projectId))
      .catch(() => {});

    return () => {
      connection.invoke("LeaveProject", projectId).catch(() => {});
      connection.stop();
    };
  }, [projectId, queryClient]);
}
