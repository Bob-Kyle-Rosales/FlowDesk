"use client";

import { useEffect } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { toast } from "sonner";
import { useAuth } from "@/contexts/AuthContext";
import { useOrganisation, useUpdateOrganisation } from "@/lib/queries";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { Badge } from "@/components/ui/badge";

const orgSchema = z.object({
  name: z.string().min(1, "Agency name is required"),
  primaryColor: z.string().nullable(),
});
type OrgFormData = z.infer<typeof orgSchema>;

export default function SettingsPage() {
  const { user } = useAuth();
  const { data: org } = useOrganisation();
  const updateOrg = useUpdateOrganisation();

  const { register, handleSubmit, reset, formState: { errors, isDirty, isSubmitting } } = useForm<OrgFormData>({
    resolver: zodResolver(orgSchema),
    defaultValues: { name: "", primaryColor: "#7c3aed" },
  });

  useEffect(() => {
    if (org) {
      reset({ name: org.name, primaryColor: org.primaryColor ?? "#7c3aed" });
    }
  }, [org, reset]);

  async function onSubmit(data: OrgFormData) {
    try {
      const updated = await updateOrg.mutateAsync(data);
      reset({ name: updated.name, primaryColor: updated.primaryColor ?? "#7c3aed" });
      toast.success("Agency settings saved");
    } catch {
      toast.error("Failed to save settings");
    }
  }

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
            <Input defaultValue={user?.name} disabled />
          </div>
          <div className="space-y-2">
            <Label>Email</Label>
            <Input defaultValue={user?.email} disabled />
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
                onClick={() => reset()}
              >
                Cancel
              </Button>
            </div>
          </form>
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
