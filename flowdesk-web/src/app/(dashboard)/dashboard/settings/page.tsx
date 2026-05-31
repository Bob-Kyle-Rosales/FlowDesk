"use client";

import { useAuth } from "@/contexts/AuthContext";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { Badge } from "@/components/ui/badge";

export default function SettingsPage() {
  const { user } = useAuth();

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
        <CardContent className="space-y-4">
          <div className="space-y-2">
            <Label>Agency name</Label>
            <Input defaultValue={user?.organisationName} disabled />
          </div>
          <p className="text-xs text-muted-foreground">
            Full agency settings (branding, custom domain, team members) are coming in Phase 2.
          </p>
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
