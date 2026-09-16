using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Fisiofit.Modules.Registry;

public static class RegistryModule
{
    public static IServiceCollection AddRegistryModule(this IServiceCollection services) => services;

    public static IEndpointRouteBuilder MapRegistryEndpoints(this IEndpointRouteBuilder endpoints) => endpoints;
}
