using Fisiofit.Modules.Registry.Patients.Application;
using Fisiofit.Modules.Registry.Patients.Domain;
using Npgsql;
using Xunit;

namespace Fisiofit.UnitTests.Registry.Patients;

public sealed class GuardianLinkTests
{
    [Fact]
    public void Create_UsesHalfOpenIntervalAndInitialVersion()
    {
        var from = new DateOnly(2026, 9, 24);
        var link = GuardianLink.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), from, from.AddDays(2), false, Guid.CreateVersion7(), DateTimeOffset.UtcNow);

        Assert.True(link.AppliesOn(from));
        Assert.True(link.AppliesOn(from.AddDays(1)));
        Assert.False(link.AppliesOn(from.AddDays(2)));
        Assert.Equal(1, link.Version);
    }

    [Fact]
    public void End_PreservesGuardianAndIncrementsVersion()
    {
        var guardian = Guid.CreateVersion7();
        var link = GuardianLink.Create(Guid.CreateVersion7(), guardian, new DateOnly(2026, 9, 24), null, true, Guid.CreateVersion7(), DateTimeOffset.UtcNow);

        link.End(new DateOnly(2026, 9, 26), Guid.CreateVersion7(), DateTimeOffset.UtcNow);

        Assert.Equal(guardian, link.GuardianPersonId);
        Assert.Equal(2, link.Version);
        Assert.Equal(new DateOnly(2026, 9, 26), link.EffectiveTo);
    }

    [Fact]
    public void MinorCoverage_RequiresContinuousOtherGuardianCoverageUntilAdulthood()
    {
        var patient = Guid.CreateVersion7();
        var start = new DateOnly(2026, 9, 24);
        var adultDate = new DateOnly(2030, 9, 24);
        var covering = GuardianLink.Create(patient, Guid.CreateVersion7(), start, null, false, Guid.CreateVersion7(), DateTimeOffset.UtcNow);

        Assert.True(ManageGuardianLinks.HasMinorCoverage([covering], start, adultDate.AddYears(-18)));
    }

    [Fact]
    public void MinorCoverage_RejectsGapBeforeAdulthood()
    {
        var start = new DateOnly(2026, 9, 24);
        var birthDate = new DateOnly(2012, 9, 24);
        var futureOnly = GuardianLink.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), start.AddDays(1), null, false, Guid.CreateVersion7(), DateTimeOffset.UtcNow);

        Assert.False(ManageGuardianLinks.HasMinorCoverage([futureOnly], start, birthDate));
    }

    [Fact]
    public void RetryClassifier_AcceptsOnlyPostgreSqlConfirmedTransactionAborts()
    {
        var deadlock = new PostgresException("deadlock", "ERROR", "ERROR", PostgresErrorCodes.DeadlockDetected);
        var serialization = new PostgresException("serialization", "ERROR", "ERROR", PostgresErrorCodes.SerializationFailure);
        var unique = new PostgresException("unique", "ERROR", "ERROR", PostgresErrorCodes.UniqueViolation);

        Assert.True(ManageGuardianLinks.IsProvenAbortedTransaction(deadlock));
        Assert.True(ManageGuardianLinks.IsProvenAbortedTransaction(new InvalidOperationException("wrapped", serialization)));
        Assert.False(ManageGuardianLinks.IsProvenAbortedTransaction(unique));
        Assert.False(ManageGuardianLinks.IsProvenAbortedTransaction(new TimeoutException()));
    }
}
