const API_BASE_URL =
  process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5145";

const TOKEN_KEY = "pathlens.accessToken";
const USER_KEY = "pathlens.user";

export interface SessionUser {
  userId: string;
  email: string;
  displayName: string;
  role: string;
}

export function getToken(): string | null {
  if (typeof window === "undefined") {
    return null;
  }
  return window.localStorage.getItem(TOKEN_KEY);
}

export function getSessionUser(): SessionUser | null {
  if (typeof window === "undefined") {
    return null;
  }
  const raw = window.localStorage.getItem(USER_KEY);
  if (!raw) {
    return null;
  }
  try {
    return JSON.parse(raw) as SessionUser;
  } catch {
    return null;
  }
}

export function clearSession() {
  window.localStorage.removeItem(TOKEN_KEY);
  window.localStorage.removeItem(USER_KEY);
}

export async function login(
  email: string,
  password: string,
): Promise<{ ok: true; user: SessionUser } | { ok: false; message: string }> {
  try {
    const response = await fetch(`${API_BASE_URL}/api/auth/login`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ email, password }),
      signal: AbortSignal.timeout(8000),
    });

    if (!response.ok) {
      return {
        ok: false,
        message:
          response.status === 401
            ? "Invalid email or password."
            : `Login failed (${response.status}).`,
      };
    }

    const data = (await response.json()) as {
      accessToken: string;
      userId: string;
      email: string;
      displayName: string;
      role: string;
    };

    const user: SessionUser = {
      userId: data.userId,
      email: data.email,
      displayName: data.displayName,
      role: data.role,
    };
    window.localStorage.setItem(TOKEN_KEY, data.accessToken);
    window.localStorage.setItem(USER_KEY, JSON.stringify(user));
    return { ok: true, user };
  } catch {
    return { ok: false, message: "The API is not reachable. Check that the backend is running." };
  }
}

export async function authFetch(path: string, init: RequestInit = {}): Promise<Response> {
  const token = getToken();
  const headers = new Headers(init.headers);
  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  }
  if (init.body && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  return fetch(`${API_BASE_URL}${path}`, { ...init, headers, signal: AbortSignal.timeout(10000) });
}
