using Fisiofit.ModuleContracts.People;
using Xunit;

namespace Fisiofit.ArchitectureTests;

public sealed class RegistryPatientRegistrationBoundaryTests
{
    [Fact]
    public void Patients_DoesNotReferencePeopleOrOrganizationInternals()
    {
        var sources = ContextSources("Patients");

        Assert.All(sources, source =>
        {
            Assert.DoesNotContain("People.Domain", source, StringComparison.Ordinal);
            Assert.DoesNotContain("People.Infrastructure", source, StringComparison.Ordinal);
            Assert.DoesNotContain("PeopleDbContext", source, StringComparison.Ordinal);
            Assert.DoesNotContain("Organization.Infrastructure", source, StringComparison.Ordinal);
            Assert.DoesNotContain("OrganizationDbContext", source, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void People_DoesNotReferencePatientsInternals()
    {
        var sources = ContextSources("People");

        Assert.All(sources, source =>
        {
            Assert.DoesNotContain("Patients.Domain", source, StringComparison.Ordinal);
            Assert.DoesNotContain("Patients.Infrastructure", source, StringComparison.Ordinal);
            Assert.DoesNotContain("PatientsDbContext", source, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void DbContexts_DeclareOnlyOwnerSchema()
    {
        var registryRoot = RegistryRoot();
        var people = File.ReadAllText(Path.Combine(registryRoot, "People", "Infrastructure", "PeopleDbContext.cs"));
        var patients = File.ReadAllText(Path.Combine(registryRoot, "Patients", "Infrastructure", "PatientsDbContext.cs"));
        var organization = File.ReadAllText(Path.Combine(registryRoot, "Organization", "Infrastructure", "OrganizationDbContext.cs"));

        Assert.Contains("Schema = \"people\"", people, StringComparison.Ordinal);
        Assert.DoesNotContain("\"patients\"", people, StringComparison.Ordinal);
        Assert.DoesNotContain("\"organization\"", people, StringComparison.Ordinal);
        Assert.Contains("Schema = \"patients\"", patients, StringComparison.Ordinal);
        Assert.DoesNotContain("\"people\"", patients, StringComparison.Ordinal);
        Assert.DoesNotContain("\"organization\"", patients, StringComparison.Ordinal);
        Assert.Contains("Schema = \"organization\"", organization, StringComparison.Ordinal);
        Assert.DoesNotContain("\"people\"", organization, StringComparison.Ordinal);
        Assert.DoesNotContain("\"patients\"", organization, StringComparison.Ordinal);
    }

    [Fact]
    public void Migrations_OnlyMentionTheirOwnerSchema()
    {
        AssertMigrationScope("People", "People", "people", ["patients", "organization"]);
        AssertMigrationScope("Patients", "Patients", "patients", ["people", "organization"]);
    }

    [Fact]
    public void PeopleContracts_DoNotExposeRegistryOrEfTypes()
    {
        var contracts = new[]
        {
            typeof(ICreatePersonForPatientRegistration),
            typeof(IGetPersonPatientRegistrationData)
        };

        var exposed = contracts
            .SelectMany(type => type.GetMethods())
            .SelectMany(method => method.GetParameters().Select(parameter => parameter.ParameterType).Append(method.ReturnType))
            .SelectMany(Flatten)
            .ToArray();

        Assert.DoesNotContain(exposed, type => type.Namespace?.StartsWith("Fisiofit.Modules.Registry", StringComparison.Ordinal) == true);
        Assert.DoesNotContain(exposed, type => type.Namespace?.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) == true);
    }

    [Fact]
    public void ApiHost_DoesNotContainPatientWorkflowOrDbContext()
    {
        var program = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "backend", "Fisiofit.Api", "Program.cs"));

        Assert.DoesNotContain("RegisterPatient", program, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", program, StringComparison.Ordinal);
        Assert.DoesNotContain("Patients.Application", program, StringComparison.Ordinal);
    }

    private static string[] ContextSources(string context) => Directory
        .EnumerateFiles(Path.Combine(RegistryRoot(), context), "*.cs", SearchOption.AllDirectories)
        .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        .Select(File.ReadAllText)
        .ToArray();

    private static void AssertMigrationScope(
        string context,
        string migrationFolder,
        string ownerSchema,
        IReadOnlyCollection<string> forbiddenSchemas)
    {
        var path = Path.Combine(RegistryRoot(), context, "Infrastructure", "Migrations", migrationFolder);
        var migrationFiles = Directory.EnumerateFiles(path, "*.cs", SearchOption.TopDirectoryOnly)
            .Where(file => !file.EndsWith("Designer.cs", StringComparison.Ordinal)
                && !file.EndsWith("ModelSnapshot.cs", StringComparison.Ordinal))
            .ToArray();

        Assert.Single(migrationFiles);
        var source = File.ReadAllText(migrationFiles[0]);
        Assert.Contains($"schema: \"{ownerSchema}\"", source, StringComparison.Ordinal);
        Assert.All(forbiddenSchemas, forbidden =>
            Assert.DoesNotContain($"schema: \"{forbidden}\"", source, StringComparison.Ordinal));
    }

    private static IEnumerable<Type> Flatten(Type type)
    {
        yield return type;
        foreach (var genericArgument in type.GetGenericArguments())
        {
            foreach (var nested in Flatten(genericArgument))
            {
                yield return nested;
            }
        }
    }

    private static string RegistryRoot() => Path.Combine(
        RepositoryRoot(),
        "src",
        "backend",
        "Modules",
        "Fisiofit.Modules.Registry");

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Fisiofit.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
