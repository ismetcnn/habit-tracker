"use client";

import { useEffect, useState } from "react";
import { Users, Flame, CheckCircle, Zap } from "lucide-react";
import { ProtectedLayout } from "@/components/ProtectedLayout";
import { StatsCard } from "@/components/StatsCard";
import { CompletionLineChart } from "@/components/charts/CompletionLineChart";
import { HabitsBarChart } from "@/components/charts/HabitsBarChart";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { adminApi } from "@/lib/api";

interface DashboardStats { totalUsers: number; totalHabits: number; totalCompletionsToday: number; activeStreaksCount: number; }
interface DailyCompletionStat { date: string; count: number; }
interface TopHabitStat { title: string; completionCount: number; }
interface RecentAchievementStat { achievementTitle: string; username: string; habitTitle: string | null; earnedAt: string; }
interface AchievementRow { id: number; user: string; avatar: string; habit: string; achievement: string; type: string; earnedAt: string; }

const TR_DAYS = ["Paz", "Pzt", "Sal", "Çar", "Per", "Cum", "Cmt"];

function dateToDayLabel(dateStr: string) {
  const [y, m, d] = dateStr.split("-").map(Number);
  return TR_DAYS[new Date(y, m - 1, d).getDay()];
}

function timeAgo(dateStr: string) {
  const diff = Date.now() - new Date(dateStr).getTime();
  const minutes = Math.floor(diff / 60000);
  if (minutes < 60) return `${minutes} dk önce`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours} sa önce`;
  return `${Math.floor(hours / 24)} gün önce`;
}

const achievementColors: Record<string, string> = {
  streak_3: "secondary",
  streak_7: "default",
  streak_30: "default",
  streak_100: "default",
  total_10: "secondary",
  total_50: "default",
  total_100: "default",
  perfect_week: "success",
};

export default function DashboardPage() {
  const [stats, setStats] = useState({ totalUsers: 0, totalHabits: 0, completionsToday: 0, activeStreaks: 0 });
  const [lineChartData, setLineChartData] = useState<{ day: string; completions: number }[]>([]);
  const [barChartData, setBarChartData] = useState<{ name: string; completions: number }[]>([]);
  const [recentAchievements, setRecentAchievements] = useState<AchievementRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    async function fetchAll() {
      try {
        const [statsRes, weeklyRes, topRes, achieveRes] = await Promise.all([
          adminApi.getDashboardStats(),
          adminApi.getWeeklyCompletions(),
          adminApi.getTopHabits(),
          adminApi.getRecentAchievements(),
        ]);

        const s = statsRes.data.data as DashboardStats;
        setStats({
          totalUsers: s.totalUsers,
          totalHabits: s.totalHabits,
          completionsToday: s.totalCompletionsToday,
          activeStreaks: s.activeStreaksCount,
        });

        setLineChartData(
          (weeklyRes.data.data as DailyCompletionStat[]).map((item) => ({
            day: dateToDayLabel(item.date),
            completions: item.count,
          }))
        );

        setBarChartData(
          (topRes.data.data as TopHabitStat[]).map((item) => ({
            name: item.title,
            completions: item.completionCount,
          }))
        );

        setRecentAchievements(
          (achieveRes.data.data as RecentAchievementStat[]).map((item, i) => ({
            id: i,
            user: item.username,
            avatar: item.username.slice(0, 2).toUpperCase(),
            habit: item.habitTitle ?? "—",
            achievement: item.achievementTitle,
            type: item.achievementTitle.toLowerCase().includes("seri")
              ? "streak_7"
              : item.achievementTitle.toLowerCase().includes("hafta")
              ? "perfect_week"
              : "total_10",
            earnedAt: timeAgo(item.earnedAt),
          }))
        );
      } catch {
        setError("Veriler yüklenirken hata oluştu.");
      } finally {
        setLoading(false);
      }
    }
    fetchAll();
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

  return (
    <ProtectedLayout>
      {/* Header */}
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-foreground">Dashboard</h1>
        <p className="text-muted-foreground text-sm mt-1">
          Genel bakış ve performans özeti
        </p>
      </div>

      {/* Stats Row */}
      <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-5 mb-8">
        <StatsCard
          title="Toplam Kullanıcı"
          value={stats.totalUsers.toLocaleString()}
          accent="blue"
          icon={Users}
          trend={{ value: 12, label: "bu ay" }}
        />
        <StatsCard
          title="Toplam Alışkanlık"
          value={stats.totalHabits.toLocaleString()}
          accent="red"
          icon={Flame}
          trend={{ value: 8, label: "bu ay" }}
        />
        <StatsCard
          title="Bugün Tamamlanan"
          value={stats.completionsToday.toLocaleString()}
          accent="blue"
          icon={CheckCircle}
          trend={{ value: 5, label: "dünden" }}
        />
        <StatsCard
          title="Aktif Seriler"
          value={stats.activeStreaks.toLocaleString()}
          accent="red"
          icon={Zap}
          trend={{ value: -3, label: "dünden" }}
        />
      </div>

      {/* Charts */}
      <div className="grid grid-cols-1 xl:grid-cols-2 gap-5 mb-8">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-base">Son 7 Gün Tamamlanma</CardTitle>
            <p className="text-xs text-muted-foreground">Günlük toplam tamamlama sayısı</p>
          </CardHeader>
          <CardContent>
            <CompletionLineChart data={lineChartData} />
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-base">En Popüler Alışkanlıklar</CardTitle>
            <p className="text-xs text-muted-foreground">Toplam tamamlama sayısına göre top 5</p>
          </CardHeader>
          <CardContent>
            <HabitsBarChart data={barChartData} />
          </CardContent>
        </Card>
      </div>

      {/* Recent Achievements */}
      <Card>
        <CardHeader className="pb-4">
          <div className="flex items-center justify-between">
            <div>
              <CardTitle className="text-base">Son Kazanılan Başarımlar</CardTitle>
              <p className="text-xs text-muted-foreground mt-1">Son 24 saat içinde kazanılan</p>
            </div>
            <Badge variant="secondary" className="text-xs">{recentAchievements.length} adet</Badge>
          </div>
        </CardHeader>
        <CardContent className="p-0">
          <div className="divide-y divide-border">
            {recentAchievements.map((item) => (
              <div
                key={item.id}
                className="flex items-center gap-4 px-6 py-3.5 hover:bg-muted/40 transition-colors"
              >
                <Avatar className="h-8 w-8 shrink-0">
                  <AvatarFallback className="text-xs">{item.avatar}</AvatarFallback>
                </Avatar>
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 flex-wrap">
                    <span className="text-sm font-medium text-foreground">{item.user}</span>
                    <span className="text-xs text-muted-foreground">·</span>
                    <span className="text-xs text-muted-foreground">{item.habit}</span>
                  </div>
                  <p className="text-sm text-foreground mt-0.5">{item.achievement}</p>
                </div>
                <div className="text-right shrink-0">
                  <Badge variant={(achievementColors[item.type] ?? "default") as "default" | "secondary" | "outline" | "destructive" | "success" | "warning"} className="text-xs">
                    {item.type.split("_")[0]}
                  </Badge>
                  <p className="text-xs text-muted-foreground mt-1">{item.earnedAt}</p>
                </div>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    </ProtectedLayout>
  );
}
