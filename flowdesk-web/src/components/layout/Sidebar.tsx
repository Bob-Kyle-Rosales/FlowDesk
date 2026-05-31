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
    <aside className="w-64 shrink-0 flex flex-col h-screen sticky top-0 bg-[#2C1A0E]">
      {/* Logo */}
      <div className="flex items-center gap-3 px-5 h-16">
        <div className="size-8 rounded-md bg-[#E05A2B] flex items-center justify-center shadow-sm">
          <span className="text-white font-bold text-sm leading-none" style={{ fontFamily: "var(--font-fraunces)" }}>F</span>
        </div>
        <div className="min-w-0">
          <p className="font-semibold text-sm text-[#F5EDE4] truncate tracking-wide">{user?.organisationName ?? "FlowDesk"}</p>
          <p className="text-xs text-[#A07855]/70 truncate">{user?.role}</p>
        </div>
      </div>

      <Separator className="bg-white/8" />

      {/* Nav */}
      <nav className="flex-1 px-3 py-5 space-y-0.5">
        {nav.map(({ href, label, icon: Icon }) => {
          const active = pathname === href;
          return (
            <Link
              key={href}
              href={href}
              className={cn(
                "flex items-center gap-3 px-3 py-2.5 rounded-md text-sm font-medium transition-colors",
                active
                  ? "bg-[#E05A2B] text-white"
                  : "text-[#C4A882]/70 hover:bg-white/6 hover:text-[#F5EDE4]"
              )}
            >
              <Icon className="size-4 shrink-0" />
              {label}
            </Link>
          );
        })}
      </nav>

      {/* User */}
      <Separator className="bg-white/8" />
      <div className="p-4 flex items-center gap-3">
        <div className="size-8 rounded-full bg-[#E05A2B]/20 border border-[#E05A2B]/30 flex items-center justify-center shrink-0">
          <span className="text-xs font-semibold text-[#E05A2B]">{initials}</span>
        </div>
        <div className="flex-1 min-w-0">
          <p className="text-sm font-medium text-[#F5EDE4] truncate">{user?.name}</p>
          <p className="text-xs text-[#A07855]/60 truncate">{user?.email}</p>
        </div>
        <button
          onClick={handleLogout}
          className="text-[#A07855]/50 hover:text-[#F5EDE4] transition-colors"
          title="Sign out"
        >
          <LogOut className="size-4" />
        </button>
      </div>
    </aside>
  );
}
