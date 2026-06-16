"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  LayoutDashboard, FolderKanban, FileText, Settings,
  LogOut, UserPlus, Sun, Moon, Menu, X,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { useAuth } from "@/contexts/AuthContext";
import { toast } from "sonner";
import { useRouter } from "next/navigation";
import { useState, useEffect } from "react";
import { motion } from "framer-motion";
import { useTheme } from "next-themes";
import { InviteDialog } from "@/components/InviteDialog";

const nav = [
  { href: "/dashboard",          label: "Dashboard", icon: LayoutDashboard, exact: true  },
  { href: "/dashboard/projects", label: "Projects",  icon: FolderKanban,    exact: false },
  { href: "/dashboard/invoices", label: "Invoices",  icon: FileText,        exact: false },
  { href: "/dashboard/settings", label: "Settings",  icon: Settings,        exact: false },
];

function SidebarContent({ onClose }: { onClose?: () => void }) {
  const pathname  = usePathname();
  const { user, logout } = useAuth();
  const router    = useRouter();
  const { theme, setTheme } = useTheme();
  const [inviteOpen, setInviteOpen] = useState(false);
  const [mounted, setMounted] = useState(false);
  useEffect(() => { setMounted(true); }, []);

  async function handleLogout() {
    try {
      await logout();
      router.push("/login");
    } catch {
      toast.error("Failed to sign out");
    }
  }

  const initials = user?.name
    ?.split(" ").map((n) => n[0]).join("").slice(0, 2).toUpperCase() ?? "??";

  return (
    <div className="flex flex-col h-full">
      {/* Logo + org */}
      <div className="flex items-center gap-3 px-5 py-5 border-b border-gray-100 dark:border-white/[0.06]">
        <div className="size-10 rounded-xl bg-[#E05A2B] flex items-center justify-center shrink-0 shadow-sm">
          <span className="text-white font-bold text-base leading-none" style={{ fontFamily: "var(--font-fraunces)" }}>
            F
          </span>
        </div>
        <div className="min-w-0">
          <p className="font-bold text-[15px] text-gray-900 dark:text-gray-100 truncate leading-tight">
            {user?.organisationName ?? "FlowDesk"}
          </p>
          <p className="text-xs text-gray-400 dark:text-gray-500 truncate leading-tight mt-0.5">{user?.role}</p>
        </div>
        {/* Close button — mobile only */}
        {onClose && (
          <button
            onClick={onClose}
            className="ml-auto shrink-0 size-7 flex items-center justify-center rounded-lg text-gray-400 hover:text-gray-600 hover:bg-gray-100 dark:hover:bg-white/[0.06] transition-colors"
          >
            <X className="size-4" />
          </button>
        )}
      </div>

      {/* Nav */}
      <nav className="flex-1 px-3 py-4 space-y-0.5 overflow-y-auto">
        {nav.map(({ href, label, icon: Icon, exact }, i) => {
          const active = exact ? pathname === href : pathname.startsWith(href);
          return (
            <motion.div
              key={href}
              initial={{ opacity: 0, scale: 0.75 }}
              animate={mounted ? { opacity: 1, scale: 1 } : {}}
              transition={{ type: "spring", stiffness: 380, damping: 26, mass: 0.8, delay: i * 0.06 }}
            >
              <Link
                href={href}
                onClick={onClose}
                className={cn(
                  "relative flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium transition-colors",
                  active
                    ? "text-[#E05A2B] dark:text-[#F5855A]"
                    : "text-gray-500 dark:text-gray-500 hover:bg-gray-50 dark:hover:bg-white/[0.04] hover:text-gray-800 dark:hover:text-gray-200"
                )}
              >
                {active && (
                  <motion.div
                    layoutId={onClose ? undefined : "nav-indicator"}
                    className="absolute inset-0 rounded-xl bg-[#FFF4EF] dark:bg-[#E05A2B]/12"
                    transition={{ type: "spring", stiffness: 380, damping: 26 }}
                  />
                )}
                <Icon className="size-[18px] shrink-0 relative z-10" />
                <span className="relative z-10">{label}</span>
              </Link>
            </motion.div>
          );
        })}

        <button
          onClick={() => { setInviteOpen(true); }}
          className="w-full flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium transition-colors text-gray-500 dark:text-gray-500 hover:bg-gray-50 dark:hover:bg-white/[0.04] hover:text-gray-800 dark:hover:text-gray-200"
        >
          <UserPlus className="size-[18px] shrink-0" />
          Invite
        </button>
      </nav>

      <InviteDialog open={inviteOpen} onOpenChange={setInviteOpen} />

      {/* User + theme toggle */}
      <div className="p-3 border-t border-gray-100 dark:border-white/[0.06]">
        <div className="flex items-center gap-2.5 px-2 py-2 rounded-xl">
          <div className="size-8 rounded-full bg-[#FFF4EF] dark:bg-[#E05A2B]/10 border border-[#F5C9B3] dark:border-[#E05A2B]/20 flex items-center justify-center shrink-0">
            <span className="text-xs font-bold text-[#E05A2B]">{initials}</span>
          </div>
          <div className="flex-1 min-w-0">
            <p className="text-[13px] font-medium text-gray-700 dark:text-gray-300 truncate leading-tight">{user?.name}</p>
            <p className="text-[11px] text-gray-400 dark:text-gray-600 truncate leading-tight">{user?.email}</p>
          </div>
          <button
            onClick={() => setTheme(theme === "dark" ? "light" : "dark")}
            className="shrink-0 size-7 flex items-center justify-center rounded-lg text-gray-400 dark:text-gray-600 hover:text-gray-600 dark:hover:text-gray-400 hover:bg-gray-100 dark:hover:bg-white/[0.06] transition-colors"
            title="Toggle theme"
          >
            {theme === "dark" ? <Sun className="size-4" /> : <Moon className="size-4" />}
          </button>
          <button
            onClick={handleLogout}
            className="shrink-0 size-7 flex items-center justify-center rounded-lg text-gray-400 dark:text-gray-600 hover:text-gray-600 dark:hover:text-gray-400 hover:bg-gray-100 dark:hover:bg-white/[0.06] transition-colors"
            title="Sign out"
          >
            <LogOut className="size-4" />
          </button>
        </div>
      </div>
    </div>
  );
}

