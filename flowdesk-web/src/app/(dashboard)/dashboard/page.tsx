"use client";

import { useAuth } from "@/contexts/AuthContext";
import { FolderKanban, Users, FileText, TrendingUp } from "lucide-react";
import { Card, CardContent } from "@/components/ui/card";

const stats = [
  { label: "Active Projects", value: "0", icon: FolderKanban },
  { label: "Active Clients", value: "0", icon: Users },
  { label: "Pending Invoices", value: "0", icon: FileText },
  { label: "Revenue (MTD)", value: "$0", icon: TrendingUp },
];

export default function DashboardPage() {
  const { user } = useAuth();

  return (
    <div className="p-8 space-y-8">
      {/* Header */}
      <div className="border-b border-border pb-6">
        <p className="text-xs font-semibold uppercase tracking-[0.15em] text-muted-foreground mb-1">
          {user?.organisationName}
        </p>
        <h1
          className="text-4xl font-bold text-foreground leading-tight"
          style={{ fontFamily: "var(--font-fraunces)" }}
        >
          Welcome back,<br />
          <span className="text-primary">{user?.name?.split(" ")[0]}.</span>
        </h1>
      </div>

      {/* Stat cards */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        {stats.map(({ label, value, icon: Icon }) => (
          <Card key={label} className="border shadow-none bg-card hover:shadow-sm transition-shadow">
            <CardContent className="p-5">
              <div className="flex items-center justify-between mb-4">
                <span className="text-xs font-semibold uppercase tracking-widest text-muted-foreground">{label}</span>
                <Icon className="size-4 text-primary/60" />
              </div>
              <p
                className="text-3xl font-bold text-foreground"
                style={{ fontFamily: "var(--font-fraunces)" }}
              >
                {value}
              </p>
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Recent activity */}
      <Card className="border shadow-none bg-card">
        <CardContent className="p-5">
          <h2
            className="text-lg font-bold text-foreground mb-4"
            style={{ fontFamily: "var(--font-fraunces)" }}
          >
            Recent Activity
          </h2>
          <div className="py-8 text-center">
            <div className="size-12 rounded-full bg-primary/8 flex items-center justify-center mx-auto mb-3">
              <FolderKanban className="size-5 text-primary/50" />
            </div>
            <p className="text-sm font-medium text-foreground">Nothing here yet</p>
            <p className="text-xs text-muted-foreground mt-1">Activity will appear as you use FlowDesk.</p>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
