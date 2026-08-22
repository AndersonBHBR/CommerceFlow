using System.Reflection;
using NetArchTest.Rules;
using Xunit;

namespace CommerceFlow.ArchitectureTests;

public sealed class ArchitectureRulesTests
{
    [Fact]
    public void SalesDomain_MustRemainIndependent()
    {
        AssertNoDependency(
            typeof(Sales.Domain.AssemblyReference).Assembly,
            "Sales.Application",
            "Sales.Infrastructure",
            "Sales.Api",
            "Inventory",
            "Microsoft.EntityFrameworkCore");
    }

    [Fact]
    public void InventoryDomain_MustRemainIndependent()
    {
        AssertNoDependency(
            typeof(Inventory.Domain.AssemblyReference).Assembly,
            "Inventory.Application",
            "Inventory.Infrastructure",
            "Inventory.Api",
            "Sales",
            "Microsoft.EntityFrameworkCore");
    }

    [Fact]
    public void SalesAssemblies_MustNotDependOnInventory()
    {
        AssertNoDependency(typeof(Sales.Domain.AssemblyReference).Assembly, "Inventory");
        AssertNoDependency(typeof(Sales.Application.AssemblyReference).Assembly, "Inventory");
        AssertNoDependency(typeof(Sales.Infrastructure.AssemblyReference).Assembly, "Inventory");
    }

    [Fact]
    public void InventoryAssemblies_MustNotDependOnSales()
    {
        AssertNoDependency(typeof(Inventory.Domain.AssemblyReference).Assembly, "Sales");
        AssertNoDependency(typeof(Inventory.Application.AssemblyReference).Assembly, "Sales");
        AssertNoDependency(typeof(Inventory.Infrastructure.AssemblyReference).Assembly, "Sales");
    }

    [Fact]
    public void Contracts_MustNotDependOnServiceImplementations()
    {
        AssertNoDependency(
            typeof(CommerceFlow.Contracts.AssemblyReference).Assembly,
            "Sales",
            "Inventory",
            "CommerceFlow.Gateway",
            "CommerceFlow.Identity");
    }

    private static void AssertNoDependency(Assembly assembly, params string[] forbiddenNamespaces)
    {
        var result = Types
            .InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenNamespaces)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"Tipos com dependências proibidas: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}
