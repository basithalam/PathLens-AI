using PathLens.Domain.Cameras;

namespace PathLens.UnitTests.Cameras;

public sealed class CameraSourceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);

    private static CameraSource Create(string? permissionNote = null, int cadence = 60) => CameraSource.Create(
        CameraProvider.NycDot, "cam-1", "5th Ave & 59th", "New York",
        40.7644, -73.9733, "https://example.test/cam.jpg", null, cadence, permissionNote, Now);

    [Fact]
    public void Create_clamps_cadence_below_minimum()
    {
        var source = Create(cadence: 10);
        Assert.Equal(CameraSource.MinimumCadenceSeconds, source.CadenceSeconds);
    }

    [Fact]
    public void Enabling_without_permission_note_is_blocked()
    {
        var source = Create();
        Assert.Throws<InvalidOperationException>(() => source.SetEnabled(true, Now));
    }

    [Fact]
    public void Enabling_with_permission_note_works_and_staleness_is_time_based()
    {
        var source = Create(permissionNote: "Public DOT feed");
        source.SetEnabled(true, Now);

        Assert.True(source.Enabled);
        // No success yet, so stale by any threshold.
        Assert.True(source.IsStale(Now, TimeSpan.FromMinutes(10)));

        source.RecordSuccess(Now, "hash", changed: true);
        Assert.False(source.IsStale(Now.AddMinutes(5), TimeSpan.FromMinutes(10)));
        Assert.True(source.IsStale(Now.AddMinutes(20), TimeSpan.FromMinutes(10)));
    }

    [Fact]
    public void Removing_permission_note_auto_disables()
    {
        var source = Create(permissionNote: "Public DOT feed");
        source.SetEnabled(true, Now);
        source.UpdatePermissionNote(null, Now);

        Assert.False(source.Enabled);
        Assert.False(source.HasPermissionNote);
    }

    [Fact]
    public void Cadence_update_clamps_to_minimum()
    {
        var source = Create();
        source.UpdateCadence(5, Now);
        Assert.Equal(CameraSource.MinimumCadenceSeconds, source.CadenceSeconds);
        source.UpdateCadence(60, Now);
        Assert.Equal(60, source.CadenceSeconds);
    }

    [Fact]
    public void Failures_back_off_exponentially_and_success_resets()
    {
        var source = Create(permissionNote: "note");
        source.SetEnabled(true, Now);

        source.RecordFailure(Now, "timeout");
        Assert.Equal(TimeSpan.FromSeconds(120), source.EffectivePollInterval());

        source.RecordFailure(Now, "timeout");
        Assert.Equal(TimeSpan.FromSeconds(240), source.EffectivePollInterval());

        source.RecordSuccess(Now, "abc", changed: true);
        Assert.Equal(TimeSpan.FromSeconds(60), source.EffectivePollInterval());
        Assert.Equal(0, source.ConsecutiveFailures);
        Assert.Equal("abc", source.LastImageHash);
    }

    [Fact]
    public void Unchanged_success_does_not_move_change_timestamp()
    {
        var source = Create();
        source.RecordSuccess(Now, "hash1", changed: true);
        source.RecordSuccess(Now.AddMinutes(1), "hash1", changed: false);

        Assert.Equal(Now, source.LastChangeAt);
        Assert.Equal(Now.AddMinutes(1), source.LastSuccessAt);
    }

    [Fact]
    public void IsDue_respects_cadence_after_attempt()
    {
        var source = Create(permissionNote: "note");
        source.SetEnabled(true, Now);
        source.RecordAttempt(Now);

        Assert.False(source.IsDue(Now.AddSeconds(59)));
        Assert.True(source.IsDue(Now.AddSeconds(60)));
    }

    [Fact]
    public void Budget_skip_acknowledges_change_and_keeps_the_reason_visible()
    {
        var source = Create(permissionNote: "note");
        source.RecordBudgetSkip(Now, "hash9", "Daily analysis budget spent.");

        Assert.Equal("hash9", source.LastImageHash);
        Assert.Equal(Now, source.LastChangeAt);
        Assert.Equal("Daily analysis budget spent.", source.LastError);
        Assert.Equal(0, source.ConsecutiveFailures);
    }

    [Fact]
    public void Disabled_source_is_never_due()
    {
        var source = Create();
        Assert.False(source.IsDue(Now.AddHours(1)));
    }
}
