import axios from "axios";

const api = axios.create({
  baseURL: "http://localhost:8080",
  headers: { "Content-Type": "application/json" },
});

api.interceptors.request.use((config) => {
  if (typeof window !== "undefined") {
    const token = localStorage.getItem("token");
    if (token) config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

api.interceptors.response.use(
  (res) => res,
  (err) => {
    if (err.response?.status === 401 && typeof window !== "undefined") {
      localStorage.removeItem("token");
      localStorage.removeItem("refreshToken");
      window.location.href = "/login";
    }
    return Promise.reject(err);
  }
);

export const adminApi = {
  getDashboardStats: () => api.get("/api/admin/dashboard"),
  getWeeklyCompletions: () => api.get("/api/admin/completions/weekly"),
  getTopHabits: () => api.get("/api/admin/habits/top"),
  getRecentAchievements: () => api.get("/api/admin/achievements/recent"),
  getUsers: (search?: string, page?: number) =>
    api.get("/api/admin/users", { params: { search, page } }),
  getUserDetail: (id: string) => api.get(`/api/admin/users/${id}`),
  getHabits: (search?: string, category?: string, page?: number) =>
    api.get("/api/admin/habits", { params: { search, category, page } }),
  banUser: (id: string) => api.post(`/api/admin/users/${id}/ban`),
  unbanUser: (id: string) => api.post(`/api/admin/users/${id}/unban`),
};

export const authApi = {
  login: (email: string, password: string) =>
    api.post("/api/auth/login", { email, password }),
};

export default api;
