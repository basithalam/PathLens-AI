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

export interface EvidenceItem {
  id: string;
  reportId: string;
  fileName: string;
  contentType: string;
  fileSize: number;
  fileUrl: string;
  captureTimeUtc: string | null;
  uploadedAt: string;
  latitude: number | null;
  longitude: number | null;
  locationAccuracyMeters: number | null;
  sourceType: string;
  analysisStatus: string;
  analysisResultJson: string | null;
  analysisProvider: string | null;
  analysisModel: string | null;
  analyzedAt: string | null;
}

export interface AnalysisResult {
  schemaVersion: string;
  obstacleType: string;
  visibleEvidence: string[];
  limitations: string[];
  confidence: number;
  humanReviewRequired: boolean;
  provider: string;
  model: string | null;
  analyzedAt: string;
}

export interface AnalysisJob {
  id: string;
  evidenceItemId: string;
  status: string;
  attempts: number;
  errorMessage: string | null;
  createdAt: string;
  completedAt: string | null;
}

export function parseAnalysisResult(item: EvidenceItem): AnalysisResult | null {
  if (!item.analysisResultJson) {
    return null;
  }
  try {
    return JSON.parse(item.analysisResultJson) as AnalysisResult;
  } catch {
    return null;
  }
}

export function evidenceFileUrl(item: EvidenceItem): string {
  return `${API_BASE_URL}${item.fileUrl}`;
}

export function getEvidenceForReport(reportId: string): Promise<EvidenceItem[] | null> {
  return fetchJson<EvidenceItem[]>(`/api/reports/${reportId}/evidence`);
}

export async function getEvidence(id: string): Promise<EvidenceItem | null> {
  return fetchJson<EvidenceItem>(`/api/evidence/${id}`);
}

export async function getAnalysisJob(id: string): Promise<AnalysisJob | null> {
  return fetchJson<AnalysisJob>(`/api/analysis-jobs/${id}`);
}

export async function startAnalysis(evidenceId: string): Promise<AnalysisJob | null> {
  try {
    const response = await fetch(`${API_BASE_URL}/api/evidence/${evidenceId}/analyze`, {
      method: "POST",
      signal: AbortSignal.timeout(8000),
    });
    if (!response.ok) {
      return null;
    }
    return (await response.json()) as AnalysisJob;
  } catch {
    return null;
  }
}

export type UploadEvidenceResult =
  | { ok: true; item: EvidenceItem }
  | { ok: false; message: string };

/** Uploads with XMLHttpRequest so the UI can show real upload progress. */
export function uploadEvidence(
  reportId: string,
  file: File,
  onProgress: (percent: number) => void,
): Promise<UploadEvidenceResult> {
  return new Promise((resolve) => {
    const request = new XMLHttpRequest();
    request.open("POST", `${API_BASE_URL}/api/reports/${reportId}/evidence`);

    request.upload.onprogress = (event) => {
      if (event.lengthComputable) {
        onProgress(Math.round((event.loaded / event.total) * 100));
      }
    };

    request.onload = () => {
      if (request.status >= 200 && request.status < 300) {
        try {
          resolve({ ok: true, item: JSON.parse(request.responseText) as EvidenceItem });
        } catch {
          resolve({ ok: false, message: "The API returned an unreadable response." });
        }
        return;
      }

      try {
        const problem = JSON.parse(request.responseText) as {
          errors?: Record<string, string[]>;
          title?: string;
        };
        const firstError = problem.errors ? Object.values(problem.errors).flat()[0] : undefined;
        resolve({ ok: false, message: firstError ?? problem.title ?? `Upload failed (${request.status}).` });
      } catch {
        resolve({ ok: false, message: `Upload failed (${request.status}).` });
      }
    };

    request.onerror = () =>
      resolve({ ok: false, message: "The API is not reachable. Check that the backend is running." });

    const form = new FormData();
    form.append("file", file);
    request.send(form);
  });
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
