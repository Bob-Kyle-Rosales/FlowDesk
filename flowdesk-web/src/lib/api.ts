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
  // No Content-Type default — axios sets application/json for objects automatically
  // and multipart/form-data (with boundary) for FormData. A hardcoded default breaks uploads.
});

api.interceptors.response.use(
  (res) => res,
  async (error) => {
    const original = error.config;
    const isAuthEndpoint = original.url?.includes("/api/auth/");
    if (error.response?.status === 401 && !original._retry && !isAuthEndpoint) {
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
