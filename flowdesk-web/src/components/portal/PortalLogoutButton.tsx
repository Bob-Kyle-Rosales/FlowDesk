"use client";

import { useRouter } from "next/navigation";
import { useAuth } from "@/contexts/AuthContext";

export function PortalLogoutButton() {
  const { logout } = useAuth();
  const router = useRouter();

  async function handleLogout() {
    await logout();
    router.push("/login");
  }

  return (
    <button
      onClick={handleLogout}
      className="text-white/70 hover:text-white text-sm transition-colors"
    >
      Log out
    </button>
  );
}
