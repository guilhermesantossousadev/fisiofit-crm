using Fisiofit.Modules.Registry.Organization.Domain;
using Xunit;

namespace Fisiofit.UnitTests.Registry.Organization;

public sealed class ClinicTests
{
    [Fact]
    public void Create_ProducesActiveClinicWithVersion7Identifier()
    {
        var clinic = Clinic.Create("Clínica de Teste", "America/Sao_Paulo");

        Assert.Equal(7, clinic.Id.Version);
        Assert.Equal(OrganizationStatus.Active, clinic.Status);
        Assert.Equal("Clínica de Teste", clinic.Name);
        Assert.Equal("America/Sao_Paulo", clinic.TimeZoneId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RejectsEmptyName(string name) =>
        Assert.Throws<ArgumentException>(() => Clinic.Create(name, "America/Sao_Paulo"));

    [Fact]
    public void Create_RejectsUnknownTimeZone() =>
        Assert.Throws<ArgumentException>(() => Clinic.Create("Clínica de Teste", "Invalid/TimeZone"));

    [Fact]
    public void Lifecycle_TransitionsBetweenActiveAndInactive()
    {
        var clinic = Clinic.Create("Clínica de Teste", "America/Sao_Paulo");

        clinic.Deactivate();
        Assert.Equal(OrganizationStatus.Inactive, clinic.Status);

        clinic.Activate();
        Assert.Equal(OrganizationStatus.Active, clinic.Status);
    }
}