export function Sidebar() {
  const [mobileOpen, setMobileOpen] = useState(false);
  const pathname = usePathname();

  // Close drawer on route change
  useEffect(() => { setMobileOpen(false); }, [pathname]);

  // Lock body scroll when drawer is open
  useEffect(() => {
    document.body.style.overflow = mobileOpen ? "hidden" : "";
    return () => { document.body.style.overflow = ""; };
  }, [mobileOpen]);

  return (
    <>
      {/* ── Desktop / Laptop sidebar (≥1024px) ── */}
      <aside className="hidden lg:flex w-60 shrink-0 flex-col h-screen sticky top-0 bg-white dark:bg-[#111111] border-r border-gray-100 dark:border-white/[0.06]">
        <SidebarContent />
      </aside>

      {/* ── Tablet / Mobile top bar (<1024px) ── */}
      <header className="lg:hidden fixed top-0 inset-x-0 z-40 h-14 bg-white dark:bg-[#111111] border-b border-gray-100 dark:border-white/[0.06] flex items-center px-4 gap-3">
        <button
          onClick={() => setMobileOpen(true)}
          className="size-9 flex items-center justify-center rounded-xl text-gray-500 hover:bg-gray-100 dark:hover:bg-white/[0.06] transition-colors"
        >
          <Menu className="size-5" />
        </button>
        <div className="flex items-center gap-2.5">
          <div className="size-7 rounded-lg bg-[#E05A2B] flex items-center justify-center">
            <span className="text-white font-bold text-xs" style={{ fontFamily: "var(--font-fraunces)" }}>F</span>
          </div>
          <span className="font-bold text-[15px] text-gray-900 dark:text-gray-100" style={{ fontFamily: "var(--font-fraunces)" }}>
            FlowDesk
          </span>
        </div>
      </header>

      {/* ── Mobile drawer overlay ── */}
      {mobileOpen && (
        <div
          className="lg:hidden fixed inset-0 z-50 flex"
          onClick={() => setMobileOpen(false)}
        >
          {/* Backdrop */}
          <div className="absolute inset-0 bg-black/40 backdrop-blur-sm" />

          {/* Drawer panel */}
          <aside
            className="relative w-72 max-w-[85vw] h-full bg-white dark:bg-[#111111] shadow-2xl flex flex-col"
            onClick={(e) => e.stopPropagation()}
          >
            <SidebarContent onClose={() => setMobileOpen(false)} />
          </aside>
        </div>
      )}
    </>
  );
}
