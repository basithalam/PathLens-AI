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
  cameraSourceId: string | null;
  hasStairs: boolean | null;
  hasRamp: boolean | null;
  surface: string | null;
  attributeSource: string | null;
}

export interface CameraSource {
  id: string;
  provider: string;
  externalId: string;
  label: string;
  city: string;
  latitude: number;
  longitude: number;
  feedImageUrl: string;
  directionLabel: string | null;
  cadenceSeconds: number;
  enabled: boolean;
  hasPermissionNote: boolean;
  lastAttemptAt: string | null;
  lastSuccessAt: string | null;
  lastChangeAt: string | null;
  lastError: string | null;
  consecutiveFailures: number;
  isStale: boolean;
  createdAt: string;
}

export function getCameras(): Promise<CameraSource[] | null> {
  return fetchJson<CameraSource[]>("/api/cameras");
}

/** Great-circle distance in metres, matching the backend's haversine. */
export function distanceMeters(
  a: { latitude: number; longitude: number },
  b: { latitude: number; longitude: number },
): number {
  const earth = 6_371_000;
  const toRad = (deg: number) => (deg * Math.PI) / 180;
  const dLat = toRad(b.latitude - a.latitude);
  const dLon = toRad(b.longitude - a.longitude);
  const h =
    Math.sin(dLat / 2) ** 2 +
    Math.cos(toRad(a.latitude)) * Math.cos(toRad(b.latitude)) * Math.sin(dLon / 2) ** 2;
  return earth * 2 * Math.atan2(Math.sqrt(h), Math.sqrt(1 - h));
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

export interface ReportReview {
  id: string;
  entityType: string;
  entityId: string;
  actorUserId: string;
  action: string;
  previousValue: string | null;
  newValue: string | null;
  note: string | null;
  createdAt: string;
}

export function getReportReviews(id: string): Promise<ReportReview[] | null> {
  return fetchJson<ReportReview[]>(`/api/reports/${id}/reviews`);
}

export interface UpdateReportInput {
  title: string;
  description: string | null;
  obstacleType: string;
  severity: string;
}

export async function updateReport(
  id: string,
  input: UpdateReportInput,
): Promise<{ ok: true; report: AccessibilityReport } | { ok: false; errors: Record<string, string[]>; message: string }> {
  try {
    const response = await fetch(`${API_BASE_URL}/api/reports/${id}`, {
      method: "PATCH",
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
      message: problem?.title ?? `Update failed (${response.status}).`,
    };
  } catch {
    return { ok: false, errors: {}, message: "The API is not reachable. Check that the backend is running." };
  }
}

export interface CreateReportInput {
  title: string;
  description: string | null;
  obstacleType: string;
  severity: string;
  latitude: number;
  longitude: number;
  hasStairs?: boolean | null;
  hasRamp?: boolean | null;
  surface?: string | null;
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
  captureTimeUtc?: string,
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
    if (captureTimeUtc) {
      form.append("captureTimeUtc", captureTimeUtc);
    }
    request.send(form);
  });
}

export interface EvidenceComparison {
  id: string;
  reportId: string;
  earlierEvidenceId: string;
  laterEvidenceId: string;
  outcome: string;
  effectiveOutcome: string;
  explanation: string;
  supportingEvidenceJson: string;
  modelVersion: string | null;
  createdAt: string;
  reviewedOutcome: string | null;
  reviewReason: string | null;
  reviewedAt: string | null;
}

export function getComparisons(reportId: string): Promise<EvidenceComparison[] | null> {
  return fetchJson<EvidenceComparison[]>(`/api/reports/${reportId}/comparisons`);
}

export async function createComparison(
  earlierEvidenceId: string,
  laterEvidenceId: string,
): Promise<{ ok: true; comparison: EvidenceComparison } | { ok: false; message: string }> {
  try {
    const response = await fetch(`${API_BASE_URL}/api/comparisons`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ earlierEvidenceId, laterEvidenceId }),
      signal: AbortSignal.timeout(8000),
    });

    if (response.ok) {
      return { ok: true, comparison: (await response.json()) as EvidenceComparison };
    }

    const problem = (await response.json().catch(() => null)) as
      | { errors?: Record<string, string[]>; title?: string }
      | null;
    const firstError = problem?.errors ? Object.values(problem.errors).flat()[0] : undefined;
    return { ok: false, message: firstError ?? problem?.title ?? `Comparison failed (${response.status}).` };
  } catch {
    return { ok: false, message: "The API is not reachable. Check that the backend is running." };
  }
}

