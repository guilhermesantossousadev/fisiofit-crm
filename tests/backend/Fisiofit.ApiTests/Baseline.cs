using Xunit;

namespace Fisiofit.ApiTests;

public sealed class BaselineTests
{
    [Fact]
    public void ApiHostAssembly_IsAvailable() =>
        Assert.Equal("Fisiofit.Api", typeof(Program).Assembly.GetName().Name);
}
