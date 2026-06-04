"use client";

import { useEffect } from "react";
import { useFieldArray, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { toast } from "sonner";
import { Plus, Trash2 } from "lucide-react";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useUpdateInvoice, useClients, useProjects } from "@/lib/queries";
import type { Invoice } from "@/types";

const itemSchema = z.object({
  description: z.string().min(1, "Required"),
  quantity: z.number({ error: "Must be > 0" }).positive("Must be > 0"),
  unitPrice: z.number({ error: "Must be ≥ 0" }).min(0, "Must be ≥ 0"),
});

const schema = z.object({
  title: z.string().min(1, "Title is required"),
  clientId: z.string().min(1, "Client is required"),
  projectId: z.string().optional(),
  dueDate: z.string().optional(),
  items: z.array(itemSchema).min(1, "At least one line item is required"),
});

type FormData = z.infer<typeof schema>;

interface UpdateInvoiceDialogProps {
  invoice: Invoice;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function UpdateInvoiceDialog({ invoice, open, onOpenChange }: UpdateInvoiceDialogProps) {
  const { data: clients = [], isLoading: clientsLoading } = useClients();
  const { data: projects = [] } = useProjects();
  const updateInvoice = useUpdateInvoice(invoice.id);

  const { register, handleSubmit, reset, control, watch, formState: { errors, isSubmitting } } =
    useForm<FormData>({
      resolver: zodResolver(schema),
      defaultValues: {
        title: invoice.title,
        clientId: invoice.clientId,
        projectId: invoice.projectId ?? "",
        dueDate: invoice.dueDate ? invoice.dueDate.split("T")[0] : "",
        items: invoice.items.map(i => ({
          description: i.description,
          quantity: i.quantity,
          unitPrice: i.unitPrice,
        })),
      },
    });

  useEffect(() => {
    if (open) {
      reset({
        title: invoice.title,
        clientId: invoice.clientId,
        projectId: invoice.projectId ?? "",
        dueDate: invoice.dueDate ? invoice.dueDate.split("T")[0] : "",
        items: invoice.items.map(i => ({
          description: i.description,
          quantity: i.quantity,
          unitPrice: i.unitPrice,
        })),
      });
    }
  }, [open, invoice, reset]);

  const { fields, append, remove } = useFieldArray({ control, name: "items" });

  const watchedItems = watch("items");
  const total = watchedItems.reduce(
    (sum, item) => sum + (isNaN(item.quantity) ? 0 : item.quantity) * (isNaN(item.unitPrice) ? 0 : item.unitPrice),
    0
  );

  async function onSubmit(data: FormData) {
    try {
      await updateInvoice.mutateAsync({
        title: data.title,
        clientId: data.clientId,
        projectId: data.projectId || null,
        dueDate: data.dueDate || null,
        items: data.items.map(i => ({
          description: i.description,
          quantity: i.quantity,
          unitPrice: i.unitPrice,
        })),
      });
      toast.success("Invoice updated");
      onOpenChange(false);
    } catch {
      toast.error("Failed to update invoice");
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Edit Invoice</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="upd-title">Title</Label>
            <Input id="upd-title" {...register("title")} />
            {errors.title && <p className="text-xs text-destructive">{errors.title.message}</p>}
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-2">
              <Label htmlFor="upd-client">Client</Label>
              <select
                id="upd-client"
                className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring disabled:opacity-50"
                disabled={clientsLoading}
                {...register("clientId")}
              >
                <option value="">Select client…</option>
                {clients.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select>
              {errors.clientId && <p className="text-xs text-destructive">{errors.clientId.message}</p>}
            </div>

            <div className="space-y-2">
              <Label htmlFor="upd-project">Project <span className="text-muted-foreground">(optional)</span></Label>
              <select
                id="upd-project"
                className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
                {...register("projectId")}
              >
                <option value="">None</option>
                {projects.map(p => <option key={p.id} value={p.id}>{p.name}</option>)}
              </select>
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="upd-due">Due date <span className="text-muted-foreground">(optional)</span></Label>
            <Input id="upd-due" type="date" {...register("dueDate")} />
          </div>

          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <Label>Line items</Label>
              <Button
                type="button"
                variant="ghost"
                size="sm"
                className="h-7 gap-1 text-xs"
                onClick={() => append({ description: "", quantity: 1, unitPrice: 0 })}
              >
                <Plus className="size-3" /> Add item
              </Button>
            </div>

            {errors.items?.root && (
              <p className="text-xs text-destructive">{errors.items.root.message}</p>
            )}

            <div className="space-y-2">
              {fields.map((field, index) => (
                <div key={field.id} className="grid grid-cols-[1fr_80px_90px_32px] gap-2 items-start">
                  <div>
                    <Input placeholder="Description" {...register(`items.${index}.description`)} />
                    {errors.items?.[index]?.description && (
                      <p className="text-xs text-destructive mt-0.5">
                        {errors.items[index]?.description?.message}
                      </p>
                    )}
                  </div>
                  <div>
                    <Input
                      type="number"
                      step="0.01"
                      placeholder="Qty"
                      {...register(`items.${index}.quantity`, { valueAsNumber: true })}
                    />
                    {errors.items?.[index]?.quantity && (
                      <p className="text-xs text-destructive mt-0.5">
                        {errors.items[index]?.quantity?.message}
                      </p>
                    )}
                  </div>
                  <div>
                    <Input
                      type="number"
                      step="0.01"
                      placeholder="Unit price"
                      {...register(`items.${index}.unitPrice`, { valueAsNumber: true })}
                    />
                    {errors.items?.[index]?.unitPrice && (
                      <p className="text-xs text-destructive mt-0.5">
                        {errors.items[index]?.unitPrice?.message}
                      </p>
                    )}
                  </div>
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    className="px-1 mt-0.5 text-muted-foreground hover:text-destructive"
                    onClick={() => fields.length > 1 && remove(index)}
                    disabled={fields.length === 1}
                    aria-label="Remove item"
                  >
                    <Trash2 className="size-3.5" />
                  </Button>
                </div>
              ))}
            </div>

            <div className="flex justify-end pt-1 text-sm font-medium">
              Total: ${total.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
            </div>
          </div>

          <DialogFooter>
            <Button type="button" variant="ghost" onClick={() => onOpenChange(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? "Saving…" : "Save changes"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
