import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";

export default async function ClientPortalPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;

  return (
    <div className="min-h-screen bg-muted/40 flex items-center justify-center p-4">
      <div className="w-full max-w-2xl space-y-6">
        <div className="text-center">
          <div className="size-12 rounded-xl bg-primary flex items-center justify-center text-primary-foreground font-bold text-xl mx-auto mb-4">
            {slug[0]?.toUpperCase()}
          </div>
          <h1 className="text-2xl font-semibold capitalize">{slug.replace(/-/g, " ")}</h1>
          <p className="text-muted-foreground text-sm mt-1">Client Portal</p>
        </div>

        <Card>
          <CardHeader>
            <CardTitle className="text-base">Your Projects</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-muted-foreground">
              Client portal project view is coming in Phase 2.
            </p>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
