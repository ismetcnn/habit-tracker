"use client";

import { useState, useEffect, useRef, useCallback } from "react";
import { Search, ChevronLeft, ChevronRight, X, Trophy, Flame, Download } from "lucide-react";
import { ProtectedLayout } from "@/components/ProtectedLayout";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import {
  Table, TableBody, TableCell, TableHead, TableHeader, TableRow,
} from "@/components/ui/table";
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription,
} from "@/components/ui/dialog";
import { Card, CardContent } from "@/components/ui/card";
import { cn } from "@/lib/utils";
import { adminApi } from "@/lib/api";

interface ApiUser {
  id: string;
  username: string;
  email: string;
  createdAt: string;
  isAdmin: boolean;
  isBanned: boolean;
  habitCount: number;
  totalCompletions: number;
  lastActiveAt: string | null;
}

interface ApiUserDetail extends ApiUser {
  habits: {
    id: string;
    title: string;
    categoryName: string | null;
    goalType: string;
    goalValue: number;
    goalUnit: string | null;
  }[];
  achievements: {
    id: string;
    title: string;
    type: string;
    earnedAt: string;
  }[];
}

function formatDate(dateStr: string | null) {
  if (!dateStr) return "—";
  return new Date(dateStr).toLocaleDateString("tr-TR", {
    day: "2-digit",
    month: "short",
    year: "numeric",
  });
}

function getInitials(username: string) {
  return username.slice(0, 2).toUpperCase();
}

function UserStatCard({
  title, value, color, loading,
}: {
  title: string; value: number; color: "blue" | "green" | "red"; loading: boolean;
}) {
  return (
    <Card>
      <CardContent className="p-4">
        <p className="text-xs text-muted-foreground mb-1.5">{title}</p>
        {loading ? (
          <div className="h-7 w-16 bg-muted rounded animate-pulse" />
        ) : (
          <p className={cn("text-2xl font-bold", {
            "text-[#2563EB]": color === "blue",
            "text-green-600 dark:text-green-400": color === "green",
            "text-red-600 dark:text-red-400": color === "red",
          })}>
            {value.toLocaleString()}
          </p>
        )}
      </CardContent>
    </Card>
  );
}

