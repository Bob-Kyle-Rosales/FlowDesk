import { notFound } from "next/navigation";
import Image from "next/image";
import type { PublicOrganisationResponse } from "@/types";

export default async function ClientPortalPage({
  params,
}: {
  params: Promise<{ slug: string }>;
}) {
  const { slug } = await params;

  const apiUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5269";
  const res = await fetch(`${apiUrl}/api/organisations/public/${slug}`, {
    cache: "no-store",
  });

  if (!res.ok) notFound();

  const org: PublicOrganisationResponse = await res.json();

  const brandColor = org.primaryColor ?? "#7c3aed";
  const initial = org.name[0]?.toUpperCase() ?? "?";

  return (
    <div className="min-h-screen bg-muted/40">
      {/* Brand header */}
      <header style={{ backgroundColor: brandColor }} className="px-6 py-4 flex items-center gap-3">
        {org.logoUrl ? (
          <Image
            src={org.logoUrl}
            alt={`${org.name} logo`}
            width={36}
            height={36}
            unoptimized
            className="size-9 rounded-lg object-cover bg-white/20"
          />
        ) : (
          <div className="size-9 rounded-lg bg-white/20 flex items-center justify-center font-bold text-white text-base">
            {initial}
          </div>
        )}
        <span className="text-white font-semibold text-base">{org.name}</span>
        <span className="ml-auto text-white/60 text-sm">Client Portal</span>
      </header>

      {/* Body placeholder — Phase 5C will add real content */}
      <main className="max-w-2xl mx-auto p-6 space-y-6">
        <div className="bg-white rounded-xl border shadow-sm p-6 text-center">
          <p className="text-muted-foreground text-sm">Your projects will appear here.</p>
        </div>
      </main>
    </div>
  );
}
