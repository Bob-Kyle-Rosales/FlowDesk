"use client";

import { useAuth } from "@/contexts/AuthContext";
import { FolderKanban, Users, FileText, TrendingUp } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

const stats = [
  {
    label: "Active Projects",
    value: "—",
    icon: FolderKanban,
    gradient: "from-blue-500 to-blue-600",
  },
  {
    label: "Active Clients",
    value: "—",
    icon: Users,
    gradient: "from-emerald-500 to-emerald-600",
  },
  {
    label: "Pending Invoices",
    value: "—",
    icon: FileText,
    gradient: "from-amber-500 to-amber-600",
  },
  {
    label: "Revenue (MTD)",
    value: "—",
    icon: TrendingUp,
    gradient: "from-violet-500 to-violet-600",
  },
];

export default function DashboardPage() {
  const { user } = useAuth();

  return (
    <div className="p-6 space-y-6">
      {/* Banner */}
      <div className="relative overflow-hidden rounded-xl bg-gradient-to-r from-violet-600 to-violet-500 p-6 text-white">
        <div className="relative z-10">
          <p className="text-violet-200 text-sm font-medium mb-1">{user?.organisationName}</p>
          <h1 className="text-2xl font-extrabold tracking-tight">
            Welcome back, {user?.name?.split(" ")[0]} 👋
          </h1>
          <p className="text-violet-200 text-sm mt-1">
            Here&apos;s what&apos;s happening with your clients today.
          </p>
        </div>
        {/* Decorative watermark */}
        <span className="pointer-events-none select-none absolute right-6 top-1/2 -translate-y-1/2 text-[7rem] leading-none text-white/5 font-bold">
          ◆
        </span>
      </div>

      {/* Stat cards */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        {stats.map(({ label, value, icon: Icon, gradient }) => (
          <div
            key={label}
            className={`bg-gradient-to-br ${gradient} rounded-xl p-5 shadow-md text-white`}
          >
            <div className="flex items-start justify-between mb-3">
              <p className="text-xs font-semibold uppercase tracking-widest text-white/70">{label}</p>
              <Icon className="size-4 text-white/60 shrink-0" />
            </div>
            <p className="text-3xl font-extrabold tracking-tight">{value}</p>
            <p className="text-xs text-white/60 mt-1">Coming in Phase 2</p>
          </div>
        ))}
      </div>

      {/* Recent activity */}
      <Card className="border-0 shadow-sm">
        <CardHeader className="px-5 pt-5">
          <CardTitle className="text-sm font-semibold text-gray-900">Recent Activity</CardTitle>
        </CardHeader>
        <CardContent className="px-5 pb-8 text-center">
          <div className="size-12 rounded-full bg-violet-50 flex items-center justify-center mx-auto mb-3">
            <FolderKanban className="size-5 text-violet-400" />
          </div>
          <p className="text-sm font-medium text-gray-700">No activity yet</p>
          <p className="text-xs text-muted-foreground mt-1">Activity feed coming in Phase 2.</p>
        </CardContent>
      </Card>
    </div>
  );
}
