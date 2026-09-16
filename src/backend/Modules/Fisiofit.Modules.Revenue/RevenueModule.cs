using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Fisiofit.Modules.Revenue;

public static class RevenueModule
{
    public static IServiceCollection AddRevenueModule(this IServiceCollection services) => services;

    public static IEndpointRouteBuilder MapRevenueEndpoints(this IEndpointRouteBuilder endpoints) => endpoints;
}
