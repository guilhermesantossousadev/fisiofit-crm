using Fisiofit.ModuleContracts.People;
using Fisiofit.Modules.Registry.People.Application;
using Xunit;

namespace Fisiofit.UnitTests.Registry.People;

public sealed class PatientSearchTests
{
    [Theory]
    [InlineData(null, "NONE", "")]
    [InlineData("  Ana   Souza  ", "NAME", "Ana Souza")]
    [InlineData("Jose\u0301", "NAME", "José")]
    [InlineData("11144477735", "CPF_OR_PHONE", "11144477735")]
    [InlineData("111.444.777-35", "CPF", "11144477735")]
    [InlineData("+55 (11) 99999-6789", "PHONE", "5511999996789")]
    public void Prepare_NormalizesAndClassifiesSupportedSearch(
        string? search,
        string expectedMode,
        string expectedValue)
    {
        var prepared = SearchPeopleForPatientList.Prepare(Request(search));

        Assert.Null(prepared.Error);
        Assert.Equal(expectedMode, prepared.Mode);
        Assert.Equal(expectedValue, prepared.Value);
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("1234567")]
    [InlineData("1234/5678")]
    [InlineData("+1234567890123456")]
    public void Prepare_RejectsIncompleteOrUnsupportedSearch(string search)
    {
        var prepared = SearchPeopleForPatientList.Prepare(Request(search));

        Assert.Equal(SearchPeopleForPatientListOutcome.ValidationFailure, prepared.Error?.Outcome);
        Assert.Contains("search", prepared.Error!.Errors!.Keys);
    }

    [Theory]
    [InlineData(0, 25, "name")]
    [InlineData(1, 0, "name")]
    [InlineData(1, 101, "name")]
    [InlineData(1, 25, "createdAt")]
    public void Prepare_RejectsPaginationAndSortOutsideWhitelist(int page, int pageSize, string sort)
    {
        var prepared = SearchPeopleForPatientList.Prepare(Request(null) with
        {
            Page = page,
            PageSize = pageSize,
            Sort = sort
        });

        Assert.Equal(SearchPeopleForPatientListOutcome.ValidationFailure, prepared.Error?.Outcome);
    }

    [Fact]
    public void Prepare_RejectsDuplicateOrEmptyEligibleIdentifiers()
    {
        var duplicated = Guid.CreateVersion7();

        var duplicateResult = SearchPeopleForPatientList.Prepare(Request(null) with
        {
            EligiblePersonIds = [duplicated, duplicated]
        });
        var emptyResult = SearchPeopleForPatientList.Prepare(Request(null) with
        {
            EligiblePersonIds = [Guid.Empty]
        });

        Assert.NotNull(duplicateResult.Error);
        Assert.NotNull(emptyResult.Error);
    }

    private static SearchPeopleForPatientListRequest Request(string? search) => new(
        [Guid.CreateVersion7()],
        search,
        1,
        25,
        "name",
        new PeopleActorContext(
            Guid.CreateVersion7(),
            true,
            true,
            false,
            [PeoplePermissions.ReadPerson]),
        "unit-test-correlation");
}
