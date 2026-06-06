"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

interface PortalNavProps {
  slug: string;
  brandColor: string;
}

export function PortalNav({ slug, brandColor }: PortalNavProps) {
  const pathname = usePathname();

  const links = [
    { href: `/portal/${slug}`, label: "Projects" },
    { href: `/portal/${slug}/invoices`, label: "Invoices" },
  ];

  return (
    <nav className="bg-white border-b px-6 flex gap-6">
      {links.map(({ href, label }) => {
        const active = pathname === href;
        return (
          <Link
            key={href}
            href={href}
            className="py-3 text-sm font-medium border-b-2 -mb-px transition-colors"
            style={
              active
                ? { borderColor: brandColor, color: brandColor }
                : { borderColor: "transparent", color: "var(--muted-foreground)" }
            }
          >
            {label}
          </Link>
        );
      })}
    </nav>
  );
}
