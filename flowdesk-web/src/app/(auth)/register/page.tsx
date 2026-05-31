"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { toast } from "sonner";
import { useAuth } from "@/contexts/AuthContext";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

const schema = z.object({
  name: z.string().min(2, "Name must be at least 2 characters"),
  email: z.string().email("Invalid email"),
  password: z.string().min(8, "Password must be at least 8 characters"),
  organisationName: z.string().min(2, "Agency name must be at least 2 characters"),
});
type FormData = z.infer<typeof schema>;

export default function RegisterPage() {
  const { register: registerUser } = useAuth();
  const router = useRouter();
  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<FormData>({
    resolver: zodResolver(schema),
  });

  async function onSubmit(data: FormData) {
    try {
      await registerUser(data.name, data.email, data.password, data.organisationName);
      router.push("/dashboard");
    } catch {
      toast.error("Failed to create account. Email may already be in use.");
    }
  }

  return (
    <div className="w-full max-w-sm space-y-8">
      {/* Mobile logo */}
      <div className="flex items-center gap-2 md:hidden">
        <div className="size-8 rounded-md bg-[#E05A2B] flex items-center justify-center">
          <span className="text-white font-bold" style={{ fontFamily: "var(--font-fraunces)" }}>F</span>
        </div>
        <span className="font-semibold" style={{ fontFamily: "var(--font-fraunces)" }}>FlowDesk</span>
      </div>

      <div>
        <h2
          className="text-3xl font-bold text-foreground"
          style={{ fontFamily: "var(--font-fraunces)" }}
        >
          Get started
        </h2>
        <p className="text-sm text-muted-foreground mt-1">Create your free agency workspace</p>
      </div>

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <div className="grid grid-cols-2 gap-3">
          <div className="space-y-1.5">
            <Label htmlFor="name" className="text-sm font-medium">Your name</Label>
            <Input id="name" placeholder="Jane Smith" className="h-11 bg-white border-border focus-visible:ring-[#E05A2B]/30" {...register("name")} />
            {errors.name && <p className="text-xs text-destructive">{errors.name.message}</p>}
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="organisationName" className="text-sm font-medium">Agency name</Label>
            <Input id="organisationName" placeholder="Acme Studio" className="h-11 bg-white border-border focus-visible:ring-[#E05A2B]/30" {...register("organisationName")} />
            {errors.organisationName && <p className="text-xs text-destructive">{errors.organisationName.message}</p>}
          </div>
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="email" className="text-sm font-medium">Email</Label>
          <Input id="email" type="email" placeholder="you@agency.com" className="h-11 bg-white border-border focus-visible:ring-[#E05A2B]/30" {...register("email")} />
          {errors.email && <p className="text-xs text-destructive">{errors.email.message}</p>}
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="password" className="text-sm font-medium">Password</Label>
          <Input id="password" type="password" placeholder="At least 8 characters" className="h-11 bg-white border-border focus-visible:ring-[#E05A2B]/30" {...register("password")} />
          {errors.password && <p className="text-xs text-destructive">{errors.password.message}</p>}
        </div>

        <Button
          type="submit"
          className="w-full h-11 bg-[#E05A2B] hover:bg-[#C94E22] text-white font-semibold"
          disabled={isSubmitting}
        >
          {isSubmitting ? "Creating account…" : "Create account"}
        </Button>
      </form>

      <p className="text-sm text-muted-foreground text-center">
        Already have an account?{" "}
        <Link href="/login" className="text-[#E05A2B] font-medium hover:underline">
          Sign in
        </Link>
      </p>
    </div>
  );
}
