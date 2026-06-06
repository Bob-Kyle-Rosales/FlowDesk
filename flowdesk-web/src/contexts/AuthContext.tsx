// flowdesk-web/src/contexts/AuthContext.tsx
"use client";

import { createContext, useContext, useState, useEffect, ReactNode } from "react";
import api from "@/lib/api";
import { AuthResponse } from "@/types";

interface AuthContextValue {
  user: AuthResponse | null;
  loading: boolean;
  login: (email: string, password: string) => Promise<AuthResponse>;
  register: (name: string, email: string, password: string, organisationName: string) => Promise<AuthResponse>;
  logout: () => Promise<void>;
  setUser: (user: AuthResponse) => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthResponse | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    api.post<AuthResponse>("/api/auth/refresh")
      .then((res) => setUser(res.data))
      .catch(() => setUser(null))
      .finally(() => setLoading(false));
  }, []);

  async function login(email: string, password: string): Promise<AuthResponse> {
    const res = await api.post<AuthResponse>("/api/auth/login", { email, password });
    setUser(res.data);
    return res.data;
  }

  async function register(name: string, email: string, password: string, organisationName: string): Promise<AuthResponse> {
    const res = await api.post<AuthResponse>("/api/auth/register", {
      name, email, password, organisationName,
    });
    setUser(res.data);
    return res.data;
  }

  async function logout() {
    await api.post("/api/auth/logout");
    setUser(null);
  }

  return (
    <AuthContext.Provider value={{ user, loading, login, register, logout, setUser }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used inside AuthProvider");
  return ctx;
}
