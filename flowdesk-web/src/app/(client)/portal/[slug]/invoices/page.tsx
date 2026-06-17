"use client";

import { useEffect, useState } from "react";
import { useRouter, useParams } from "next/navigation";
import { useAuth } from "@/contexts/AuthContext";
import { useInvoices, usePayInvoice } from "@/lib/queries";
import { PayInvoiceDialog } from "@/components/invoices/PayInvoiceDialog";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import type { Invoice } from "@/types";
import { toast } from "sonner";

const statusColor: Record<string, string> = {
  Draft: "bg-gray-100 text-gray-600",
  Sent: "bg-blue-100 text-blue-700",
  Paid: "bg-green-100 text-green-700",
  Overdue: "bg-red-100 text-red-700",
};

function InvoiceRow({
  invoice,
  onPay,
}: {
  invoice: Invoice;
  onPay: (invoice: Invoice) => void;
}) {
  return (
    <div className="bg-white rounded-xl border shadow-sm p-4 flex items-center justify-between gap-4">
      <div className="flex-1 min-w-0">
        <p className="font-medium text-sm text-gray-900 truncate">{invoice.title}</p>
        <p className="text-xs text-gray-500 mt-0.5">
          {invoice.dueDate
            ? `Due ${new Date(invoice.dueDate).toLocaleDateString()}`
            : "No due date"}
        </p>
      </div>
      <div className="text-right shrink-0">
        <p className="font-semibold text-sm text-gray-900">
          ${invoice.total.toLocaleString(undefined, {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2,
          })}
        </p>
      </div>
      <Badge className={statusColor[invoice.status] ?? "bg-gray-100 text-gray-600"}>
        {invoice.status}
      </Badge>
      {invoice.status === "Sent" && (
        <Button size="sm" onClick={() => onPay(invoice)}>
          Pay
        </Button>
      )}
    </div>
  );
}

export default function PortalInvoicesPage() {
  const router = useRouter();
  const { slug } = useParams<{ slug: string }>();
  const { user, loading } = useAuth();
  const { data: invoices = [], isLoading } = useInvoices();
  const payInvoice = usePayInvoice();

  const [payOpen, setPayOpen] = useState(false);
  const [selectedInvoice, setSelectedInvoice] = useState<Invoice | null>(null);
  const [clientSecret, setClientSecret] = useState<string | null>(null);

  useEffect(() => {
    if (!loading && !user) router.push("/login");
    else if (!loading && user && user.organisationSlug !== slug) {
      router.push(`/portal/${user.organisationSlug}`);
    }
  }, [user, loading, router, slug]);

  async function handlePay(invoice: Invoice) {
    try {
      const result = await payInvoice.mutateAsync(invoice.id);
      setSelectedInvoice(invoice);
      setClientSecret(result.clientSecret);
      setPayOpen(true);
    } catch {
      toast.error("Failed to start payment");
    }
  }

  if (loading || isLoading) {
    return (
      <p className="text-sm text-gray-500 animate-pulse py-8 text-center">
        Loading invoices…
      </p>
    );
  }

  if (!invoices.length) {
    return (
      <div className="text-center py-16">
        <p className="text-gray-500 text-sm">No invoices yet.</p>
      </div>
    );
  }

  return (
    <div className="space-y-3">
      {invoices.map((invoice) => (
        <InvoiceRow key={invoice.id} invoice={invoice} onPay={handlePay} />
      ))}

      {clientSecret && selectedInvoice && (
        <PayInvoiceDialog
          invoiceId={selectedInvoice.id}
          clientSecret={clientSecret}
          total={selectedInvoice.total}
          open={payOpen}
          onOpenChange={(open) => {
            setPayOpen(open);
            if (!open) {
              setClientSecret(null);
              setSelectedInvoice(null);
            }
          }}
        />
      )}
    </div>
  );
}
