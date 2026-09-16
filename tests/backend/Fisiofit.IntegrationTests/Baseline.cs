using Fisiofit.ModuleContracts;
using Xunit;

namespace Fisiofit.IntegrationTests;

public sealed class BaselineTests
{
    [Fact]
    public void ModuleContractsAssembly_IsAvailable() =>
        Assert.Equal("Fisiofit.ModuleContracts", typeof(ModuleContractsAssembly).Assembly.GetName().Name);
}
