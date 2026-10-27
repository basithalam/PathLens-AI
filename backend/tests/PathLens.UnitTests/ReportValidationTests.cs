using PathLens.Application.Reports;
using PathLens.Domain.Reports;

namespace PathLens.UnitTests;

public class ReportValidationTests
{
    [Fact]
    public void Valid_request_has_no_errors()
    {
        var errors = ReportRequestValidator.ValidateCreate(new CreateReportRequest(
            "Blocked sidewalk", null, "BlockedSidewalk", "High", 23.8, 90.4));

        Assert.Empty(errors);
    }

    [Fact]
    public void Invalid_coordinates_and_unknown_enums_are_rejected()
    {
        var errors = ReportRequestValidator.ValidateCreate(new CreateReportRequest(
            "", null, "NotAThing", "Severe", 123, -999));

        Assert.Contains("title", errors.Keys);
        Assert.Contains("latitude", errors.Keys);
        Assert.Contains("longitude", errors.Keys);
        Assert.Contains("obstacleType", errors.Keys);
        Assert.Contains("severity", errors.Keys);
    }

    [Fact]
    public void Report_factory_rejects_out_of_range_latitude()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AccessibilityReport.Create("Title", null, ObstacleType.Other, SeverityLevel.Low,
                latitude: 91, longitude: 0, null, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void New_report_starts_active_and_unverified()
    {
        var report = AccessibilityReport.Create("Title", null, ObstacleType.Other, SeverityLevel.Low,
            10, 20, null, DateTimeOffset.UtcNow);

        Assert.Equal(ReportStatus.Active, report.Status);
        Assert.Equal(VerificationStatus.Unverified, report.VerificationStatus);
    }
}
