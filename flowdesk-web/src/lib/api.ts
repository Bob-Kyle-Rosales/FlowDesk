import axios from "axios";

const isServer = typeof window === "undefined";

const api = axios.create({
  // In the browser, use relative paths so requests go through the Next.js proxy
  // (which re-issues cookies on the Vercel domain so the middleware can read them).
  // On the server there is no proxy, so target Railway directly.
  baseURL: isServer
    ? (process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5269")
    : "",
  withCredentials: true,
  headers: { "Content-Type": "application/json" },
});

api.interceptors.response.use(
  (res) => res,
  async (error) => {
    const original = error.config;
    if (error.response?.status === 401 && !original._retry && !original.url?.includes("/api/auth/refresh")) {
      original._retry = true;
      try {
        await api.post("/api/auth/refresh");
        return api(original);
      } catch {
        window.location.href = "/login";
      }
    }
    return Promise.reject(error);
  }
);

export default api;
