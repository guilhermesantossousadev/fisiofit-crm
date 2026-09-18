using Fisiofit.Modules.Registry.Organization.Domain;
using Xunit;

namespace Fisiofit.UnitTests.Registry.Organization;

public sealed class UnitTests
{
    [Fact]
    public void Create_RequiresClinicAndStartsActive()
    {
        var clinicId = Guid.CreateVersion7();

        var unit = Unit.Create(clinicId, "Unidade de Teste");

        Assert.Equal(7, unit.Id.Version);
        Assert.Equal(clinicId, unit.ClinicId);
        Assert.Equal(OrganizationStatus.Active, unit.Status);
    }

    [Fact]
    public void Create_RejectsMissingClinic() =>
        Assert.Throws<ArgumentException>(() => Unit.Create(Guid.Empty, "Unidade de Teste"));

    [Fact]
    public void Lifecycle_TransitionsBetweenActiveAndInactive()
    {
        var unit = Unit.Create(Guid.CreateVersion7(), "Unidade de Teste");

        unit.Deactivate();
        Assert.Equal(OrganizationStatus.Inactive, unit.Status);

        unit.Activate();
        Assert.Equal(OrganizationStatus.Active, unit.Status);
    }
}
