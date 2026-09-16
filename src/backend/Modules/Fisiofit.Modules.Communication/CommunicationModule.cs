using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Fisiofit.Modules.Communication;

public static class CommunicationModule
{
    public static IServiceCollection AddCommunicationModule(this IServiceCollection services) => services;

    public static IEndpointRouteBuilder MapCommunicationEndpoints(this IEndpointRouteBuilder endpoints) => endpoints;
}
