using Fisiofit.ModuleContracts.People;
using Xunit;

namespace Fisiofit.ArchitectureTests;

public sealed class RegistryPatientSearchBoundaryTests
{
    [Fact]
    public void SearchContract_DoesNotExposePersistenceQueryOrRegistryTypes()
    {
        var exposed = typeof(ISearchPeopleForPatientList)
            .GetMethods()
            .SelectMany(method => method.GetParameters().Select(parameter => parameter.ParameterType).Append(method.ReturnType))
            .SelectMany(Flatten)
            .ToArray();

        Assert.DoesNotContain(exposed, type => type.Namespace?.StartsWith("Fisiofit.Modules.Registry", StringComparison.Ordinal) == true);
        Assert.DoesNotContain(exposed, type => type.Namespace?.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) == true);
        Assert.DoesNotContain(exposed, type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IQueryable<>));
        Assert.DoesNotContain(exposed, type => typeof(Delegate).IsAssignableFrom(type));
    }

    [Fact]
    public void PatientSearch_PreservesOwnerBoundariesAndHostRemainsAdapterOnly()
    {
        var root = RepositoryRoot();
        var registry = Path.Combine(root, "src", "backend", "Modules", "Fisiofit.Modules.Registry");
        var patientSearch = File.ReadAllText(Path.Combine(registry, "Patients", "Application", "SearchPatients.cs"));
        var peopleSearch = File.ReadAllText(Path.Combine(registry, "People", "Application", "SearchPeopleForPatientList.cs"));
        var endpoint = File.ReadAllText(Path.Combine(registry, "Patients", "PatientsEndpoints.cs"));
        var host = File.ReadAllText(Path.Combine(root, "src", "backend", "Fisiofit.Api", "Program.cs"));

        Assert.DoesNotContain("PeopleDbContext", patientSearch, StringComparison.Ordinal);
        Assert.DoesNotContain("OrganizationDbContext", patientSearch, StringComparison.Ordinal);
        Assert.DoesNotContain("PatientProfile", peopleSearch, StringComparison.Ordinal);
        Assert.DoesNotContain("PatientsDbContext", peopleSearch, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", endpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("SearchPatients", host, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", host, StringComparison.Ordinal);
    }

    [Fact]
    public void Slice_AddsNoMigrationProjectionCacheOrReportingImplementation()
    {
        var root = RepositoryRoot();
        var registry = Path.Combine(root, "src", "backend", "Modules", "Fisiofit.Modules.Registry");
        var migrationFiles = Directory.EnumerateFiles(registry, "*.cs", SearchOption.AllDirectories)
            .Where(path => path.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => !path.EndsWith("Designer.cs", StringComparison.Ordinal)
                && !path.EndsWith("ModelSnapshot.cs", StringComparison.Ordinal))
            .ToArray();
        var searchSources = Directory.EnumerateFiles(registry, "*Search*.cs", SearchOption.AllDirectories)
            .Select(File.ReadAllText)
            .ToArray();

        Assert.Equal(3, migrationFiles.Length);
        Assert.All(searchSources, source =>
        {
            Assert.DoesNotContain("Redis", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Elasticsearch", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Reports", source, StringComparison.Ordinal);
            Assert.DoesNotContain("Projection", source, StringComparison.Ordinal);
        });
    }

    private static IEnumerable<Type> Flatten(Type type)
    {
        yield return type;
        foreach (var argument in type.GetGenericArguments())
        {
            foreach (var nested in Flatten(argument))
            {
                yield return nested;
            }
        }
    }

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
