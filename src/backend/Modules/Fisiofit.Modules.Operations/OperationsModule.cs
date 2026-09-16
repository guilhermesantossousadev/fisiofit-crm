using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Fisiofit.Modules.Operations;

public static class OperationsModule
{
    public static IServiceCollection AddOperationsModule(this IServiceCollection services) => services;

    public static IEndpointRouteBuilder MapOperationsEndpoints(this IEndpointRouteBuilder endpoints) => endpoints;
}
