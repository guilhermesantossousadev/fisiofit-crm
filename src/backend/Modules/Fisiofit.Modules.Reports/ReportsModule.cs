using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Fisiofit.Modules.Reports;

public static class ReportsModule
{
    public static IServiceCollection AddReportsModule(this IServiceCollection services) => services;

    public static IEndpointRouteBuilder MapReportsEndpoints(this IEndpointRouteBuilder endpoints) => endpoints;
}
