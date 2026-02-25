using Intune.Commander.Core.Services;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Core.Tests.Services;

public class ReusablePolicySettingServiceTests
{
    [Fact]
    public void Service_ImplementsInterface()
    {
        Assert.True(typeof(IReusablePolicySettingService).IsAssignableFrom(typeof(ReusablePolicySettingService)));
    }

    [Fact]
    public void Service_HasGraphClientConstructor()
    {
        var constructor = typeof(ReusablePolicySettingService).GetConstructor([typeof(GraphServiceClient)]);
        Assert.NotNull(constructor);
    }

    [Fact]
    public void Interface_DefinesListMethod()
    {
        var method = typeof(IReusablePolicySettingService).GetMethod("ListReusablePolicySettingsAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<List<DeviceManagementReusablePolicySetting>>), method.ReturnType);
    }

    [Fact]
    public void Interface_DefinesGetMethod()
    {
        var method = typeof(IReusablePolicySettingService).GetMethod("GetReusablePolicySettingAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<DeviceManagementReusablePolicySetting?>), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(typeof(string), parameters[0].ParameterType);
    }

    [Fact]
    public void Interface_DefinesCreateMethod()
    {
        var method = typeof(IReusablePolicySettingService).GetMethod("CreateReusablePolicySettingAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<DeviceManagementReusablePolicySetting>), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(typeof(DeviceManagementReusablePolicySetting), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
    }

    [Fact]
    public void Interface_DefinesUpdateMethod()
    {
        var method = typeof(IReusablePolicySettingService).GetMethod("UpdateReusablePolicySettingAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<DeviceManagementReusablePolicySetting>), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(typeof(DeviceManagementReusablePolicySetting), parameters[0].ParameterType);
    }

    [Fact]
    public void Interface_DefinesDeleteMethod()
    {
        var method = typeof(IReusablePolicySettingService).GetMethod("DeleteReusablePolicySettingAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(typeof(string), parameters[0].ParameterType);
    }

    [Fact]
    public void Interface_AllMethodsAcceptCancellationToken()
    {
        var methods = typeof(IReusablePolicySettingService).GetMethods();
        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            var lastParam = parameters[^1];
            Assert.Equal(typeof(CancellationToken), lastParam.ParameterType);
            Assert.True(lastParam.HasDefaultValue);
        }
    }

    [Fact]
    public void Interface_HasFiveMethods()
    {
        var methods = typeof(IReusablePolicySettingService).GetMethods();
        Assert.Equal(5, methods.Length);
    }
}
