using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Fisiofit.Modules.Clinical;

public static class ClinicalModule
{
    public static IServiceCollection AddClinicalModule(this IServiceCollection services) => services;

    public static IEndpointRouteBuilder MapClinicalEndpoints(this IEndpointRouteBuilder endpoints) => endpoints;
}
