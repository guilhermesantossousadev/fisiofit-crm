using System.Security.Claims;
using System.Text.Encodings.Web;
using Fisiofit.Modules.Registry.Organization.Domain;
using Fisiofit.Modules.Registry.Organization.Infrastructure;
using Fisiofit.Modules.Registry.Patients.Infrastructure;
using Fisiofit.Modules.Registry.People.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;
using Xunit;

namespace Fisiofit.ApiTests.Patients;

public sealed class PatientApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("fisiofit_patient_api_tests")
        .WithUsername("fisiofit_test")
        .WithPassword("test-only-password")
        .Build();
    private PatientWebApplicationFactory? factory;

    public Guid ClinicId { get; } = Guid.CreateVersion7();
    public Guid ActiveUnitId { get; } = Guid.CreateVersion7();
    public Guid InactiveUnitId { get; } = Guid.CreateVersion7();

    public async Task InitializeAsync()
    {
        await container.StartAsync();
        factory = new PatientWebApplicationFactory(container.GetConnectionString());

        await using var scope = factory.Services.CreateAsyncScope();
        var organization = scope.ServiceProvider.GetRequiredService<OrganizationDbContext>();
        var people = scope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        var patients = scope.ServiceProvider.GetRequiredService<PatientsDbContext>();
        await organization.Database.MigrateAsync();
        await people.Database.MigrateAsync();
        await patients.Database.MigrateAsync();

        var clinic = Clinic.Create(ClinicId, "Clínica Fictícia de API", "America/Sao_Paulo");
        var active = Unit.Create(ActiveUnitId, ClinicId, "Unidade Fictícia Ativa");
        var inactive = Unit.Create(InactiveUnitId, ClinicId, "Unidade Fictícia Inativa");
        inactive.Deactivate();
        organization.AddRange(clinic, active, inactive);
        await organization.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        factory?.Dispose();
        await container.DisposeAsync();
    }

    public HttpClient CreateClient() =>
        (factory ?? throw new InvalidOperationException("Fixture is not initialized.")).CreateClient();

    private sealed class PatientWebApplicationFactory(string connectionString)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:Database", connectionString);
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                        options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                        TestAuthenticationHandler.SchemeName,
                        _ => { });
            });
        }
    }
}

internal sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "PatientApiTests";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-Actor", out var actorValues)
            || !Guid.TryParse(actorValues.FirstOrDefault(), out var actorId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, actorId.ToString("D")),
            new("account_status", Request.Headers["X-Test-Account-Status"].FirstOrDefault() ?? "ACTIVE")
        };
        if (string.Equals(Request.Headers["X-Test-Explicit-Deny"].FirstOrDefault(), "true", StringComparison.OrdinalIgnoreCase))
        {
            claims.Add(new Claim("explicit_deny", "true"));
        }

        foreach (var permission in SplitHeader("X-Test-Permissions"))
        {
            claims.Add(new Claim("permission", permission));
        }

        foreach (var unit in SplitHeader("X-Test-Units"))
        {
            claims.Add(new Claim("unit_id", unit));
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }

    private IEnumerable<string> SplitHeader(string name) => Request.Headers[name]
        .SelectMany(value => (value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}

[CollectionDefinition(Name)]
public sealed class PatientApiCollection : ICollectionFixture<PatientApiFixture>
{
    public const string Name = "Patient API PostgreSQL";
}
