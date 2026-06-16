"use client";

import { motion } from "framer-motion";

interface Props {
  children: React.ReactNode;
  className?: string;
}

export function SpringCard({ children, className }: Props) {
  return (
    <motion.div
      className={className}
      whileHover={{ y: -6, scale: 1.025 }}
      whileTap={{ scale: 0.97 }}
      transition={{ type: "spring", stiffness: 500, damping: 28 }}
    >
      {children}
    </motion.div>
  );
}
