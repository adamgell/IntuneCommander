using Intune.Commander.Core.Services;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Core.Tests.Services;

public class CloudPcUserSettingsServiceTests
{
    [Fact]
    public void Service_ImplementsInterface()
    {
        Assert.True(typeof(ICloudPcUserSettingsService).IsAssignableFrom(typeof(CloudPcUserSettingsService)));
    }

    [Fact]
    public void Service_HasGraphClientConstructor()
    {
        var constructor = typeof(CloudPcUserSettingsService).GetConstructor([typeof(GraphServiceClient)]);
        Assert.NotNull(constructor);
    }

    [Fact]
    public void Interface_DefinesListMethod()
    {
        var method = typeof(ICloudPcUserSettingsService).GetMethod("ListUserSettingsAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<List<CloudPcUserSetting>>), method.ReturnType);
    }

    [Fact]
    public void Interface_DefinesGetMethod()
    {
        var method = typeof(ICloudPcUserSettingsService).GetMethod("GetUserSettingAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<CloudPcUserSetting?>), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(typeof(string), parameters[0].ParameterType);
    }

    [Fact]
    public void Interface_AllMethodsAcceptCancellationToken()
    {
        var methods = typeof(ICloudPcUserSettingsService).GetMethods();
        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            var lastParam = parameters[^1];
            Assert.Equal(typeof(CancellationToken), lastParam.ParameterType);
            Assert.True(lastParam.HasDefaultValue);
        }
    }

    [Fact]
    public void Interface_HasTwoMethods()
    {
        var methods = typeof(ICloudPcUserSettingsService).GetMethods();
        Assert.Equal(2, methods.Length);
    }
}
