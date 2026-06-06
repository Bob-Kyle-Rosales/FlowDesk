"use client";

import { useEffect, useRef, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { toast } from "sonner";
import { useAuth } from "@/contexts/AuthContext";
import {
  useOrganisation,
  useUpdateOrganisation,
  useStripeConnectUrl,
  useLogoUploadUrl,
  useUpdateLogo,
} from "@/lib/queries";
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
  const getLogoUploadUrl = useLogoUploadUrl();
  const updateLogo = useUpdateLogo();
  const [connectingStripe, setConnectingStripe] = useState(false);
  const [uploadProgress, setUploadProgress] = useState(0);
  const [isUploading, setIsUploading] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const { register, handleSubmit, reset, formState: { errors, isDirty, isSubmitting } } = useForm<OrgFormData>({
    resolver: zodResolver(orgSchema),
    defaultValues: { name: "", primaryColor: "#7c3aed" },
  });

  useEffect(() => {
    if (org) {
      reset({ name: org.name, primaryColor: org.primaryColor ?? "#7c3aed" });
    }
  }, [org, reset]);

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

  async function handleLogoChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file) return;

    setIsUploading(true);
    setUploadProgress(0);

    try {
      const { uploadUrl, fileUrl } = await getLogoUploadUrl.mutateAsync({
        fileName: file.name,
        contentType: file.type || "image/png",
      });

      await new Promise<void>((resolve, reject) => {
        const xhr = new XMLHttpRequest();
        xhr.upload.onprogress = ev => {
          if (ev.lengthComputable) setUploadProgress(Math.round((ev.loaded / ev.total) * 100));
        };
        xhr.onload = () => (xhr.status === 200 ? resolve() : reject(new Error("Upload failed")));
        xhr.onerror = () => reject(new Error("Upload failed"));
        xhr.open("PUT", uploadUrl);
        xhr.setRequestHeader("Content-Type", file.type || "image/png");
        xhr.send(file);
      });

      await updateLogo.mutateAsync(fileUrl);
      toast.success("Logo updated");
    } catch {
      toast.error("Logo upload failed");
    } finally {
      setIsUploading(false);
      setUploadProgress(0);
      if (fileInputRef.current) fileInputRef.current.value = "";
    }
  }

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
  const brandColor = org?.primaryColor ?? "#7c3aed";
  const initial = (org?.name ?? user?.name ?? "?")[0].toUpperCase();

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
        <CardContent className="space-y-4">
          {/* Logo upload */}
          <div className="space-y-2">
            <Label>Agency logo</Label>
            <div className="flex items-center gap-4">
              <button
                type="button"
                onClick={() => fileInputRef.current?.click()}
                disabled={isUploading || !isOwner}
                className="relative group size-20 rounded-full overflow-hidden focus:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:cursor-not-allowed"
                aria-label="Upload logo"
              >
                {org?.logoUrl ? (
                  <img
                    src={org.logoUrl}
                    alt="Agency logo"
                    className="size-full object-cover"
                  />
                ) : (
                  <div
                    className="size-full flex items-center justify-center text-white font-bold text-2xl"
                    style={{ backgroundColor: brandColor }}
                  >
                    {initial}
                  </div>
                )}
                {isOwner && (
                  <div className="absolute inset-0 bg-black/40 flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity">
                    <span className="text-white text-xs font-medium">
                      {isUploading ? `${uploadProgress}%` : "Change"}
                    </span>
                  </div>
                )}
              </button>
              <input
                ref={fileInputRef}
                type="file"
                accept="image/*"
                className="hidden"
                onChange={handleLogoChange}
              />
              <p className="text-xs text-muted-foreground">
                Click to upload. PNG, JPG, SVG accepted.
              </p>
            </div>
            {isUploading && (
              <div className="h-1 bg-muted rounded-full overflow-hidden w-20">
                <div
                  className="h-full bg-violet-500 transition-all duration-200"
                  style={{ width: `${uploadProgress}%` }}
                />
              </div>
            )}
          </div>

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
