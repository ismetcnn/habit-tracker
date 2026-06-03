import { type LucideIcon } from "lucide-react";
import { cn } from "@/lib/utils";

interface StatsCardProps {
  title: string;
  value: string | number;
  subtitle?: string;
  icon: LucideIcon;
  accent: "blue" | "red";
  trend?: { value: number; label: string };
}

export function StatsCard({ title, value, subtitle, icon: Icon, accent, trend }: StatsCardProps) {
  const isBlue = accent === "blue";

  return (
    <div className="rounded-xl border border-border bg-card p-6 shadow-sm hover:shadow-md transition-all animate-fade-in">
      <div className="flex items-start justify-between">
        <div className="flex-1 min-w-0">
          <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wider mb-1">
            {title}
          </p>
          <p className="text-3xl font-bold text-foreground leading-tight">{value}</p>
          {subtitle && (
            <p className="text-sm text-muted-foreground mt-1 truncate">{subtitle}</p>
          )}
          {trend && (
            <p
              className={cn(
                "text-xs font-medium mt-2",
                trend.value >= 0 ? "text-emerald-500" : "text-[#EF4444]"
              )}
            >
              {trend.value >= 0 ? "↑" : "↓"} {Math.abs(trend.value)}% {trend.label}
            </p>
          )}
        </div>
        <div
          className={cn(
            "w-12 h-12 rounded-xl flex items-center justify-center shrink-0 ml-4",
            isBlue
              ? "bg-blue-50 dark:bg-blue-950/40"
              : "bg-red-50 dark:bg-red-950/40"
          )}
        >
          <Icon
            className={cn(
              "h-6 w-6",
              isBlue ? "text-[#2563EB]" : "text-[#EF4444]"
            )}
          />
        </div>
      </div>
    </div>
  );
}