export default function UsersPage() {
  const [users, setUsers] = useState<ApiUser[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [loading, setLoading] = useState(true);
  const [selectedUser, setSelectedUser] = useState<ApiUser | null>(null);
  const [detail, setDetail] = useState<ApiUserDetail | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);
  const [banLoading, setBanLoading] = useState(false);
  const [userStats, setUserStats] = useState({ total: 0, active: 0, banned: 0, todayNew: 0 });
  const [statsLoading, setStatsLoading] = useState(true);
  const [confirmBan, setConfirmBan] = useState<ApiUser | null>(null);
  const [exporting, setExporting] = useState(false);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const fetchUsers = useCallback(async (q: string, p: number) => {
    setLoading(true);
    try {
      const res = await adminApi.getUsers(q || undefined, p);
      const data = res.data.data;
      setUsers(data.items);
      setTotalCount(data.totalCount);
      setTotalPages(data.totalPages);
    } finally {
      setLoading(false);
    }
  }, []);

  const fetchStats = useCallback(async () => {
    setStatsLoading(true);
    try {
      const today = new Date().toDateString();
      const res1 = await adminApi.getUsers(undefined, 1);
      const { items: page1, totalCount: tc, totalPages: tp } = res1.data.data as {
        items: ApiUser[]; totalCount: number; totalPages: number;
      };

      let allItems: ApiUser[] = [...page1];
      const extraPages = Math.min(tp, 10) - 1;
      if (extraPages > 0) {
        const results = await Promise.all(
          Array.from({ length: extraPages }, (_, i) => adminApi.getUsers(undefined, i + 2))
        );
        results.forEach(r => { allItems = allItems.concat(r.data.data.items); });
      }

      const banned = allItems.filter(u => u.isBanned).length;
      const todayNew = allItems.filter(u => new Date(u.createdAt).toDateString() === today).length;
      setUserStats({ total: tc, active: tc - banned, banned, todayNew });
    } catch {
      // fail silently
    } finally {
      setStatsLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchStats();
  }, [fetchStats]);

  useEffect(() => {
    fetchUsers(search, page);
  }, [page]); // eslint-disable-line react-hooks/exhaustive-deps

  function handleSearchChange(value: string) {
    setSearch(value);
    if (debounceRef.current) clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(() => {
      setPage(1);
      fetchUsers(value, 1);
    }, 500);
  }

  async function handleRowClick(user: ApiUser) {
    setSelectedUser(user);
    setDetail(null);
    setDetailLoading(true);
    try {
      const res = await adminApi.getUserDetail(user.id);
      setDetail(res.data.data);
    } finally {
      setDetailLoading(false);
    }
  }

  async function handleBan(userId: string, isBanned: boolean) {
    setBanLoading(true);
    try {
      if (isBanned) {
        await adminApi.unbanUser(userId);
      } else {
        await adminApi.banUser(userId);
      }
      setSelectedUser(null);
      setConfirmBan(null);
      await fetchUsers(search, page);
      fetchStats();
    } finally {
      setBanLoading(false);
    }
  }

  async function handleExportCSV() {
    setExporting(true);
    try {
      let allExportUsers: ApiUser[] = [];
      let pg = 1;
      let tp = 1;
      do {
        const res = await adminApi.getUsers(search || undefined, pg);
        const { items, totalPages: t } = res.data.data;
        allExportUsers = allExportUsers.concat(items);
        tp = t;
        pg++;
      } while (pg <= tp);

      const headers = ["Username", "Email", "Kayıt Tarihi", "Alışkanlık Sayısı", "Toplam Tamamlama"];
      const rows = allExportUsers.map(u => [
        u.username,
        u.email,
        formatDate(u.createdAt),
        String(u.habitCount),
        String(u.totalCompletions),
      ]);

      const csv = [headers, ...rows]
        .map(row => row.map(cell => `"${String(cell).replace(/"/g, '""')}"`).join(","))
        .join("\n");

      const blob = new Blob(["﻿" + csv], { type: "text/csv;charset=utf-8;" });
      const url = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = `kullanicilar-${new Date().toISOString().slice(0, 10)}.csv`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);
    } finally {
      setExporting(false);
    }
  }

  return (
    <ProtectedLayout>
      {/* Header */}
      <div className="mb-6 flex items-start justify-between">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Kullanıcılar</h1>
          <p className="text-muted-foreground text-sm mt-1">
            {totalCount} kayıtlı kullanıcı
          </p>
        </div>
        <Button variant="outline" size="sm" onClick={handleExportCSV} disabled={exporting || loading}>
          <Download className="h-4 w-4 mr-2" />
          {exporting ? "İndiriliyor..." : "CSV İndir"}
        </Button>
      </div>

      {/* Stats cards */}
      <div className="grid grid-cols-2 xl:grid-cols-4 gap-4 mb-6">
        <UserStatCard title="Toplam Kullanıcı" value={userStats.total} color="blue" loading={statsLoading} />
        <UserStatCard title="Aktif Kullanıcı" value={userStats.active} color="green" loading={statsLoading} />
        <UserStatCard title="Banlı Kullanıcı" value={userStats.banned} color="red" loading={statsLoading} />
        <UserStatCard title="Bugün Kayıt" value={userStats.todayNew} color="blue" loading={statsLoading} />
      </div>

      {/* Search */}
      <div className="relative mb-6 max-w-sm">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
        <Input
          placeholder="Kullanıcı ara..."
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

      {/* Table */}
      <Card>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow className="hover:bg-transparent even:bg-transparent">
                <TableHead>Kullanıcı</TableHead>
                <TableHead>Email</TableHead>
                <TableHead>Kayıt Tarihi</TableHead>
                <TableHead>Toplam Alışkanlık</TableHead>
                <TableHead>Son Aktiflik</TableHead>
                <TableHead></TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {loading ? (
                <TableRow>
                  <TableCell colSpan={6} className="text-center py-12">
                    <div className="w-6 h-6 border-2 border-[#2563EB] border-t-transparent rounded-full animate-spin mx-auto" />
                  </TableCell>
                </TableRow>
              ) : users.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={6} className="text-center py-12 text-muted-foreground">
                    Kullanıcı bulunamadı
                  </TableCell>
                </TableRow>
              ) : (
                users.map((user) => (
                  <TableRow
                    key={user.id}
                    onClick={() => handleRowClick(user)}
                    className={user.isBanned ? "!bg-red-50 dark:!bg-red-950/20 hover:!bg-red-100 dark:hover:!bg-red-950/30" : ""}
                  >
                    <TableCell>
                      <div className="flex items-center gap-3">
                        <Avatar className="h-8 w-8">
                          <AvatarFallback className="text-xs">{getInitials(user.username)}</AvatarFallback>
                        </Avatar>
                        <span className="font-medium text-sm">{user.username}</span>
                        {user.isBanned && (
                          <Badge variant="destructive" className="text-xs">Banlı</Badge>
                        )}
                      </div>
                    </TableCell>
                    <TableCell className="text-muted-foreground text-sm">{user.email}</TableCell>
                    <TableCell className="text-sm">{formatDate(user.createdAt)}</TableCell>
                    <TableCell>
                      <Badge variant={user.habitCount >= 10 ? "default" : "secondary"}>
                        {user.habitCount} alışkanlık
                      </Badge>
                    </TableCell>
                    <TableCell className="text-sm text-muted-foreground">
                      {formatDate(user.lastActiveAt)}
                    </TableCell>
                    <TableCell className="text-right">
                      {user.isBanned ? (
                        <Button
                          variant="outline"
                          size="sm"
                          className="h-7 text-xs text-green-600 border-green-200 hover:bg-green-50 hover:text-green-700 dark:text-green-400 dark:border-green-900 dark:hover:bg-green-950/30"
                          disabled={banLoading}
                          onClick={(e) => { e.stopPropagation(); handleBan(user.id, true); }}
                        >
                          Banı Kaldır
                        </Button>
                      ) : (
                        <Button
                          variant="outline"
                          size="sm"
                          className="h-7 text-xs text-red-600 border-red-200 hover:bg-red-50 hover:text-red-700 dark:text-red-400 dark:border-red-900 dark:hover:bg-red-950/30"
                          disabled={banLoading}
                          onClick={(e) => { e.stopPropagation(); setConfirmBan(user); }}
                        >
                          Banla
                        </Button>
                      )}
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between px-6 py-4 border-t border-border">
              <p className="text-sm text-muted-foreground">
                {totalCount} kullanıcıdan {(page - 1) * 10 + 1}–
                {Math.min(page * 10, totalCount)} gösteriliyor
              </p>
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
            </div>
          )}
        </CardContent>
      </Card>

      {/* Confirm Ban Dialog */}
      <Dialog open={!!confirmBan} onOpenChange={(open) => !open && setConfirmBan(null)}>
        {confirmBan && (
          <DialogContent className="max-w-sm">
            <DialogHeader>
              <DialogTitle>Kullanıcıyı Banla</DialogTitle>
              <DialogDescription>
                Bu kullanıcıyı banlamak istediğinize emin misiniz?
              </DialogDescription>
            </DialogHeader>
            <div className="flex justify-end gap-2 mt-2">
              <Button variant="outline" size="sm" onClick={() => setConfirmBan(null)}>
                İptal
              </Button>
              <Button
                variant="destructive"
                size="sm"
                disabled={banLoading}
                onClick={() => handleBan(confirmBan.id, false)}
              >
                {banLoading ? "İşleniyor..." : "Banla"}
              </Button>
            </div>
          </DialogContent>
        )}
      </Dialog>

      {/* User Detail Modal */}
      <Dialog open={!!selectedUser} onOpenChange={(open) => !open && setSelectedUser(null)}>
        {selectedUser && (
          <DialogContent className="max-w-xl max-h-[90vh] overflow-y-auto">
            <DialogHeader>
              <div className="flex items-center gap-4 mb-2">
                <Avatar className="h-12 w-12">
                  <AvatarFallback className="text-sm">{getInitials(selectedUser.username)}</AvatarFallback>
                </Avatar>
                <div>
                  <DialogTitle>{selectedUser.username}</DialogTitle>
                  <p className="text-sm text-muted-foreground">{selectedUser.email}</p>
                </div>
              </div>
            </DialogHeader>

            <div className="grid grid-cols-3 gap-3 mb-6">
              {[
                { label: "Kayıt Tarihi", value: formatDate(selectedUser.createdAt) },
                { label: "Alışkanlıklar", value: selectedUser.habitCount },
                { label: "Son Aktiflik", value: formatDate(selectedUser.lastActiveAt) },
              ].map(({ label, value }) => (
                <div key={label} className="bg-muted/50 rounded-lg p-3 text-center">
                  <p className="text-xs text-muted-foreground mb-1">{label}</p>
                  <p className="text-sm font-semibold">{value}</p>
                </div>
              ))}
            </div>

            {detailLoading ? (
              <div className="flex justify-center py-8">
                <div className="w-6 h-6 border-2 border-[#2563EB] border-t-transparent rounded-full animate-spin" />
              </div>
            ) : detail ? (
              <>
                {/* Habits */}
                <div className="mb-5">
                  <div className="flex items-center gap-2 mb-3">
                    <Flame className="h-4 w-4 text-[#EF4444]" />
                    <h3 className="text-sm font-semibold">Alışkanlıklar</h3>
                  </div>
                  {detail.habits.length === 0 ? (
                    <p className="text-sm text-muted-foreground">Alışkanlık bulunamadı</p>
                  ) : (
                    <div className="space-y-2">
                      {detail.habits.map((h) => (
                        <div key={h.id} className="flex items-center justify-between bg-muted/40 rounded-lg px-4 py-2.5">
                          <div>
                            <p className="text-sm font-medium">{h.title}</p>
                            <p className="text-xs text-muted-foreground">{h.categoryName ?? "—"}</p>
                          </div>
                          <div className="text-right">
                            <p className="text-xs text-[#EF4444] font-medium">{h.goalType}</p>
                            <p className="text-xs text-muted-foreground">
                              {h.goalType === "check" ? "—" : `${h.goalValue} ${h.goalUnit ?? ""}`.trim()}
                            </p>
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </div>

                {/* Achievements */}
                <div className="mb-5">
                  <div className="flex items-center gap-2 mb-3">
                    <Trophy className="h-4 w-4 text-[#2563EB]" />
                    <h3 className="text-sm font-semibold">Başarımlar</h3>
                  </div>
                  {detail.achievements.length === 0 ? (
                    <p className="text-sm text-muted-foreground">Henüz başarım kazanılmamış</p>
                  ) : (
                    <div className="flex flex-wrap gap-2">
                      {detail.achievements.map((a) => (
                        <Badge key={a.id} variant="outline" className="text-xs py-1 px-3">
                          {a.title}
                        </Badge>
                      ))}
                    </div>
                  )}
                </div>

                {/* Ban / Unban */}
                <div className="flex justify-end pt-2 border-t border-border">
                  <Button
                    variant={detail.isBanned ? "outline" : "destructive"}
                    size="sm"
                    disabled={banLoading}
                    onClick={() => handleBan(detail.id, detail.isBanned)}
                  >
                    {banLoading
                      ? "İşleniyor..."
                      : detail.isBanned
                      ? "Banı Kaldır"
                      : "Kullanıcıyı Banla"}
                  </Button>
                </div>
              </>
            ) : null}
          </DialogContent>
        )}
      </Dialog>
    </ProtectedLayout>
  );
}
