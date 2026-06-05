"use client";

import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { toast } from "sonner";
import { useAuth } from "@/contexts/AuthContext";
import { useOrganisation, useUpdateOrganisation, useStripeConnectUrl } from "@/lib/queries";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { Badge } from "@/components/ui/badge";

const orgSchema = z.object({
  name: z.string().min(1, "Agency name is required"),
  primaryColor: z.string(),
});
type OrgFormData = z.infer<typeof orgSchema>;

export default function SettingsPage() {
  const { user } = useAuth();
  const { data: org } = useOrganisation();
  const updateOrg = useUpdateOrganisation();
  const getConnectUrl = useStripeConnectUrl();
  const [connectingStripe, setConnectingStripe] = useState(false);

  const { register, handleSubmit, reset, formState: { errors, isDirty, isSubmitting } } = useForm<OrgFormData>({
    resolver: zodResolver(orgSchema),
    defaultValues: { name: "", primaryColor: "#7c3aed" },
  });

  useEffect(() => {
    if (org) {
      reset({ name: org.name, primaryColor: org.primaryColor ?? "#7c3aed" });
    }
  }, [org, reset]);

  // Handle Stripe Connect redirect result
  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    if (params.get("stripe") === "connected") {
      toast.success("Stripe account connected successfully!");
      window.history.replaceState({}, "", window.location.pathname);
    } else if (params.get("stripe_error") === "denied") {
      toast.error("Stripe Connect was cancelled.");
      window.history.replaceState({}, "", window.location.pathname);
    } else if (params.get("stripe_error")) {
      toast.error("Stripe Connect failed. Please try again.");
      window.history.replaceState({}, "", window.location.pathname);
    }
  }, []);

  async function onSubmit(data: OrgFormData) {
    try {
      const updated = await updateOrg.mutateAsync(data);
      reset({ name: updated.name, primaryColor: updated.primaryColor ?? "#7c3aed" });
      toast.success("Agency settings saved");
    } catch {
      toast.error("Failed to save settings");
    }
  }

  async function handleConnectStripe() {
    setConnectingStripe(true);
    try {
      const { url } = await getConnectUrl.mutateAsync();
      window.location.href = url;
    } catch {
      toast.error("Failed to start Stripe Connect. Check STRIPE_CLIENT_ID is set.");
      setConnectingStripe(false);
    }
  }

  const isOwner = user?.role === "AgencyOwner";
  const stripeConnected = !!org?.stripeAccountId;

  return (
    <div className="p-6 max-w-2xl space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Settings</h1>
        <p className="text-muted-foreground text-sm mt-1">Manage your account and agency settings</p>
      </div>

      <Card className="border-0 shadow-sm">
        <CardHeader>
          <CardTitle className="text-base">Profile</CardTitle>
          <CardDescription>Your personal account details</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-2">
            <Label>Name</Label>
            <Input value={user?.name ?? ""} disabled readOnly />
          </div>
          <div className="space-y-2">
            <Label>Email</Label>
            <Input value={user?.email ?? ""} disabled readOnly />
          </div>
          <div className="space-y-2">
            <Label>Role</Label>
            <div><Badge variant="secondary">{user?.role}</Badge></div>
          </div>
        </CardContent>
      </Card>

      <Separator />

      <Card className="border-0 shadow-sm">
        <CardHeader>
          <CardTitle className="text-base">Agency</CardTitle>
          <CardDescription>Your organisation settings</CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="org-name">Agency name</Label>
              <Input id="org-name" {...register("name")} />
              {errors.name && <p className="text-xs text-destructive">{errors.name.message}</p>}
            </div>
            <div className="space-y-2">
              <Label htmlFor="primaryColor">Brand color</Label>
              <div className="flex items-center gap-3">
                <input
                  id="primaryColor"
                  type="color"
                  className="h-9 w-16 cursor-pointer rounded-md border border-input p-1 bg-background"
                  {...register("primaryColor")}
                />
                <span className="text-sm text-muted-foreground">Used across your client portal</span>
              </div>
            </div>
            <div className="flex gap-2 pt-2">
              <Button type="submit" size="sm" disabled={!isDirty || isSubmitting}>
                {isSubmitting ? "Saving…" : "Save changes"}
              </Button>
              <Button
                type="button"
                size="sm"
                variant="ghost"
                disabled={!isDirty}
                onClick={() => reset({ name: org?.name ?? "", primaryColor: org?.primaryColor ?? "#7c3aed" })}
              >
                Cancel
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>

      <Separator />

      <Card className="border-0 shadow-sm">
        <CardHeader>
          <CardTitle className="text-base">Payments</CardTitle>
          <CardDescription>
            Connect your Stripe account so clients can pay invoices directly to you.
          </CardDescription>
        </CardHeader>
        <CardContent>
          {stripeConnected ? (
            <div className="flex items-center gap-3">
              <Badge className="bg-green-100 text-green-700 border-green-200">Connected</Badge>
              <span className="text-sm text-muted-foreground font-mono">
                {org?.stripeAccountId}
              </span>
            </div>
          ) : (
            <div className="space-y-3">
              <p className="text-sm text-muted-foreground">
                No Stripe account connected. Clients cannot pay invoices until you connect.
              </p>
              {isOwner ? (
                <Button
                  size="sm"
                  onClick={handleConnectStripe}
                  disabled={connectingStripe}
                >
                  {connectingStripe ? "Redirecting…" : "Connect with Stripe"}
                </Button>
              ) : (
                <p className="text-xs text-muted-foreground">
                  Only the agency owner can connect Stripe.
                </p>
              )}
            </div>
          )}
        </CardContent>
      </Card>

      <Card className="border border-destructive/30 shadow-sm">
        <CardHeader>
          <CardTitle className="text-base text-destructive">Danger Zone</CardTitle>
          <CardDescription>Irreversible account actions</CardDescription>
        </CardHeader>
        <CardContent>
          <Button variant="destructive" disabled>Delete account</Button>
        </CardContent>
      </Card>
    </div>
  );
}
