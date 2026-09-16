using System.Reflection;
using System.Xml.Linq;
using Xunit;

namespace Fisiofit.ArchitectureTests;

public sealed class AssemblyDependencyTests
{
    private static readonly string[] ModuleAssemblyNames =
    [
        "Fisiofit.Modules.Access",
        "Fisiofit.Modules.Registry",
        "Fisiofit.Modules.Crm",
        "Fisiofit.Modules.Operations",
        "Fisiofit.Modules.Clinical",
        "Fisiofit.Modules.Revenue",
        "Fisiofit.Modules.Communication",
        "Fisiofit.Modules.Documents",
        "Fisiofit.Modules.Audit",
        "Fisiofit.Modules.Reports"
    ];

    [Fact]
    public void Baseline_ContainsExactlyTheApprovedModuleAssemblies()
    {
        var moduleProjects = Directory
            .EnumerateFiles(Path.Combine(RepositoryRoot(), "src", "backend", "Modules"), "*.csproj", SearchOption.AllDirectories)
            .Select(Path.GetFileNameWithoutExtension)
            .OfType<string>()
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(ModuleAssemblyNames.Order(StringComparer.Ordinal), moduleProjects);
        Assert.All(moduleProjects, name => Assert.NotNull(Assembly.Load(name)));
    }

    [Fact]
    public void ModuleAssemblies_DoNotReferenceOtherModuleAssemblies()
    {
        foreach (var projectPath in ModuleProjectPaths())
        {
            var forbiddenReferences = ProjectReferences(projectPath)
                .Where(reference => reference.Contains("Fisiofit.Modules.", StringComparison.Ordinal))
                .ToArray();

            Assert.Empty(forbiddenReferences);
        }
    }

    [Fact]
    public void SharedAssemblies_DoNotReferenceModuleAssemblies()
    {
        var sharedProjects = new[]
        {
            Path.Combine(RepositoryRoot(), "src", "backend", "Fisiofit.BuildingBlocks", "Fisiofit.BuildingBlocks.csproj"),
            Path.Combine(RepositoryRoot(), "src", "backend", "Fisiofit.ModuleContracts", "Fisiofit.ModuleContracts.csproj")
        };

        foreach (var projectPath in sharedProjects)
        {
            Assert.DoesNotContain(
                ProjectReferences(projectPath),
                reference => reference.Contains("Fisiofit.Modules.", StringComparison.Ordinal));
        }
    }

    private static string[] ModuleProjectPaths() => Directory
        .EnumerateFiles(Path.Combine(RepositoryRoot(), "src", "backend", "Modules"), "*.csproj", SearchOption.AllDirectories)
        .ToArray();

    private static IEnumerable<string> ProjectReferences(string projectPath) => XDocument
        .Load(projectPath)
        .Descendants("ProjectReference")
        .Select(reference => (string?)reference.Attribute("Include"))
        .OfType<string>();

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
