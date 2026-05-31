export default function AuthLayout({ children }: { children: React.ReactNode }) {
  return (
    <div className="min-h-screen flex">
      {/* Brand panel — espresso */}
      <div className="hidden md:flex md:w-5/12 bg-[#2C1A0E] flex-col justify-between p-12">
        <div className="flex items-center gap-3">
          <div className="size-9 rounded-md bg-[#E05A2B] flex items-center justify-center shadow-sm">
            <span
              className="text-white font-bold text-lg leading-none"
              style={{ fontFamily: "var(--font-fraunces)" }}
            >F</span>
          </div>
          <span
            className="text-[#F5EDE4] font-semibold text-lg tracking-wide"
            style={{ fontFamily: "var(--font-fraunces)" }}
          >FlowDesk</span>
        </div>

        <div className="space-y-8">
          <h1
            className="text-5xl font-bold text-[#F5EDE4] leading-tight"
            style={{ fontFamily: "var(--font-fraunces)" }}
          >
            Your clients,<br />
            <span className="text-[#E05A2B]">beautifully</span><br />
            managed.
          </h1>

          <ul className="space-y-4">
            {[
              "Track projects in real time",
              "Collect payments instantly",
              "Impress every client",
            ].map((item) => (
              <li key={item} className="flex items-center gap-3 text-sm text-[#C4A882]/80">
                <span className="size-1.5 rounded-full bg-[#E05A2B] shrink-0" />
                {item}
              </li>
            ))}
          </ul>
        </div>

        <p className="text-[#A07855]/50 text-xs">© {new Date().getFullYear()} FlowDesk</p>
      </div>

      {/* Form panel — warm paper */}
      <div className="flex-1 flex items-center justify-center px-8 py-12 bg-[#FAF6F1]">
        {children}
      </div>
    </div>
  );
}
