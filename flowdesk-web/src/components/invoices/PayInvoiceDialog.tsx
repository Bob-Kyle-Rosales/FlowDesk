"use client";

import { useState } from "react";
import { loadStripe } from "@stripe/stripe-js";
import {
  Elements,
  PaymentElement,
  useStripe,
  useElements,
} from "@stripe/react-stripe-js";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { toast } from "sonner";
import { useQueryClient } from "@tanstack/react-query";

const stripePromise = loadStripe(
  process.env.NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY ?? ""
);

interface PaymentFormProps {
  onSuccess: () => void;
  onClose: () => void;
}

function PaymentForm({ onSuccess, onClose }: PaymentFormProps) {
  const stripe = useStripe();
  const elements = useElements();
  const [paying, setPaying] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!stripe || !elements) return;

    setPaying(true);
    const { error } = await stripe.confirmPayment({
      elements,
      confirmParams: { return_url: window.location.href },
      redirect: "if_required",
    });

    if (error) {
      toast.error(error.message ?? "Payment failed. Please try again.");
      setPaying(false);
    } else {
      toast.success("Payment successful!");
      onSuccess();
      onClose();
    }
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <PaymentElement />
      <DialogFooter>
        <Button type="button" variant="ghost" onClick={onClose} disabled={paying}>
          Cancel
        </Button>
        <Button type="submit" disabled={paying || !stripe || !elements}>
          {paying ? "Processing…" : "Pay Now"}
        </Button>
      </DialogFooter>
    </form>
  );
}

interface PayInvoiceDialogProps {
  invoiceId: string;
  clientSecret: string;
  total: number;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function PayInvoiceDialog({
  invoiceId,
  clientSecret,
  total,
  open,
  onOpenChange,
}: PayInvoiceDialogProps) {
  const queryClient = useQueryClient();

  function handleSuccess() {
    queryClient.invalidateQueries({ queryKey: ["invoices"] });
    queryClient.invalidateQueries({ queryKey: ["invoices", invoiceId] });
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>
            Pay Invoice —{" "}
            ${total.toLocaleString(undefined, {
              minimumFractionDigits: 2,
              maximumFractionDigits: 2,
            })}
          </DialogTitle>
        </DialogHeader>
        {open && clientSecret && (
          <Elements stripe={stripePromise} options={{ clientSecret }}>
            <PaymentForm
              onSuccess={handleSuccess}
              onClose={() => onOpenChange(false)}
            />
          </Elements>
        )}
      </DialogContent>
    </Dialog>
  );
}
