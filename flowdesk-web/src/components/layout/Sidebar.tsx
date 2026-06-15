"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { LayoutDashboard, FolderKanban, FileText, Settings, LogOut, UserPlus, Sun, Moon } from "lucide-react";
import { cn } from "@/lib/utils";
import { useAuth } from "@/contexts/AuthContext";
import { toast } from "sonner";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { useTheme } from "next-themes";
import { InviteDialog } from "@/components/InviteDialog";

const nav = [
  { href: "/dashboard", label: "Dashboard", icon: LayoutDashboard, exact: true },
  { href: "/dashboard/projects", label: "Projects", icon: FolderKanban, exact: false },
  { href: "/dashboard/invoices", label: "Invoices", icon: FileText, exact: false },
  { href: "/dashboard/settings", label: "Settings", icon: Settings, exact: false },
];

export function Sidebar() {
  const pathname = usePathname();
  const { user, logout } = useAuth();
  const router = useRouter();
  const { theme, setTheme } = useTheme();
  const [inviteOpen, setInviteOpen] = useState(false);

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
    <aside className="w-56 shrink-0 flex flex-col h-screen sticky top-0 bg-white dark:bg-[#111111] border-r border-gray-100 dark:border-white/[0.06]">
      {/* Logo */}
      <div className="flex items-center gap-2.5 px-4 h-14">
        <div className="size-7 rounded-lg bg-[#E05A2B] flex items-center justify-center shrink-0">
          <span className="text-white font-bold text-xs leading-none" style={{ fontFamily: "var(--font-fraunces)" }}>F</span>
        </div>
        <div className="min-w-0">
          <p className="font-semibold text-[13px] text-gray-900 dark:text-gray-100 truncate leading-tight">
            {user?.organisationName ?? "FlowDesk"}
          </p>
          <p className="text-[11px] text-gray-400 dark:text-gray-600 truncate leading-tight">{user?.role}</p>
        </div>
      </div>

      {/* Nav */}
      <nav className="flex-1 px-2 py-3 space-y-0.5 overflow-y-auto">
        {nav.map(({ href, label, icon: Icon, exact }) => {
          const active = exact ? pathname === href : pathname.startsWith(href);
          return (
            <Link
              key={href}
              href={href}
              className={cn(
                "flex items-center gap-2.5 px-3 py-2 rounded-lg text-[13.5px] font-medium transition-colors",
                active
                  ? "bg-[#FFF4EF] text-[#E05A2B] dark:bg-[#E05A2B]/12 dark:text-[#F5855A]"
                  : "text-gray-500 dark:text-gray-500 hover:bg-gray-50 dark:hover:bg-white/[0.04] hover:text-gray-800 dark:hover:text-gray-200"
              )}
            >
              <Icon className="size-4 shrink-0" />
              {label}
            </Link>
          );
        })}

        <button
          onClick={() => setInviteOpen(true)}
          className="w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-[13.5px] font-medium transition-colors text-gray-500 dark:text-gray-500 hover:bg-gray-50 dark:hover:bg-white/[0.04] hover:text-gray-800 dark:hover:text-gray-200"
        >
          <UserPlus className="size-4 shrink-0" />
          Invite
        </button>
      </nav>

      <InviteDialog open={inviteOpen} onOpenChange={setInviteOpen} />

      {/* User + theme toggle */}
      <div className="p-2 border-t border-gray-100 dark:border-white/[0.06]">
        <div className="flex items-center gap-2 px-2 py-2 rounded-lg">
          <div className="size-7 rounded-full bg-[#FFF4EF] dark:bg-[#E05A2B]/10 border border-[#F5C9B3] dark:border-[#E05A2B]/20 flex items-center justify-center shrink-0">
            <span className="text-[10px] font-bold text-[#E05A2B]">{initials}</span>
          </div>
          <div className="flex-1 min-w-0">
            <p className="text-[12px] font-medium text-gray-700 dark:text-gray-300 truncate leading-tight">{user?.name}</p>
            <p className="text-[11px] text-gray-400 dark:text-gray-600 truncate leading-tight">{user?.email}</p>
          </div>
          {/* Theme toggle */}
          <button
            onClick={() => setTheme(theme === "dark" ? "light" : "dark")}
            className="shrink-0 size-6 flex items-center justify-center rounded-md text-gray-400 dark:text-gray-600 hover:text-gray-600 dark:hover:text-gray-400 hover:bg-gray-100 dark:hover:bg-white/[0.06] transition-colors"
            title="Toggle theme"
          >
            {theme === "dark" ? <Sun className="size-3.5" /> : <Moon className="size-3.5" />}
          </button>
          <button
            onClick={handleLogout}
            className="shrink-0 size-6 flex items-center justify-center rounded-md text-gray-400 dark:text-gray-600 hover:text-gray-600 dark:hover:text-gray-400 hover:bg-gray-100 dark:hover:bg-white/[0.06] transition-colors"
            title="Sign out"
          >
            <LogOut className="size-3.5" />
          </button>
        </div>
      </div>
    </aside>
  );
}
