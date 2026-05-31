export default function AuthLayout({ children }: { children: React.ReactNode }) {
  const dotGrid: React.CSSProperties = {
    backgroundImage:
      "radial-gradient(circle, rgba(167,139,250,0.2) 1px, transparent 1px)",
    backgroundSize: "20px 20px",
  };

  return (
    <div className="min-h-screen flex">
      {/* Brand panel */}
      <div
        className="hidden md:flex md:w-1/2 bg-gradient-to-b from-violet-950 via-violet-900 to-violet-800 flex-col justify-between p-10"
        style={dotGrid}
      >
        <div className="flex items-center gap-3">
          <div className="size-9 rounded-lg bg-violet-400/20 ring-1 ring-violet-400/30 shadow-lg shadow-violet-950 flex items-center justify-center">
            <span className="text-violet-300 font-bold text-lg leading-none">◆</span>
          </div>
          <span className="text-white font-semibold text-lg">FlowDesk</span>
        </div>

        <div className="space-y-6">
          <div className="space-y-3">
            <h1 className="text-4xl font-extrabold text-white leading-tight tracking-tight">
              Your clients,<br />beautifully managed.
            </h1>
            <p className="text-violet-300/80 text-base">
              Give every client a branded portal — all in one place.
            </p>
          </div>

          <ul className="space-y-3">
            {[
              "Track projects in real time",
              "Collect payments instantly",
              "Impress every client",
            ].map((item) => (
              <li key={item} className="flex items-center gap-3 text-sm text-violet-200/80">
                <span className="text-violet-400 text-xs">◆</span>
                {item}
              </li>
            ))}
          </ul>
        </div>

        <p className="text-violet-400/50 text-sm">© {new Date().getFullYear()} FlowDesk</p>
      </div>

      {/* Form panel */}
      <div className="flex-1 flex items-center justify-center px-6 py-12 bg-white">
        {children}
      </div>
    </div>
  );
}
