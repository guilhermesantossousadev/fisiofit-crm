using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Fisiofit.ModuleContracts.Organization;
using Fisiofit.Modules.Registry.Organization.Application;
using Fisiofit.Modules.Registry.Organization.Infrastructure;
using Fisiofit.ModuleContracts.People;
using Fisiofit.Modules.Registry.Patients;
using Fisiofit.Modules.Registry.Patients.Application;
using Fisiofit.Modules.Registry.Patients.Infrastructure;
using Fisiofit.Modules.Registry.People.Application;
using Fisiofit.Modules.Registry.People.Infrastructure;

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
        services.AddScoped<IGetUnitForPatientRead, GetUnitForPatientRead>();

        services.AddDbContext<PeopleDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("Database");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "ConnectionStrings:Database must be configured before People persistence is used.");
            }

            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    PeopleDbContext.MigrationsHistoryTable,
                    PeopleDbContext.Schema));
        });
        services.AddScoped<ICreatePersonForPatientRegistration, CreatePersonForPatientRegistration>();
        services.AddScoped<IGetPersonPatientRegistrationData, GetPersonPatientRegistrationData>();

        services.AddDbContext<PatientsDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("Database");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "ConnectionStrings:Database must be configured before Patients persistence is used.");
            }

            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    PatientsDbContext.MigrationsHistoryTable,
                    PatientsDbContext.Schema));
        });
        services.AddScoped<RegisterPatient>();
        services.AddScoped<GetPatientDetails>();

        return services;
    }

    public static IEndpointRouteBuilder MapRegistryEndpoints(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPatientsEndpoints();
}
