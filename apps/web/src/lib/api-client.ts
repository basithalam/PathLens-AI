export interface HealthCheckEntry {
  name: string;
  status: string;
  description?: string | null;
}

export interface HealthResponse {
  status: string;
  checks: HealthCheckEntry[];
}

const API_BASE_URL =
  process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5145";

/**
 * Fetches a backend health endpoint. Returns null when the API is
 * unreachable so pages can render an honest error state instead of crashing.
 */
export interface AccessibilityReport {
  id: string;
  title: string;
  description: string | null;
  obstacleType: string;
  severity: string;
  latitude: number;
  longitude: number;
  status: string;
  verificationStatus: string;
  createdAt: string;
  updatedAt: string;
  lastEvidenceAt: string | null;
}

export interface PagedReports {
  items: AccessibilityReport[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface ReportListQuery {
  search?: string;
  obstacleType?: string;
  page?: number;
  pageSize?: number;
}

async function fetchJson<T>(path: string): Promise<T | null> {
  try {
    const response = await fetch(`${API_BASE_URL}${path}`, {
      cache: "no-store",
      signal: AbortSignal.timeout(5000),
    });
    if (!response.ok) {
      return null;
    }
    return (await response.json()) as T;
  } catch {
    return null;
  }
}

export function getReports(query: ReportListQuery = {}): Promise<PagedReports | null> {
  const params = new URLSearchParams();
  if (query.search) params.set("search", query.search);
  if (query.obstacleType) params.set("obstacleType", query.obstacleType);
  params.set("page", String(query.page ?? 1));
  params.set("pageSize", String(query.pageSize ?? 20));
  return fetchJson<PagedReports>(`/api/reports?${params.toString()}`);
}

export function getReport(id: string): Promise<AccessibilityReport | null> {
  return fetchJson<AccessibilityReport>(`/api/reports/${id}`);
}

export interface CreateReportInput {
  title: string;
  description: string | null;
  obstacleType: string;
  severity: string;
  latitude: number;
  longitude: number;
}

export type CreateReportResult =
  | { ok: true; report: AccessibilityReport }
  | { ok: false; errors: Record<string, string[]>; message: string };

export async function createReport(input: CreateReportInput): Promise<CreateReportResult> {
  try {
    const response = await fetch(`${API_BASE_URL}/api/reports`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(input),
      signal: AbortSignal.timeout(8000),
    });

    if (response.ok) {
      return { ok: true, report: (await response.json()) as AccessibilityReport };
    }

    const problem = (await response.json().catch(() => null)) as
      | { errors?: Record<string, string[]>; title?: string }
      | null;
    return {
      ok: false,
      errors: problem?.errors ?? {},
      message: problem?.title ?? `Request failed (${response.status}).`,
    };
  } catch {
    return {
      ok: false,
      errors: {},
      message: "The API is not reachable. Check that the backend is running.",
    };
  }
}

export async function getHealth(
  kind: "live" | "ready",
): Promise<HealthResponse | null> {
  try {
    const response = await fetch(`${API_BASE_URL}/health/${kind}`, {
      cache: "no-store",
      signal: AbortSignal.timeout(3000),
    });
    if (!response.ok && response.status !== 503) {
      return null;
    }
    return (await response.json()) as HealthResponse;
  } catch {
    return null;
  }
}
