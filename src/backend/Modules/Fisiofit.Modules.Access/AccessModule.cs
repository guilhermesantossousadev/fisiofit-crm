using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Fisiofit.Modules.Access;

public static class AccessModule
{
    public static IServiceCollection AddAccessModule(this IServiceCollection services) => services;

    public static IEndpointRouteBuilder MapAccessEndpoints(this IEndpointRouteBuilder endpoints) => endpoints;
}
