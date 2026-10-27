namespace PathLens.Domain.Reports;

public enum ObstacleType
{
    BlockedSidewalk,
    BrokenSurface,
    MissingOrBlockedRamp,
    StairsOrStep,
    ConstructionBarrier,
    Other,
    Unknown,
}

public enum SeverityLevel
{
    Unknown,
    Low,
    Medium,
    High,
}

public enum ReportStatus
{
    Active,
    Archived,
}

public enum VerificationStatus
{
    Unverified,
    HumanVerified,
    Disputed,
    Rejected,
}

public enum AnalysisStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
}

public enum EvidenceComparisonOutcome
{
    LikelyStillPresent,
    NotObservedInNewImage,
    ConflictingEvidence,
    Inconclusive,
}
