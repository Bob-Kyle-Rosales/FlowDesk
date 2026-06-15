"use client";

import { useState } from "react";
import Link from "next/link";
import { useInvoices } from "@/lib/queries";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Plus, FileText } from "lucide-react";
import { CreateInvoiceDialog } from "@/components/invoices/CreateInvoiceDialog";
import type { Invoice } from "@/types";

const statusColor: Record<Invoice["status"], string> = {
  Draft:   "bg-gray-100 text-gray-500 dark:bg-white/5 dark:text-gray-500",
  Sent:    "bg-blue-50 text-blue-700 dark:bg-blue-500/10 dark:text-blue-400",
  Paid:    "bg-emerald-50 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-400",
  Overdue: "bg-red-50 text-red-700 dark:bg-red-500/10 dark:text-red-400",
};

export default function InvoicesPage() {
  const { data: invoices, isLoading, isError } = useInvoices();
  const [createOpen, setCreateOpen] = useState(false);

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold">Invoices</h1>
          <p className="text-muted-foreground text-sm mt-1">Track payments from your clients</p>
        </div>
        <Button size="sm" className="gap-2" onClick={() => setCreateOpen(true)}>
          <Plus className="size-4" /> New Invoice
        </Button>
      </div>

      {isLoading && (
        <Card className="animate-pulse border-0 shadow-sm">
          <CardContent className="py-8">
            <div className="h-4 bg-muted rounded w-1/2 mx-auto" />
          </CardContent>
        </Card>
      )}

      {isError && (
        <Card className="border-0 shadow-sm">
          <CardContent className="py-12 text-center text-muted-foreground">
            <FileText className="size-10 mx-auto mb-3 opacity-30" />
            <p>Could not load invoices.</p>
          </CardContent>
        </Card>
      )}

      {invoices && invoices.length === 0 && (
        <Card className="border-0 shadow-sm">
          <CardContent className="py-12 text-center text-muted-foreground">
            <FileText className="size-10 mx-auto mb-3 opacity-30" />
            <p className="font-medium">No invoices yet</p>
            <p className="text-sm mt-1">Create your first invoice to start getting paid.</p>
          </CardContent>
        </Card>
      )}

      {invoices && invoices.length > 0 && (
        <Card className="border-0 shadow-sm">
          <CardHeader><CardTitle className="text-base">All Invoices</CardTitle></CardHeader>
          <CardContent className="p-0">
            <table className="w-full text-sm">
              <thead className="border-b bg-muted/40">
                <tr>
                  {["Title", "Client", "Status", "Amount", "Due Date"].map(h => (
                    <th key={h} className="text-left px-4 py-3 font-medium text-muted-foreground">{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y">
                {invoices.map(inv => (
                  <tr
                    key={inv.id}
                    className="hover:bg-muted/20 transition-colors cursor-pointer"
                  >
                    <td className="px-4 py-3">
                      <Link href={`/dashboard/invoices/${inv.id}`} className="hover:underline font-medium">
                        {inv.title}
                      </Link>
                    </td>
                    <td className="px-4 py-3">{inv.clientName}</td>
                    <td className="px-4 py-3">
                      <Badge className={`text-xs ${statusColor[inv.status]}`} variant="outline">
                        {inv.status}
                      </Badge>
                    </td>
                    <td className="px-4 py-3 font-medium tabular-nums">
                      ${inv.total.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                    </td>
                    <td className="px-4 py-3 text-muted-foreground">
                      {inv.dueDate ? new Date(inv.dueDate).toLocaleDateString() : "—"}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </CardContent>
        </Card>
      )}

      <CreateInvoiceDialog open={createOpen} onOpenChange={setCreateOpen} />
    </div>
  );
}
