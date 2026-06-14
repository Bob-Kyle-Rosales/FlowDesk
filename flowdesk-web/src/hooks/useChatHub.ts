"use client";

import { useEffect, useRef, useState } from "react";
import * as signalR from "@microsoft/signalr";
import type { Message } from "@/types";
import api from "@/lib/api";

type ConnectionState = "connecting" | "connected" | "disconnected";

async function getAccessToken(): Promise<string> {
  const res = await api.get<{ accessToken: string }>("/api/auth/token");
  return res.data.accessToken;
}

export function useChatHub(projectId: string) {
  const [liveMessages, setLiveMessages] = useState<Message[]>([]);
  const [connectionState, setConnectionState] = useState<ConnectionState>("connecting");
  const connectionRef = useRef<signalR.HubConnection | null>(null);

  useEffect(() => {
    const url = `${process.env.NEXT_PUBLIC_SIGNALR_URL ?? "http://localhost:5269"}/hubs/chat`;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(url, { accessTokenFactory: getAccessToken })
      .withAutomaticReconnect()
      .build();

    connectionRef.current = connection;

    connection.on("ReceiveMessage", (message: Message) => {
      setLiveMessages(prev => [...prev, message]);
    });

    connection.onreconnecting(() => setConnectionState("connecting"));
    connection.onreconnected(() => setConnectionState("connected"));
    connection.onclose(() => setConnectionState("disconnected"));

    connection
      .start()
      .then(() => {
        setConnectionState("connected");
        return connection.invoke("JoinProject", projectId);
      })
      .catch(() => setConnectionState("disconnected"));

    return () => {
      connection.invoke("LeaveProject", projectId).catch(() => {});
      connection.stop();
    };
  }, [projectId]);

  return { liveMessages, connectionState };
}
