using Intune.Commander.Core.Models;
using Intune.Commander.Core.Services;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Core.Tests.Services;

public class BaselineServiceContractTests
{
    [Fact]
    public void BaselineService_ImplementsIBaselineService()
    {
        Assert.True(typeof(IBaselineService).IsAssignableFrom(typeof(BaselineService)));
    }

    [Fact]
    public void Interface_DefinesGetAllBaselines()
    {
        var method = typeof(IBaselineService).GetMethod("GetAllBaselines");
        Assert.NotNull(method);
        Assert.Equal(typeof(IReadOnlyList<BaselinePolicy>), method.ReturnType);
        Assert.Empty(method.GetParameters());
    }

    [Fact]
    public void Interface_DefinesGetBaselinesByType()
    {
        var method = typeof(IBaselineService).GetMethod("GetBaselinesByType");
        Assert.NotNull(method);
        Assert.Equal(typeof(IReadOnlyList<BaselinePolicy>), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Single(parameters);
        Assert.Equal(typeof(BaselinePolicyType), parameters[0].ParameterType);
    }

    [Fact]
    public void Interface_DefinesGetCategories()
    {
        var method = typeof(IBaselineService).GetMethod("GetCategories");
        Assert.NotNull(method);
        Assert.Equal(typeof(IReadOnlyList<string>), method.ReturnType);
    }

    [Fact]
    public void Interface_DefinesGetBaselinesByCategory()
    {
        var method = typeof(IBaselineService).GetMethod("GetBaselinesByCategory");
        Assert.NotNull(method);
        Assert.Equal(typeof(IReadOnlyList<BaselinePolicy>), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(typeof(string), parameters[0].ParameterType);
    }

    [Fact]
    public void Interface_DefinesCompareSettingsCatalog()
    {
        var method = typeof(IBaselineService).GetMethod("CompareSettingsCatalog");
        Assert.NotNull(method);
        Assert.Equal(typeof(BaselineComparisonResult), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(typeof(BaselinePolicy), parameters[0].ParameterType);
        Assert.Equal(typeof(IReadOnlyList<DeviceManagementConfigurationSetting>), parameters[1].ParameterType);
    }

    [Fact]
    public void BaselineService_HasParameterlessConstructor()
    {
        var ctor = typeof(BaselineService).GetConstructor(Type.EmptyTypes);
        Assert.NotNull(ctor);
    }
}
