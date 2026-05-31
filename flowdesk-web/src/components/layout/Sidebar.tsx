"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { LayoutDashboard, FolderKanban, FileText, Settings, LogOut } from "lucide-react";
import { cn } from "@/lib/utils";
import { useAuth } from "@/contexts/AuthContext";
import { Separator } from "@/components/ui/separator";
import { toast } from "sonner";
import { useRouter } from "next/navigation";

const nav = [
  { href: "/dashboard", label: "Dashboard", icon: LayoutDashboard },
  { href: "/dashboard/projects", label: "Projects", icon: FolderKanban },
  { href: "/dashboard/invoices", label: "Invoices", icon: FileText },
  { href: "/dashboard/settings", label: "Settings", icon: Settings },
];

// Dot grid rendered as inline style — Tailwind can't generate arbitrary radial-gradient values
const dotGrid: React.CSSProperties = {
  backgroundImage:
    "radial-gradient(circle, rgba(167,139,250,0.15) 1px, transparent 1px)",
  backgroundSize: "20px 20px",
};

export function Sidebar() {
  const pathname = usePathname();
  const { user, logout } = useAuth();
  const router = useRouter();

  async function handleLogout() {
    try {
      await logout();
      router.push("/login");
    } catch {
      toast.error("Failed to sign out");
    }
  }

  const initials = user?.name
    ?.split(" ")
    .map((n) => n[0])
    .join("")
    .slice(0, 2)
    .toUpperCase() ?? "??";

  return (
    <aside
      className="w-60 shrink-0 flex flex-col h-screen sticky top-0 bg-gradient-to-b from-violet-950 via-violet-900 to-violet-800"
      style={dotGrid}
    >
      {/* Logo */}
      <div className="flex items-center gap-3 px-5 h-16">
        <div className="size-8 rounded-lg bg-violet-400/20 ring-1 ring-violet-400/30 shadow-lg shadow-violet-950 flex items-center justify-center">
          <span className="text-violet-300 font-bold text-base leading-none">◆</span>
        </div>
        <div className="min-w-0">
          <p className="font-semibold text-sm text-white truncate">{user?.organisationName ?? "FlowDesk"}</p>
          <p className="text-xs text-violet-300/70 truncate">{user?.role}</p>
        </div>
      </div>

      <Separator className="bg-white/10" />

      {/* Nav */}
      <nav className="flex-1 px-3 py-4 space-y-0.5">
        {nav.map(({ href, label, icon: Icon }) => {
          const active = pathname === href;
          return (
            <Link
              key={href}
              href={href}
              className={cn(
                "flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-colors relative",
                active
                  ? "bg-white/15 text-white border-l-2 border-violet-400 pl-[10px]"
                  : "text-violet-200/70 hover:bg-white/8 hover:text-violet-100"
              )}
            >
              <Icon className={cn("size-4 shrink-0", active ? "text-violet-300" : "text-violet-400/60")} />
              {label}
            </Link>
          );
        })}
      </nav>

      {/* User */}
      <Separator className="bg-white/10" />
      <div className="p-4 flex items-center gap-3">
        <div className="size-8 rounded-full bg-violet-400/20 ring-1 ring-violet-400/30 flex items-center justify-center shrink-0">
          <span className="text-xs font-semibold text-violet-200">{initials}</span>
        </div>
        <div className="flex-1 min-w-0">
          <p className="text-sm font-medium text-white truncate">{user?.name}</p>
          <p className="text-xs text-violet-300/60 truncate">{user?.email}</p>
        </div>
        <button
          onClick={handleLogout}
          className="text-violet-400/50 hover:text-violet-200 transition-colors"
          title="Sign out"
        >
          <LogOut className="size-4" />
        </button>
      </div>
    </aside>
  );
}
