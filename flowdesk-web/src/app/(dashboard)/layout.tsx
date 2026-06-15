import { Sidebar } from "@/components/layout/Sidebar";

export default function DashboardLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex h-screen overflow-hidden">
      <Sidebar />
      {/* pt-14 offsets the fixed mobile top bar on screens < lg */}
      <main className="flex-1 overflow-y-auto bg-[#FAFAF8] dark:bg-[#0F0F0F] pt-14 lg:pt-0">
        {children}
      </main>
    </div>
  );
}
