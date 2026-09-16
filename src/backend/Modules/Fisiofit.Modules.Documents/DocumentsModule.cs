using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Fisiofit.Modules.Documents;

public static class DocumentsModule
{
    public static IServiceCollection AddDocumentsModule(this IServiceCollection services) => services;

    public static IEndpointRouteBuilder MapDocumentsEndpoints(this IEndpointRouteBuilder endpoints) => endpoints;
}
