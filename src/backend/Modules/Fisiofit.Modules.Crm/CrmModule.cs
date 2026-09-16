using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Fisiofit.Modules.Crm;

public static class CrmModule
{
    public static IServiceCollection AddCrmModule(this IServiceCollection services) => services;

    public static IEndpointRouteBuilder MapCrmEndpoints(this IEndpointRouteBuilder endpoints) => endpoints;
}
