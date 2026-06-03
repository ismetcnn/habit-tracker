"use client";

import { useState, useEffect } from "react";
import { Trophy, Star, Crown, Target, Zap } from "lucide-react";
import { ProtectedLayout } from "@/components/ProtectedLayout";
import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import { adminApi } from "@/lib/api";

interface Achievement {
  type: string;
  title: string;
  emoji: string;
  description: string;
  earnedCount: number;
  rarity: string;
}

interface RecentAchievementItem {
  achievementTitle: string;
  username: string;
  habitTitle: string | null;
  earnedAt: string;
}

const ACHIEVEMENT_META: Record<string, { type: string; emoji: string; description: string; rarity: string }> = {
  "100 Günlük Seri!": { type: "streak_100", emoji: "🏆", description: "Bir alışkanlığı 100 gün üst üste tamamladın", rarity: "legendary" },
  "100 Tamamlama!": { type: "total_100", emoji: "👑", description: "Bir alışkanlığı 100 kez tamamladın", rarity: "legendary" },
  "30 Günlük Seri!": { type: "streak_30", emoji: "💪", description: "Bir alışkanlığı 30 gün üst üste tamamladın", rarity: "epic" },
  "50 Tamamlama!": { type: "total_50", emoji: "🌟", description: "Bir alışkanlığı 50 kez tamamladın", rarity: "epic" },
  "Mükemmel Hafta!": { type: "perfect_week", emoji: "🎯", description: "Son 7 günde planlanmış tüm alışkanlıkları tamamladın", rarity: "rare" },
  "7 Günlük Seri!": { type: "streak_7", emoji: "🔥", description: "Bir alışkanlığı 7 gün üst üste tamamladın", rarity: "rare" },
  "10 Tamamlama!": { type: "total_10", emoji: "⭐", description: "Bir alışkanlığı 10 kez tamamladın", rarity: "common" },
  "3 Günlük Seri!": { type: "streak_3", emoji: "🔥", description: "Bir alışkanlığı 3 gün üst üste tamamladın", rarity: "common" },
};

const rarityConfig: Record<
  string,
  { label: string; bg: string; border: string; icon: string; badge: "default" | "secondary" | "destructive" | "outline" }
> = {
  legendary: {
    label: "Efsanevi",
    bg: "bg-gradient-to-br from-amber-50 to-yellow-50 dark:from-amber-950/30 dark:to-yellow-950/30",
    border: "border-amber-300 dark:border-amber-700",
    icon: "text-amber-500",
    badge: "default",
  },
  epic: {
    label: "Epik",
    bg: "bg-gradient-to-br from-purple-50 to-violet-50 dark:from-purple-950/30 dark:to-violet-950/30",
    border: "border-purple-300 dark:border-purple-700",
    icon: "text-purple-500",
    badge: "default",
  },
  rare: {
    label: "Nadir",
    bg: "bg-gradient-to-br from-blue-50 to-sky-50 dark:from-blue-950/30 dark:to-sky-950/30",
    border: "border-[#2563EB]/30 dark:border-[#2563EB]/50",
    icon: "text-[#2563EB]",
    badge: "secondary",
  },
  common: {
    label: "Yaygın",
    bg: "bg-gradient-to-br from-slate-50 to-gray-50 dark:from-slate-800/30 dark:to-gray-800/30",
    border: "border-border",
    icon: "text-slate-500",
    badge: "outline",
  },
};

const rarityIcons: Record<string, React.ElementType> = {
  legendary: Crown,
  epic: Star,
  rare: Zap,
  common: Target,
};

const rarityOrder = ["legendary", "epic", "rare", "common"];

function RaritySection({ rarity, items, maxEarned }: { rarity: string; items: Achievement[]; maxEarned: number }) {
  const config = rarityConfig[rarity];
  const Icon = rarityIcons[rarity];
  return (
    <div className="mb-8">
      <div className="flex items-center gap-2 mb-4">
        <Icon className={cn("h-4 w-4", config.icon)} />
        <h2 className="text-sm font-bold uppercase tracking-wider text-foreground">
          {config.label}
        </h2>
        <span className="text-xs text-muted-foreground">({items.length})</span>
      </div>
      <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4">
        {items.map((a) => (
          <AchievementCard key={a.type} achievement={a} maxEarned={maxEarned} />
        ))}
      </div>
    </div>
  );
}

