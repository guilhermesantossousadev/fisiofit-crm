using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Fisiofit.ModuleContracts.Organization;
using Fisiofit.Modules.Registry.Organization.Application;
using Fisiofit.Modules.Registry.Organization.Infrastructure;

namespace Fisiofit.Modules.Registry;

public static class RegistryModule
{
    public static IServiceCollection AddRegistryModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<OrganizationDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("Database");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "ConnectionStrings:Database must be configured before Organization persistence is used.");
            }

            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    OrganizationDbContext.MigrationsHistoryTable,
                    OrganizationDbContext.Schema));
        });
        services.AddScoped<IValidateUnitForPatientRegistration, ValidateUnitForPatientRegistration>();

        return services;
    }

    public static IEndpointRouteBuilder MapRegistryEndpoints(this IEndpointRouteBuilder endpoints) => endpoints;
}
