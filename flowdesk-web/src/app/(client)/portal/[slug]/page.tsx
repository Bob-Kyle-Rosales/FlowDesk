// Header and nav are in layout.tsx — this page renders only the projects list content.
// Projects list is implemented in Task 6.
export default function PortalPage() {
  return (
    <div className="bg-white rounded-xl border shadow-sm p-6 text-center">
      <p className="text-muted-foreground text-sm">Your projects will appear here.</p>
    </div>
  );
}
