using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Fisiofit.ModuleContracts.PrivacyAudit;
using Fisiofit.Modules.Audit.Application;
using Fisiofit.Modules.Audit.Infrastructure;

namespace Fisiofit.Modules.Audit;

public static class AuditModule
{
    public static IServiceCollection AddAuditModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:Database must be configured before Audit persistence is used.");
        }

        services.AddDbContext<AuditDbContext>(options => options.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable(AuditDbContext.MigrationsHistoryTable, AuditDbContext.Schema)));
        services.AddScoped<IRecordAdministrativeEvidence, RecordAdministrativeEvidence>();
        return services;
    }

    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder endpoints) => endpoints;
}
