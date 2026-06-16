"use client";

import Link from "next/link";
import { ArrowRight, CheckCircle, LayoutDashboard, FileText, MessageSquare, Globe } from "lucide-react";
import { motion } from "framer-motion";
import { AnimatedSection } from "@/components/ui/AnimatedSection";
import { SpringCard } from "@/components/ui/SpringCard";

// ─── Shared styles ───────────────────────────────────────────────
const serif = { fontFamily: "var(--font-fraunces)" } as const;

// ─── Spring configs ──────────────────────────────────────────────
const spring = { type: "spring" as const, stiffness: 380, damping: 26, mass: 0.8 };
const springSnap = { type: "spring" as const, stiffness: 500, damping: 28 };

const heroContainer = {
  hidden: {},
  show: { transition: { staggerChildren: 0.1, delayChildren: 0.4 } },
};

const heroItem = {
  hidden: { opacity: 0, y: 20 },
  show:   { opacity: 1, y: 0, transition: spring },
};

// ─── Data ────────────────────────────────────────────────────────
const features = [
  {
    icon: LayoutDashboard,
    title: "Project tracking",
    body: "Milestones, deliverables, and approval workflows — all in one place.",
  },
  {
    icon: FileText,
    title: "Invoice & payments",
    body: "Send professional invoices and collect payments via Stripe.",
  },
  {
    icon: Globe,
    title: "Client portal",
    body: "A branded URL for every client. No login friction, full visibility.",
  },
  {
    icon: MessageSquare,
    title: "Real-time messaging",
    body: "Per-project chat with file attachments and read receipts.",
  },
];

const steps = [
  {
    n: "01",
    title: "Create your workspace",
    body: "Register your agency in under a minute. Set your brand name and colour.",
  },
  {
    n: "02",
    title: "Invite your clients",
    body: "Send a magic-link invite. Clients get a portal branded to your agency.",
  },
  {
    n: "03",
    title: "Deliver beautifully",
    body: "Track projects, share deliverables, collect payments — all in one place.",
  },
];

const techStack = [
  "ASP.NET Core 8", "Next.js 16", "PostgreSQL", "EF Core",
  "Tailwind CSS", "shadcn/ui", "JWT Auth", "Stripe", "Railway", "Vercel", "SignalR",
];

const pricing = [
  {
    tier: "Starter",
    price: "Free",
    sub: "forever",
    features: ["1 client", "1 active project", "Basic invoicing", "Client portal"],
    cta: "Get started",
    highlight: false,
  },
  {
    tier: "Pro",
    price: "$49",
    sub: "per month",
    features: ["Up to 10 clients", "Unlimited projects", "Stripe payments", "Custom branding", "Real-time chat"],
    cta: "Start free trial",
    highlight: true,
  },
  {
    tier: "Agency",
    price: "$99",
    sub: "per month",
    features: ["Unlimited clients", "Unlimited projects", "Custom domain", "AI status reports", "Priority support"],
    cta: "Contact us",
    highlight: false,
  },
];