function AchievementCard({ achievement: a, maxEarned }: { achievement: Achievement; maxEarned: number }) {
  const config = rarityConfig[a.rarity] ?? rarityConfig.common;
  const rarityPct = maxEarned > 0 ? Math.round((a.earnedCount / maxEarned) * 100) : 0;

  return (
    <div
      className={cn(
        "rounded-xl border p-5 transition-all hover:shadow-md hover:-translate-y-0.5",
        config.bg,
        config.border
      )}
    >
      <div className="flex items-start justify-between mb-3">
        <div className="text-3xl">{a.emoji}</div>
        <Badge variant={config.badge} className="text-xs shrink-0">
          {config.label}
        </Badge>
      </div>

      <h3 className="font-bold text-sm text-foreground mb-1">{a.title}</h3>
      <p className="text-xs text-muted-foreground mb-4 leading-relaxed">{a.description}</p>

      <div className="space-y-1.5">
        <div className="flex items-center justify-between">
          <span className="text-xs text-muted-foreground">Kazanan</span>
          <span className="text-xs font-bold text-foreground">
            {a.earnedCount.toLocaleString()} kullanıcı
          </span>
        </div>
        <div className="h-1.5 bg-black/10 dark:bg-white/10 rounded-full overflow-hidden">
          <div
            className={cn(
              "h-full rounded-full transition-all",
              a.rarity === "legendary"
                ? "bg-amber-400"
                : a.rarity === "epic"
                ? "bg-purple-500"
                : a.rarity === "rare"
                ? "bg-[#2563EB]"
                : "bg-slate-400"
            )}
            style={{ width: `${Math.max(rarityPct, 2)}%` }}
          />
        </div>
        <p className="text-xs text-muted-foreground">{rarityPct}% elde etti</p>
      </div>
    </div>
  );
}

export default function AchievementsPage() {
  const [achievements, setAchievements] = useState<Achievement[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    adminApi.getRecentAchievements()
      .then(res => {
        const items = res.data.data as RecentAchievementItem[];
        const countMap = new Map<string, number>();
        for (const item of items) {
          countMap.set(item.achievementTitle, (countMap.get(item.achievementTitle) ?? 0) + 1);
        }
        const mapped: Achievement[] = Array.from(countMap.entries()).map(([title, earnedCount]) => {
          const meta = ACHIEVEMENT_META[title] ?? { type: title, emoji: "🎖️", description: "", rarity: "common" };
          return { ...meta, title, earnedCount };
        });
        setAchievements(mapped);
      })
      .catch(() => setError("Başarımlar yüklenirken hata oluştu."))
      .finally(() => setLoading(false));
  }, []);

  if (loading) {
    return (
      <ProtectedLayout>
        <div className="flex items-center justify-center h-64">
          <div className="w-8 h-8 border-2 border-[#2563EB] border-t-transparent rounded-full animate-spin" />
        </div>
      </ProtectedLayout>
    );
  }

  if (error) {
    return (
      <ProtectedLayout>
        <div className="flex items-center justify-center h-64">
          <p className="text-sm text-muted-foreground">{error}</p>
        </div>
      </ProtectedLayout>
    );
  }

  const totalEarned = achievements.reduce((sum, a) => sum + a.earnedCount, 0);
  const maxEarned = achievements.length > 0 ? Math.max(...achievements.map(a => a.earnedCount)) : 1;

  return (
    <ProtectedLayout>
      <div className="mb-8 flex items-start justify-between">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Başarımlar</h1>
          <p className="text-muted-foreground text-sm mt-1">
            {achievements.length} başarım türü · {totalEarned.toLocaleString()} toplam kazanım
          </p>
        </div>
        <div className="flex items-center gap-2 text-sm text-muted-foreground bg-muted/60 px-3 py-2 rounded-lg">
          <Trophy className="h-4 w-4 text-amber-500" />
          Nadirliğe göre sıralı
        </div>
      </div>

      {achievements.length === 0 ? (
        <div className="text-center py-16 text-muted-foreground text-sm">
          Son 24 saatte başarım kazanılmadı
        </div>
      ) : (
        rarityOrder.map((rarity) => {
          const items = achievements.filter((a) => a.rarity === rarity);
          if (items.length === 0) return null;
          return <RaritySection key={rarity} rarity={rarity} items={items} maxEarned={maxEarned} />;
        })
      )}
    </ProtectedLayout>
  );
}
