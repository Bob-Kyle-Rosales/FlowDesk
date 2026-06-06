"use client";

import { use, useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { useInvoice, useDeleteInvoice, useSendInvoice, usePayInvoice } from "@/lib/queries";
import { UpdateInvoiceDialog } from "@/components/invoices/UpdateInvoiceDialog";
import { PayInvoiceDialog } from "@/components/invoices/PayInvoiceDialog";
import { useAuth } from "@/contexts/AuthContext";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { toast } from "sonner";
import { ArrowLeft, Pencil, Trash2, Send, CreditCard } from "lucide-react";

const statusColor: Record<string, string> = {
  Draft: "bg-gray-100 text-gray-600",
  Sent: "bg-blue-100 text-blue-700",
  Paid: "bg-green-100 text-green-700",
  Overdue: "bg-red-100 text-red-700",
};

export default function InvoiceDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const router = useRouter();
  const { data: invoice, isLoading, isError } = useInvoice(id);
  const deleteInvoice = useDeleteInvoice();
  const sendInvoice = useSendInvoice();
  const payInvoice = usePayInvoice();
  const { user } = useAuth();
  const [editOpen, setEditOpen] = useState(false);
  const [payOpen, setPayOpen] = useState(false);
  const [clientSecret, setClientSecret] = useState<string | null>(null);

  async function handleDelete() {
    if (!confirm("Delete this invoice? This cannot be undone.")) return;
    try {
      await deleteInvoice.mutateAsync(id);
      toast.success("Invoice deleted");
      router.push("/dashboard/invoices");
    } catch {
      toast.error("Failed to delete invoice");
    }
  }

  if (isLoading) {
    return (
      <div className="p-6">
        <div className="h-8 bg-muted rounded w-48 animate-pulse" />
      </div>
    );
  }

  if (isError || !invoice) {
    return (
      <div className="p-6">
        <p className="text-destructive text-sm">Invoice not found.</p>
        <Link href="/dashboard/invoices" className="text-sm underline mt-2 inline-block">
          Back to invoices
        </Link>
      </div>
    );
  }

  const isDraft = invoice.status === "Draft";
  const isSent = invoice.status === "Sent";
  const isClient = user?.role === "Client";

  return (
    <div className="p-6 space-y-6 max-w-3xl">
      <div className="flex items-center gap-3">
        <Link
          href="/dashboard/invoices"
          className="flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground transition-colors"
        >
          <ArrowLeft className="size-4" /> Invoices
        </Link>
      </div>

      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-2xl font-semibold">{invoice.title}</h1>
          <p className="text-muted-foreground text-sm mt-1">
            Created {new Date(invoice.createdAt).toLocaleDateString()}
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Badge className={`text-xs ${statusColor[invoice.status]}`} variant="outline">
            {invoice.status}
          </Badge>
          {isDraft && (
            <>
              <Button
                variant="outline"
                size="sm"
                className="gap-1.5"
                onClick={async () => {
                  try {
                    await sendInvoice.mutateAsync(invoice.id);
                    toast.success("Invoice sent to client");
                  } catch {
                    toast.error("Failed to send invoice");
                  }
                }}
                disabled={sendInvoice.isPending}
              >
                <Send className="size-3.5" />
                {sendInvoice.isPending ? "Sending…" : "Send to Client"}
              </Button>
              <Button variant="outline" size="sm" className="gap-1.5" onClick={() => setEditOpen(true)}>
                <Pencil className="size-3.5" /> Edit
              </Button>
              <Button
                variant="outline"
                size="sm"
                className="gap-1.5 text-destructive hover:text-destructive"
                onClick={handleDelete}
                disabled={deleteInvoice.isPending}
              >
                <Trash2 className="size-3.5" /> {deleteInvoice.isPending ? "Deleting…" : "Delete"}
              </Button>
            </>
          )}
          {isSent && isClient && (
            <Button
              size="sm"
              className="gap-1.5"
              onClick={async () => {
                try {
                  const { clientSecret: cs } = await payInvoice.mutateAsync(invoice.id);
                  setClientSecret(cs);
                  setPayOpen(true);
                } catch (err: unknown) {
                  const detail =
                    (err as { response?: { data?: { detail?: string; title?: string } } })
                      ?.response?.data?.detail ??
                    (err as { response?: { data?: { title?: string } } })
                      ?.response?.data?.title ??
                    "Failed to initialise payment";
                  toast.error(detail);
                }
              }}
              disabled={payInvoice.isPending}
            >
              <CreditCard className="size-3.5" />
              {payInvoice.isPending ? "Loading…" : "Pay Now"}
            </Button>
          )}
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4 text-sm">
        <div>
          <p className="text-muted-foreground">Client</p>
          <p className="font-medium mt-0.5">{invoice.clientName}</p>
        </div>
        {invoice.projectName && (
          <div>
            <p className="text-muted-foreground">Project</p>
            <p className="font-medium mt-0.5">{invoice.projectName}</p>
          </div>
        )}
        <div>
          <p className="text-muted-foreground">Due date</p>
          <p className="font-medium mt-0.5">
            {invoice.dueDate ? new Date(invoice.dueDate).toLocaleDateString() : "—"}
          </p>
        </div>
        {invoice.paidAt && (
          <div>
            <p className="text-muted-foreground">Paid on</p>
            <p className="font-medium mt-0.5">{new Date(invoice.paidAt).toLocaleDateString()}</p>
          </div>
        )}
      </div>

      <Card className="border-0 shadow-sm">
        <CardHeader><CardTitle className="text-base">Line Items</CardTitle></CardHeader>
        <CardContent className="p-0">
          <table className="w-full text-sm">
            <thead className="border-b bg-muted/40">
              <tr>
                {["Description", "Qty", "Unit Price", "Total"].map(h => (
                  <th
                    key={h}
                    className={`px-4 py-3 font-medium text-muted-foreground ${
                      h === "Description" ? "text-left" : "text-right"
                    }`}
                  >
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y">
              {invoice.items.map(item => (
                <tr key={item.id}>
                  <td className="px-4 py-3">{item.description}</td>
                  <td className="px-4 py-3 text-right tabular-nums">{item.quantity}</td>
                  <td className="px-4 py-3 text-right tabular-nums">
                    ${item.unitPrice.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                  </td>
                  <td className="px-4 py-3 text-right tabular-nums">
                    ${item.lineTotal.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                  </td>
                </tr>
              ))}
            </tbody>
            <tfoot className="border-t bg-muted/20">
              <tr>
                <td colSpan={3} className="px-4 py-3 text-right font-medium">Total</td>
                <td className="px-4 py-3 text-right font-semibold tabular-nums">
                  ${invoice.total.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                </td>
              </tr>
            </tfoot>
          </table>
        </CardContent>
      </Card>

      {isDraft && (
        <UpdateInvoiceDialog
          invoice={invoice}
          open={editOpen}
          onOpenChange={setEditOpen}
        />
      )}
      {clientSecret && (
        <PayInvoiceDialog
          invoiceId={id}
          clientSecret={clientSecret}
          total={invoice.total}
          open={payOpen}
          onOpenChange={open => {
            setPayOpen(open);
            if (!open) setClientSecret(null);
          }}
        />
      )}
    </div>
  );
}
