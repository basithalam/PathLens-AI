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