// ─── Component ───────────────────────────────────────────────────
export default function LandingPage() {
  return (
    <div className="min-h-screen bg-[#FAF6F1] text-[#1A1207]">

      {/* ── Navbar ─────────────────────────────────────────────── */}
      <motion.header
        initial={{ y: -64, opacity: 0 }}
        animate={{ y: 0, opacity: 1 }}
        transition={{ ...spring, delay: 0.05 }}
        className="sticky top-0 z-50 bg-[#FAF6F1]/95 backdrop-blur-sm border-b border-[#E2D9D0]"
      >
        <div className="max-w-6xl mx-auto px-6 h-16 flex items-center justify-between">
          <div className="flex items-center gap-2.5">
            <motion.div
              initial={{ scale: 0, rotate: -15 }}
              animate={{ scale: 1, rotate: 0 }}
              transition={{ ...spring, delay: 0.2 }}
              className="size-8 rounded-md bg-[#E05A2B] flex items-center justify-center shadow-sm"
            >
              <span className="text-white font-bold text-sm leading-none" style={serif}>F</span>
            </motion.div>
            <span className="font-semibold text-lg text-[#1A1207]" style={serif}>FlowDesk</span>
          </div>
          <nav className="flex items-center gap-4">
            <a href="#contact" className="text-sm font-medium text-[#7A6559] hover:text-[#1A1207] transition-colors hidden sm:block">
              Contact
            </a>
            <Link href="/login" className="text-sm font-medium text-[#7A6559] hover:text-[#1A1207] transition-colors">
              Sign in
            </Link>
            <motion.div
              initial={{ opacity: 0, scale: 0.8 }}
              animate={{ opacity: 1, scale: 1 }}
              transition={{ ...spring, delay: 0.35 }}
              whileHover={{ scale: 1.06 }}
              whileTap={{ scale: 0.94 }}
            >
              <Link
                href="/register"
                className="text-sm font-semibold bg-[#E05A2B] text-white px-4 py-2 rounded-md hover:bg-[#C94E22] transition-colors"
              >
                Get started
              </Link>
            </motion.div>
          </nav>
        </div>
      </motion.header>

      {/* ── Hero ───────────────────────────────────────────────── */}
      <section className="max-w-6xl mx-auto px-6 pt-20 pb-24 grid lg:grid-cols-2 gap-16 items-center">
        <motion.div
          className="space-y-6"
          variants={heroContainer}
          initial="hidden"
          animate="show"
        >
          <motion.p variants={heroItem} className="text-xs font-semibold uppercase tracking-[0.15em] text-[#E05A2B]">
            Client portals for agencies
          </motion.p>
          <motion.h1 variants={heroItem} className="text-5xl lg:text-6xl font-bold text-[#1A1207] leading-tight" style={serif}>
            Your clients deserve a better experience.
          </motion.h1>
          <motion.p variants={heroItem} className="text-lg text-[#7A6559] max-w-lg leading-relaxed">
            FlowDesk gives every client a branded portal to track projects, approve deliverables, and pay invoices — without the endless email threads.
          </motion.p>
          <motion.div variants={heroItem} className="flex flex-wrap gap-3 pt-2">
            <motion.div whileHover={{ y: -3, scale: 1.04 }} whileTap={{ scale: 0.94 }} transition={springSnap}>
              <Link
                href="/register"
                className="inline-flex items-center gap-2 bg-[#E05A2B] text-white font-semibold px-6 py-3 rounded-md hover:bg-[#C94E22] transition-colors"
              >
                Get started free <ArrowRight className="size-4" />
              </Link>
            </motion.div>
            <motion.div whileHover={{ y: -2 }} whileTap={{ scale: 0.96 }} transition={springSnap}>
              <a
                href="#how-it-works"
                className="inline-flex items-center gap-2 border border-[#E2D9D0] text-[#1A1207] font-medium px-6 py-3 rounded-md hover:border-[#E05A2B] hover:text-[#E05A2B] transition-colors"
              >
                See how it works
              </a>
            </motion.div>
          </motion.div>
        </motion.div>

        {/* Dashboard mock */}
        <motion.div
          className="relative"
          initial={{ opacity: 0, y: 24, filter: "blur(8px)" }}
          animate={{ opacity: 1, y: 0, filter: "blur(0px)" }}
          transition={{ ...spring, delay: 0.5 }}
        >
          <div className="rounded-xl overflow-hidden shadow-2xl border border-[#E2D9D0]">
            {/* Mock browser chrome */}
            <div className="bg-[#F0EBE4] px-4 py-3 flex items-center gap-2 border-b border-[#E2D9D0]">
              <div className="flex gap-1.5">
                <div className="size-3 rounded-full bg-[#E05A2B]/40" />
                <div className="size-3 rounded-full bg-[#E2D9D0]" />
                <div className="size-3 rounded-full bg-[#E2D9D0]" />
              </div>
              <div className="flex-1 bg-white rounded text-xs text-[#7A6559] px-3 py-1 text-center">
                app.flowdesk.co/dashboard
              </div>
            </div>
            {/* Mock dashboard */}
            <div className="flex bg-[#FAF6F1]" style={{ height: 280 }}>
              {/* Sidebar */}
              <div className="w-14 bg-[#2C1A0E] flex flex-col items-center pt-4 gap-3">
                <div className="size-7 rounded-md bg-[#E05A2B] flex items-center justify-center">
                  <span className="text-white text-xs font-bold" style={serif}>F</span>
                </div>
                {[1,2,3,4].map((i) => (
                  <div key={i} className={`size-6 rounded ${i === 1 ? "bg-[#E05A2B]/30" : "bg-white/10"}`} />
                ))}
              </div>
              {/* Content */}
              <div className="flex-1 p-4 space-y-3">
                <div className="h-12 rounded-lg bg-gradient-to-r from-[#E05A2B] to-[#C94E22] opacity-90" />
                <div className="grid grid-cols-4 gap-2">
                  {["from-blue-400 to-blue-500","from-emerald-400 to-emerald-500","from-amber-400 to-amber-500","from-[#E05A2B] to-[#C94E22]"].map((g, i) => (
                    <div key={i} className={`h-16 rounded-lg bg-gradient-to-br ${g} opacity-80`} />
                  ))}
                </div>
                <div className="h-24 rounded-lg bg-white border border-[#E2D9D0]" />
              </div>
            </div>
          </div>
          {/* Decorative blur */}
          <div className="absolute -bottom-4 -right-4 size-32 bg-[#E05A2B]/10 rounded-full blur-2xl -z-10" />
        </motion.div>
      </section>

      {/* ── Problem ────────────────────────────────────────────── */}
      <section className="bg-[#2C1A0E] py-20">
        <div className="max-w-6xl mx-auto px-6">
          <h2 className="text-3xl lg:text-4xl font-bold text-[#F5EDE4] mb-12 max-w-2xl" style={serif}>
            Agencies manage great work. The handoff is where things break.
          </h2>
          <div className="grid md:grid-cols-3 gap-8">
            {[
              { title: "Feedback buried in email", body: "Revision requests get lost across threads, Slack, and voice notes. Nothing is trackable." },
              { title: "Chasing invoice payments", body: "PDF invoices, manual follow-ups, bank transfers. Getting paid takes longer than the project." },
              { title: "Clients have no visibility", body: "Clients don't know what's done, what's next, or where to find the files you sent last week." },
            ].map(({ title, body }) => (
              <div key={title} className="space-y-3">
                <div className="size-8 rounded-full border border-[#E05A2B]/40 flex items-center justify-center">
                  <div className="size-2 rounded-full bg-[#E05A2B]" />
                </div>
                <h3 className="font-semibold text-[#F5EDE4]" style={serif}>{title}</h3>
                <p className="text-sm text-[#C4A882]/70 leading-relaxed">{body}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* ── Features ───────────────────────────────────────────── */}
      <section className="py-20 bg-[#FAF6F1]">
        <div className="max-w-6xl mx-auto px-6">
          <p className="text-xs font-semibold uppercase tracking-[0.15em] text-[#E05A2B] mb-3">What FlowDesk does</p>
          <h2 className="text-3xl lg:text-4xl font-bold text-[#1A1207] mb-12" style={serif}>
            Everything your agency needs.<br />Nothing it doesn&apos;t.
          </h2>
          <div className="grid sm:grid-cols-2 lg:grid-cols-4 gap-6">
            {features.map(({ icon: Icon, title, body }) => (
              <div key={title} className="bg-white rounded-xl p-6 shadow-sm border border-[#E2D9D0] space-y-4">
                <div className="size-10 rounded-lg bg-[#E05A2B]/8 flex items-center justify-center">
                  <Icon className="size-5 text-[#E05A2B]" />
                </div>
                <h3 className="font-semibold text-[#1A1207]" style={serif}>{title}</h3>
                <p className="text-sm text-[#7A6559] leading-relaxed">{body}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* ── How It Works ───────────────────────────────────────── */}
      <section id="how-it-works" className="bg-[#2C1A0E] py-20">
        <div className="max-w-6xl mx-auto px-6">
          <p className="text-xs font-semibold uppercase tracking-[0.15em] text-[#E05A2B] mb-3">Three steps</p>
          <h2 className="text-3xl lg:text-4xl font-bold text-[#F5EDE4] mb-12" style={serif}>
            Up and running in minutes.
          </h2>
          <div className="grid md:grid-cols-3 gap-10">
            {steps.map(({ n, title, body }) => (
              <div key={n} className="space-y-4">
                <span className="text-5xl font-bold text-[#E05A2B]/30" style={serif}>{n}</span>
                <h3 className="text-lg font-semibold text-[#F5EDE4]" style={serif}>{title}</h3>
                <p className="text-sm text-[#C4A882]/70 leading-relaxed">{body}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* ── Dashboard Preview ──────────────────────────────────── */}
      <section className="py-20 bg-[#FAF6F1]">
        <div className="max-w-6xl mx-auto px-6">
          <h2 className="text-3xl lg:text-4xl font-bold text-[#1A1207] mb-3" style={serif}>
            Built for the work you already do.
          </h2>
          <p className="text-[#7A6559] mb-12 max-w-xl">A clean, focused interface that your clients will actually enjoy using.</p>
          <div className="grid md:grid-cols-3 gap-4">
            {/* Stat card */}
            <div className="bg-gradient-to-br from-blue-500 to-blue-600 rounded-xl p-5 shadow-md text-white">
              <p className="text-xs font-semibold uppercase tracking-widest text-white/70 mb-3">Active Projects</p>
              <p className="text-4xl font-bold" style={serif}>8</p>
              <p className="text-xs text-white/60 mt-1">Across 6 clients</p>
            </div>
            {/* Project card */}
            <div className="bg-white rounded-xl p-5 shadow-sm border-t-2 border-t-[#E05A2B] border border-[#E2D9D0]">
              <div className="flex items-start justify-between mb-3">
                <p className="font-semibold text-sm text-[#1A1207]" style={serif}>Brand Redesign</p>
                <span className="text-xs bg-emerald-50 text-emerald-700 border border-emerald-200 rounded-full px-2 py-0.5">Active</span>
              </div>
              <p className="text-xs text-[#7A6559] mb-3">Full brand identity for Acme Inc.</p>
              <div className="space-y-1.5">
                <div className="flex items-center justify-between text-xs text-[#7A6559]">
                  <span>Progress</span><span>4/6 milestones</span>
                </div>
                <div className="h-1.5 bg-[#E2D9D0] rounded-full overflow-hidden">
                  <div className="h-full w-2/3 bg-[#E05A2B] rounded-full" />
                </div>
              </div>
            </div>
            {/* Invoice card */}
            <div className="bg-white rounded-xl p-5 shadow-sm border border-[#E2D9D0]">
              <p className="font-semibold text-sm text-[#1A1207] mb-3" style={serif}>Invoice #0042</p>
              <div className="space-y-2 text-xs">
                {[["Client", "Acme Inc."], ["Amount", "$4,800"], ["Due", "Jun 15, 2026"]].map(([k, v]) => (
                  <div key={k} className="flex justify-between">
                    <span className="text-[#7A6559]">{k}</span>
                    <span className="font-medium text-[#1A1207]">{v}</span>
                  </div>
                ))}
              </div>
              <div className="mt-3 flex justify-between items-center">
                <span className="text-xs bg-amber-50 text-amber-700 border border-amber-200 rounded-full px-2 py-0.5">Sent</span>
                <button className="text-xs font-medium text-[#E05A2B] hover:underline">Pay now →</button>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* ── Tech Stack ─────────────────────────────────────────── */}
      <section className="bg-[#2C1A0E] py-20">
        <div className="max-w-6xl mx-auto px-6">
          <h2 className="text-3xl font-bold text-[#F5EDE4] mb-8" style={serif}>Built with.</h2>
          <div className="flex flex-wrap gap-3">
            {techStack.map((tech) => (
              <span
                key={tech}
                className="border border-white/20 text-[#C4A882]/70 rounded-full px-4 py-1.5 text-xs font-medium"
              >
                {tech}
              </span>
            ))}
          </div>
        </div>
      </section>

      {/* ── Pricing ────────────────────────────────────────────── */}
      <section className="py-20 bg-[#FAF6F1]">
        <div className="max-w-6xl mx-auto px-6">
          <p className="text-xs font-semibold uppercase tracking-[0.15em] text-[#E05A2B] mb-3">Pricing</p>
          <h2 className="text-3xl lg:text-4xl font-bold text-[#1A1207] mb-12" style={serif}>
            Simple, honest pricing.
          </h2>
          <div className="grid md:grid-cols-3 gap-6">
            {pricing.map(({ tier, price, sub, features: feats, cta, highlight }) => (
              <div
                key={tier}
                className={`bg-white rounded-xl p-7 shadow-sm flex flex-col gap-6 ${highlight ? "border-2 border-[#E05A2B] ring-4 ring-[#E05A2B]/8" : "border border-[#E2D9D0]"}`}
              >
                <div>
                  {highlight && (
                    <span className="text-xs font-semibold text-[#E05A2B] uppercase tracking-widest mb-2 block">Most popular</span>
                  )}
                  <h3 className="text-lg font-bold text-[#1A1207]" style={serif}>{tier}</h3>
                  <div className="flex items-baseline gap-1 mt-2">
                    <span className="text-4xl font-bold text-[#1A1207]" style={serif}>{price}</span>
                    <span className="text-sm text-[#7A6559]">/{sub}</span>
                  </div>
                </div>
                <ul className="space-y-2.5 flex-1">
                  {feats.map((f) => (
                    <li key={f} className="flex items-center gap-2.5 text-sm text-[#7A6559]">
                      <CheckCircle className="size-4 text-[#E05A2B] shrink-0" />
                      {f}
                    </li>
                  ))}
                </ul>
                      <a
                  href={tier === "Agency" ? "#contact" : "/register"}
                  className={`text-center text-sm font-semibold py-2.5 rounded-md transition-colors ${highlight ? "bg-[#E05A2B] text-white hover:bg-[#C94E22]" : "border border-[#E2D9D0] text-[#1A1207] hover:border-[#E05A2B] hover:text-[#E05A2B]"}`}
                >
                  {cta}
                </a>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* ── Contact ────────────────────────────────────────────── */}
      <section id="contact" className="bg-[#FAF6F1] py-20 border-t border-[#E2D9D0]">
        <div className="max-w-6xl mx-auto px-6 grid lg:grid-cols-2 gap-16 items-start">
          <div className="space-y-4">
            <p className="text-xs font-semibold uppercase tracking-[0.15em] text-[#E05A2B]">Get in touch</p>
            <h2 className="text-3xl lg:text-4xl font-bold text-[#1A1207]" style={serif}>
              Let&apos;s talk about your agency.
            </h2>
            <p className="text-[#7A6559] leading-relaxed max-w-md">
              Interested in the Agency plan or have questions about how FlowDesk fits your workflow? Send us a message and we&apos;ll get back to you within one business day.
            </p>
            <a
              href="mailto:rosalesbobkyle@gmail.com"
              className="inline-flex items-center gap-2 text-[#E05A2B] font-medium hover:underline text-sm"
            >
              rosalesbobkyle@gmail.com
            </a>
          </div>
          <form
            action="mailto:rosalesbobkyle@gmail.com"
            method="get"
            encType="text/plain"
            className="bg-white rounded-xl border border-[#E2D9D0] shadow-sm p-7 space-y-5"
          >
            <div className="grid sm:grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <label className="text-xs font-medium text-[#1A1207]">Name</label>
                <input
                  name="name"
                  type="text"
                  placeholder="Full name"
                  className="w-full h-10 rounded-md border border-[#E2D9D0] px-3 text-sm text-[#1A1207] placeholder:text-[#C4A882] focus:outline-none focus:ring-2 focus:ring-[#E05A2B]/20 focus:border-[#E05A2B]"
                />
              </div>
              <div className="space-y-1.5">
                <label className="text-xs font-medium text-[#1A1207]">Email</label>
                <input
                  name="email"
                  type="email"
                  placeholder="Work email"
                  className="w-full h-10 rounded-md border border-[#E2D9D0] px-3 text-sm text-[#1A1207] placeholder:text-[#C4A882] focus:outline-none focus:ring-2 focus:ring-[#E05A2B]/20 focus:border-[#E05A2B]"
                />
              </div>
            </div>
            <div className="space-y-1.5">
              <label className="text-xs font-medium text-[#1A1207]">Message</label>
              <textarea
                name="body"
                rows={4}
                placeholder="How can we help you?"
                className="w-full rounded-md border border-[#E2D9D0] px-3 py-2 text-sm text-[#1A1207] placeholder:text-[#C4A882] focus:outline-none focus:ring-2 focus:ring-[#E05A2B]/20 focus:border-[#E05A2B] resize-none"
              />
            </div>
            <button
              type="submit"
              className="w-full h-11 bg-[#E05A2B] hover:bg-[#C94E22] text-white text-sm font-semibold rounded-md transition-colors"
            >
              Send message
            </button>
          </form>
        </div>
      </section>

      {/* ── Footer ─────────────────────────────────────────────── */}
      <footer className="bg-[#2C1A0E] border-t border-white/10 py-10">
        <div className="max-w-6xl mx-auto px-6 flex flex-col sm:flex-row items-center justify-between gap-4">
          <div className="flex items-center gap-2.5">
            <div className="size-7 rounded-md bg-[#E05A2B] flex items-center justify-center">
              <span className="text-white font-bold text-xs leading-none" style={serif}>F</span>
            </div>
            <span className="text-[#F5EDE4] font-semibold text-sm" style={serif}>FlowDesk</span>
            <span className="text-[#A07855]/50 text-xs ml-2">© {new Date().getFullYear()}</span>
          </div>
          <nav className="flex items-center gap-6 text-sm">
            <Link href="/login" className="text-[#C4A882]/60 hover:text-[#F5EDE4] transition-colors">Login</Link>
            <Link href="/register" className="text-[#C4A882]/60 hover:text-[#F5EDE4] transition-colors">Register</Link>
            <a href="#contact" className="text-[#C4A882]/60 hover:text-[#F5EDE4] transition-colors">Contact</a>
          </nav>
        </div>
      </footer>

    </div>
  );
}
