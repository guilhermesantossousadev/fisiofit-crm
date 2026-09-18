using Fisiofit.ModuleContracts.People;
using Fisiofit.Modules.Registry.Patients.Application;
using Fisiofit.Modules.Registry.Patients.Domain;
using Xunit;

namespace Fisiofit.UnitTests.Registry.Patients;

public sealed class PatientRegistrationTests
{
    [Theory]
    [InlineData(2008, 9, 18, 2026, 9, 18, true)]
    [InlineData(2008, 9, 19, 2026, 9, 18, false)]
    [InlineData(2000, 2, 29, 2018, 2, 28, true)]
    public void AdultBoundary_UsesRelationshipStartDate(
        int birthYear,
        int birthMonth,
        int birthDay,
        int startYear,
        int startMonth,
        int startDay,
        bool expected) =>
        Assert.Equal(
            expected,
            RegisterPatient.IsAdult(
                new DateOnly(birthYear, birthMonth, birthDay),
                new DateOnly(startYear, startMonth, startDay)));

    [Fact]
    public void PatientProfile_CreatesActiveWithoutCivilData()
    {
        var personId = Guid.CreateVersion7();
        var unitId = Guid.CreateVersion7();

        var profile = PatientProfile.Create(personId, unitId, new DateOnly(2026, 9, 18));

        Assert.Equal(PatientAdministrativeStatus.Active, profile.AdministrativeStatus);
        Assert.Equal(personId, profile.PersonId);
        Assert.Equal(unitId, profile.PrimaryUnitId);
        Assert.DoesNotContain(
            typeof(PatientProfile).GetProperties(),
            property => property.Name is "FullName" or "Cpf" or "Phone" or "BirthDate");
    }

    [Fact]
    public void CanonicalHash_IsStableForSemanticallyEquivalentInput()
    {
        var phone = new PatientRegistrationPhone("999", "000", "000000");
        var birthDate = new DateOnly(1990, 1, 1);
        var startDate = new DateOnly(2026, 9, 18);
        var unitId = Guid.Parse("0199ffee-1234-7000-8000-000000000001");

        var first = PatientRequestCanonicalizer.RequestHash(
            PatientRequestCanonicalizer.NormalizeName("  Pessoa   Fictícia "),
            birthDate,
            phone,
            PatientRequestCanonicalizer.NormalizeCpf("111.444.777-35"),
            unitId,
            startDate,
            PatientRequestCanonicalizer.NormalizePayerMode("self"));
        var second = PatientRequestCanonicalizer.RequestHash(
            "Pessoa Fictícia",
            birthDate,
            phone,
            "11144477735",
            unitId,
            startDate,
            "SELF");

        Assert.Equal(first, second);
        Assert.Equal(64, first.Length);
    }
}
