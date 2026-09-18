using Fisiofit.ModuleContracts.Organization;
using Xunit;

namespace Fisiofit.ArchitectureTests;

public sealed class RegistryOrganizationBoundaryTests
{
    private static readonly string[] ProtectedSiblingContexts = ["Patients", "People", "Staff"];

    [Fact]
    public void RegistrySiblingContexts_DoNotDependOnOrganizationInfrastructure()
    {
        var registryRoot = Path.Combine(
            RepositoryRoot(),
            "src",
            "backend",
            "Modules",
            "Fisiofit.Modules.Registry");

        foreach (var context in ProtectedSiblingContexts)
        {
            var files = Directory.EnumerateFiles(
                Path.Combine(registryRoot, context),
                "*.cs",
                SearchOption.AllDirectories);

            foreach (var file in files)
            {
                var source = File.ReadAllText(file);
                Assert.DoesNotContain("Organization.Infrastructure", source, StringComparison.Ordinal);
                Assert.DoesNotContain("OrganizationDbContext", source, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void OrganizationContract_DoesNotExposePersistenceOrDomainTypes()
    {
        var contractType = typeof(IValidateUnitForPatientRegistration);
        var exposedTypes = contractType.GetMethods()
            .SelectMany(method =>
                method.GetParameters().Select(parameter => parameter.ParameterType)
                    .Append(method.ReturnType))
            .SelectMany(FlattenGenericTypes)
            .ToArray();

        Assert.DoesNotContain(
            exposedTypes,
            type => type.Namespace?.StartsWith("Fisiofit.Modules.Registry", StringComparison.Ordinal) == true);
        Assert.DoesNotContain(
            exposedTypes,
            type => type.Namespace?.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) == true);
    }

    private static IEnumerable<Type> FlattenGenericTypes(Type type)
    {
        yield return type;

        foreach (var argument in type.GetGenericArguments())
        {
            foreach (var nested in FlattenGenericTypes(argument))
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
