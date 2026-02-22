using Intune.Commander.Core.Services;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Core.Tests.Services;

public class SettingsCatalogServiceTests
{
    [Fact]
    public void Service_ImplementsInterface()
    {
        Assert.True(typeof(ISettingsCatalogService).IsAssignableFrom(typeof(SettingsCatalogService)));
    }

    [Fact]
    public void Service_HasGraphClientConstructor()
    {
        var constructor = typeof(SettingsCatalogService).GetConstructor([typeof(GraphServiceClient)]);
        Assert.NotNull(constructor);
    }

    [Fact]
    public void Interface_DefinesListMethod()
    {
        var method = typeof(ISettingsCatalogService).GetMethod("ListSettingsCatalogPoliciesAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<List<DeviceManagementConfigurationPolicy>>), method.ReturnType);
    }

    [Fact]
    public void Interface_DefinesGetMethod()
    {
        var method = typeof(ISettingsCatalogService).GetMethod("GetSettingsCatalogPolicyAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<DeviceManagementConfigurationPolicy?>), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(typeof(string), parameters[0].ParameterType);
    }

    [Fact]
    public void Interface_DefinesGetAssignmentsMethod()
    {
        var method = typeof(ISettingsCatalogService).GetMethod("GetAssignmentsAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<List<DeviceManagementConfigurationPolicyAssignment>>), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(typeof(string), parameters[0].ParameterType);
    }

    [Fact]
    public void Interface_DefinesGetPolicySettingsMethod()
    {
        var method = typeof(ISettingsCatalogService).GetMethod("GetPolicySettingsAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<List<DeviceManagementConfigurationSetting>>), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
        Assert.True(parameters[1].HasDefaultValue);
    }

    [Fact]
    public void Interface_DefinesCreateMethod()
    {
        var method = typeof(ISettingsCatalogService).GetMethod("CreateSettingsCatalogPolicyAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<DeviceManagementConfigurationPolicy>), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(typeof(DeviceManagementConfigurationPolicy), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
        Assert.True(parameters[1].HasDefaultValue);
    }

    [Fact]
    public void Interface_DefinesAssignMethod()
    {
        var method = typeof(ISettingsCatalogService).GetMethod("AssignSettingsCatalogPolicyAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal(typeof(List<DeviceManagementConfigurationPolicyAssignment>), parameters[1].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[2].ParameterType);
        Assert.True(parameters[2].HasDefaultValue);
    }

    [Fact]
    public void Interface_AllMethodsAcceptCancellationToken()
    {
        var methods = typeof(ISettingsCatalogService).GetMethods();
        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            var lastParam = parameters[^1];
            Assert.Equal(typeof(CancellationToken), lastParam.ParameterType);
            Assert.True(lastParam.HasDefaultValue);
        }
    }

    [Fact]
    public void Interface_HasSixMethods()
    {
        var methods = typeof(ISettingsCatalogService).GetMethods();
        Assert.Equal(6, methods.Length);
    }
}
