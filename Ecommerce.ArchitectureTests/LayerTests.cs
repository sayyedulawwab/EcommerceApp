using System.Reflection;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using Architecture = ArchUnitNET.Domain.Architecture;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Ecommerce.ArchitectureTests;

// Project references already stop Domain and Application from using the outer layers.
// These tests also cover what references can't: the API project references Infrastructure
// so that Program.cs can register it, but controllers must still go through Application.
public class LayerTests
{
    // Loaded by name, so the tests also work while a new solution's Domain project has no types yet.
    private static readonly Assembly Domain = Assembly.Load("Ecommerce.Domain");
    private static readonly Assembly Application = typeof(Application.DependencyInjection).Assembly;
    private static readonly Assembly Infrastructure = typeof(Infrastructure.DependencyInjection).Assembly;
    private static readonly Assembly Api = typeof(Program).Assembly;

    private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(Domain, Application, Infrastructure, Api)
        .Build();

    [Fact]
    public void Domain_Should_NotDependOnOtherLayers()
    {
        Types().That().ResideInAssembly(Domain)
            .Should().NotDependOnAny(Types().That().ResideInAssembly(Application, Infrastructure, Api))
            .Check(Architecture);
    }

    [Fact]
    public void Application_Should_NotDependOnInfrastructureOrApi()
    {
        Types().That().ResideInAssembly(Application)
            .Should().NotDependOnAny(Types().That().ResideInAssembly(Infrastructure, Api))
            .Check(Architecture);
    }

    [Fact]
    public void Infrastructure_Should_NotDependOnApi()
    {
        Types().That().ResideInAssembly(Infrastructure)
            .Should().NotDependOnAny(Types().That().ResideInAssembly(Api))
            .Check(Architecture);
    }

    [Fact]
    public void Controllers_Should_NotDependOnInfrastructure()
    {
        Types().That().HaveFullNameStartingWith("Ecommerce.API.Controllers.")
            .Should().NotDependOnAny(Types().That().ResideInAssembly(Infrastructure))
            .Check(Architecture);
    }
}