export async function reviewComparison(
  id: string,
  reviewOutcome: string,
  reason: string,
): Promise<EvidenceComparison | null> {
  try {
    const headers: Record<string, string> = { "Content-Type": "application/json" };
    if (typeof window !== "undefined") {
      const token = window.localStorage.getItem("pathlens.accessToken");
      if (token) {
        headers.Authorization = `Bearer ${token}`;
      }
    }

    const response = await fetch(`${API_BASE_URL}/api/comparisons/${id}/review`, {
      method: "POST",
      headers,
      body: JSON.stringify({ reviewOutcome, reason }),
      signal: AbortSignal.timeout(8000),
    });
    if (!response.ok) {
      return null;
    }
    return (await response.json()) as EvidenceComparison;
  } catch {
    return null;
  }
}

export interface MapViewport {
  minLatitude: number;
  maxLatitude: number;
  minLongitude: number;
  maxLongitude: number;
}

export function getReportsInViewport(
  viewport: MapViewport,
  filters: { obstacleType?: string; verificationStatus?: string } = {},
): Promise<AccessibilityReport[] | null> {
  const params = new URLSearchParams({
    minLatitude: String(viewport.minLatitude),
    maxLatitude: String(viewport.maxLatitude),
    minLongitude: String(viewport.minLongitude),
    maxLongitude: String(viewport.maxLongitude),
    limit: "200",
  });
  if (filters.obstacleType) params.set("obstacleType", filters.obstacleType);
  if (filters.verificationStatus) params.set("verificationStatus", filters.verificationStatus);
  return fetchJson<AccessibilityReport[]>(`/api/map/reports?${params.toString()}`);
}

export interface MapSummary {
  totalReports: number;
  byObstacleType: Record<string, number>;
  byVerificationStatus: Record<string, number>;
  latestReportAt: string | null;
}

export function getMapSummary(): Promise<MapSummary | null> {
  return fetchJson<MapSummary>("/api/map/summary");
}

export interface NearbyObstacle {
  reportId: string;
  title: string;
  obstacleType: string;
  verificationStatus: string;
  distanceMeters: number;
  evidenceAt: string;
  latitude: number;
  longitude: number;
  isCameraCandidate: boolean;
  hasStairs: boolean | null;
  hasRamp: boolean | null;
  surface: string | null;
}

export interface EvaluatedRoute {
  index: number;
  geometry: { latitude: number; longitude: number }[];
  distanceMeters: number;
  durationSeconds: number;
  knownObstacles: NearbyObstacle[];
  unknownLengthPercent: number;
  newestEvidenceAt: string | null;
  explanation: string[];
  hasConfirmedStairs: boolean;
  staleObstacleCount: number;
}

export interface RoutePreferences {
  avoidStairs: boolean;
  preferVerifiedEvidence: boolean;
  avoidStaleEvidence: boolean;
  preferShortest: boolean;
}

export const DEFAULT_ROUTE_PREFERENCES: RoutePreferences = {
  avoidStairs: true,
  preferVerifiedEvidence: false,
  avoidStaleEvidence: false,
  preferShortest: false,
};

export interface RouteRecommendation {
  id: string;
  provider: string;
  profile: string;
  origin: { latitude: number; longitude: number };
  destination: { latitude: number; longitude: number };
  routes: EvaluatedRoute[];
  limitations: string[];
  createdAt: string;
  appliedPreferences: string[];
}

export async function recommendRoutes(
  origin: { latitude: number; longitude: number },
  destination: { latitude: number; longitude: number },
  preferences: RoutePreferences = DEFAULT_ROUTE_PREFERENCES,
): Promise<{ ok: true; recommendation: RouteRecommendation } | { ok: false; message: string }> {
  try {
    const response = await fetch(`${API_BASE_URL}/api/routes/recommend`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        originLatitude: origin.latitude,
        originLongitude: origin.longitude,
        destinationLatitude: destination.latitude,
        destinationLongitude: destination.longitude,
        preferences,
      }),
      signal: AbortSignal.timeout(30000),
    });

    if (response.ok) {
      return { ok: true, recommendation: (await response.json()) as RouteRecommendation };
    }

    const problem = (await response.json().catch(() => null)) as
      | { errors?: Record<string, string[]>; title?: string; detail?: string }
      | null;
    const firstError = problem?.errors ? Object.values(problem.errors).flat()[0] : undefined;
    return {
      ok: false,
      message:
        firstError ??
        problem?.detail ??
        problem?.title ??
        `Route request failed (${response.status}). No route is shown when the provider fails.`,
    };
  } catch {
    return {
      ok: false,
      message: "The API is not reachable. Check that the backend is running. No route is shown instead.",
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
