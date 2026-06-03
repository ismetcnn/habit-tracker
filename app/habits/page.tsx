"use client";

import { useState, useEffect, useRef } from "react";
import { Search, X, Flame, CheckCircle, Zap, ChevronLeft, ChevronRight } from "lucide-react";
import { ProtectedLayout } from "@/components/ProtectedLayout";
import { StatsCard } from "@/components/StatsCard";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent } from "@/components/ui/card";
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from "@/components/ui/table";
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from "@/components/ui/select";
import { adminApi } from "@/lib/api";

interface ApiHabit {
  id: string;
  title: string;
  ownerUsername: string;
  categoryName: string | null;
  goalType: string;
  totalCompletions: number;
  currentStreak: number;
}

const categories = ["Tümü", "Sağlık", "Spor", "Zihin", "Eğitim"];

const goalTypeLabels: Record<string, string> = {
  check: "Kontrol",
  count: "Sayı",
  duration: "Süre",
};

const goalTypeVariants: Record<string, "default" | "secondary" | "outline"> = {
  check: "secondary",
  count: "default",
  duration: "outline",
};

export default function HabitsPage() {
  const [habits, setHabits] = useState<ApiHabit[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [category, setCategory] = useState("Tümü");
  const [loading, setLoading] = useState(true);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  async function fetchHabits(q: string, cat: string, p: number) {
    setLoading(true);
    try {
      const res = await adminApi.getHabits(
        q || undefined,
        cat === "Tümü" ? undefined : cat,
        p
      );
      const data = res.data.data;
      setHabits(data.items);
      setTotalCount(data.totalCount);
      setTotalPages(data.totalPages);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    fetchHabits(search, category, page);
  }, [page, category]); // eslint-disable-line react-hooks/exhaustive-deps

  function handleSearchChange(value: string) {
    setSearch(value);
    if (debounceRef.current) clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(() => {
      setPage(1);
      fetchHabits(value, category, 1);
    }, 500);
  }

  function handleCategoryChange(value: string) {
    setCategory(value);
    setPage(1);
  }

  const topHabit = habits.length > 0
    ? habits.reduce((a, b) => (a.totalCompletions > b.totalCompletions ? a : b))
    : null;
  const bestStreak = habits.length > 0
    ? habits.reduce((a, b) => (a.currentStreak > b.currentStreak ? a : b))
    : null;

  return (
    <ProtectedLayout>
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-foreground">Alışkanlıklar</h1>
        <p className="text-muted-foreground text-sm mt-1">
          Tüm kullanıcı alışkanlıklarını görüntüle ve yönet
        </p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-5 mb-8">
        <StatsCard
          title="Toplam Aktif"
          value={totalCount.toLocaleString()}
          accent="blue"
          icon={CheckCircle}
        />
        <StatsCard
          title="En Çok Tamamlanan"
          value={topHabit?.title ?? "—"}
          subtitle={topHabit ? `${topHabit.totalCompletions.toLocaleString()} tamamlama` : undefined}
          accent="red"
          icon={Flame}
        />
        <StatsCard
          title="En Uzun Seri"
          value={bestStreak ? `${bestStreak.currentStreak} gün` : "—"}
          subtitle={bestStreak ? `${bestStreak.ownerUsername} · ${bestStreak.title}` : undefined}
          accent="red"
          icon={Zap}
        />
      </div>

      {/* Filters */}
      <div className="flex flex-col sm:flex-row gap-3 mb-6">
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Alışkanlık veya kullanıcı ara..."
            value={search}
            onChange={(e) => handleSearchChange(e.target.value)}
            className="pl-9"
          />
          {search && (
            <button
              onClick={() => handleSearchChange("")}
              className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
            >
              <X className="h-4 w-4" />
            </button>
          )}
        </div>
        <Select value={category} onValueChange={handleCategoryChange}>
          <SelectTrigger className="w-full sm:w-44">
            <SelectValue placeholder="Kategori" />
          </SelectTrigger>
          <SelectContent>
            {categories.map((c) => (
              <SelectItem key={c} value={c}>{c}</SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      {/* Table */}
      <Card>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow className="hover:bg-transparent even:bg-transparent">
                <TableHead>Alışkanlık</TableHead>
                <TableHead>Sahip</TableHead>
                <TableHead>Kategori</TableHead>
                <TableHead>Hedef Türü</TableHead>
                <TableHead>Toplam</TableHead>
                <TableHead>Güncel Seri</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {loading ? (
                <TableRow>
                  <TableCell colSpan={6} className="text-center py-12">
                    <div className="w-6 h-6 border-2 border-[#2563EB] border-t-transparent rounded-full animate-spin mx-auto" />
                  </TableCell>
                </TableRow>
              ) : habits.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={6} className="text-center py-12 text-muted-foreground">
                    Alışkanlık bulunamadı
                  </TableCell>
                </TableRow>
              ) : (
                habits.map((habit) => (
                  <TableRow key={habit.id}>
                    <TableCell className="font-medium">{habit.title}</TableCell>
                    <TableCell className="text-muted-foreground text-sm">{habit.ownerUsername}</TableCell>
                    <TableCell>
                      <Badge variant="outline" className="text-xs">
                        {habit.categoryName ?? "—"}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      <Badge variant={goalTypeVariants[habit.goalType] ?? "outline"} className="text-xs">
                        {goalTypeLabels[habit.goalType] ?? habit.goalType}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      <span className="font-semibold text-sm text-[#2563EB]">
                        {habit.totalCompletions.toLocaleString()}
                      </span>
                    </TableCell>
                    <TableCell>
                      {habit.currentStreak > 0 ? (
                        <span className="flex items-center gap-1 text-sm font-medium text-[#EF4444]">
                          🔥 {habit.currentStreak} gün
                        </span>
                      ) : (
                        <span className="text-sm text-muted-foreground">—</span>
                      )}
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>

          {/* Pagination + count */}
          <div className="flex items-center justify-between px-6 py-3 border-t border-border">
            <p className="text-xs text-muted-foreground">
              {totalCount} alışkanlık
            </p>
            {totalPages > 1 && (
              <div className="flex items-center gap-2">
                <Button
                  variant="outline"
                  size="icon"
                  className="h-8 w-8"
                  onClick={() => setPage((p) => p - 1)}
                  disabled={page === 1}
                >
                  <ChevronLeft className="h-4 w-4" />
                </Button>
                {Array.from({ length: totalPages }, (_, i) => i + 1).map((p) => (
                  <Button
                    key={p}
                    variant={p === page ? "default" : "outline"}
                    size="icon"
                    className="h-8 w-8 text-xs"
                    onClick={() => setPage(p)}
                  >
                    {p}
                  </Button>
                ))}
                <Button
                  variant="outline"
                  size="icon"
                  className="h-8 w-8"
                  onClick={() => setPage((p) => p + 1)}
                  disabled={page === totalPages}
                >
                  <ChevronRight className="h-4 w-4" />
                </Button>
              </div>
            )}
          </div>
        </CardContent>
      </Card>
    </ProtectedLayout>
  );
}
