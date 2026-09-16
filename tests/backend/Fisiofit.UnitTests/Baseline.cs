using Fisiofit.BuildingBlocks;
using Xunit;

namespace Fisiofit.UnitTests;

public sealed class BaselineTests
{
    [Fact]
    public void BuildingBlocksAssembly_IsAvailable() =>
        Assert.Equal("Fisiofit.BuildingBlocks", typeof(BuildingBlocksAssembly).Assembly.GetName().Name);
}
