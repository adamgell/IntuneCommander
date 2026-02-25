using Intune.Commander.Core.Services;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Core.Tests.Services;

public class AppleDepServiceTests
{
    [Fact]
    public void Service_ImplementsInterface()
    {
        Assert.True(typeof(IAppleDepService).IsAssignableFrom(typeof(AppleDepService)));
    }

    [Fact]
    public void Service_HasGraphClientConstructor()
    {
        var constructor = typeof(AppleDepService).GetConstructor([typeof(GraphServiceClient)]);
        Assert.NotNull(constructor);
    }

    [Fact]
    public void Service_HasPrivateGraphClientField()
    {
        var field = typeof(AppleDepService).GetField("_graphClient",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(field);
        Assert.Equal(typeof(GraphServiceClient), field.FieldType);
    }

    [Fact]
    public void Interface_DefinesListDepOnboardingSettingsMethod()
    {
        var method = typeof(IAppleDepService).GetMethod("ListDepOnboardingSettingsAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<List<DepOnboardingSetting>>), method.ReturnType);
    }

    [Fact]
    public void Interface_DefinesGetDepOnboardingSettingMethod()
    {
        var method = typeof(IAppleDepService).GetMethod("GetDepOnboardingSettingAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<DepOnboardingSetting?>), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
    }

    [Fact]
    public void Interface_DefinesListImportedAppleDeviceIdentitiesMethod()
    {
        var method = typeof(IAppleDepService).GetMethod("ListImportedAppleDeviceIdentitiesAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<List<ImportedAppleDeviceIdentity>>), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
    }

    [Fact]
    public void Interface_HasThreeMethods()
    {
        var methods = typeof(IAppleDepService).GetMethods();
        Assert.Equal(3, methods.Length);
    }

    [Fact]
    public void Interface_AllMethodsAcceptCancellationToken()
    {
        var methods = typeof(IAppleDepService).GetMethods();
        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            var lastParam = parameters[^1];
            Assert.Equal(typeof(CancellationToken), lastParam.ParameterType);
            Assert.True(lastParam.HasDefaultValue);
        }
    }

    [Fact]
    public void ListMethod_HasCancellationTokenWithDefault()
    {
        var method = typeof(IAppleDepService).GetMethod("ListDepOnboardingSettingsAsync");
        Assert.NotNull(method);
        var parameters = method.GetParameters();
        Assert.Single(parameters);
        Assert.Equal(typeof(CancellationToken), parameters[0].ParameterType);
        Assert.True(parameters[0].HasDefaultValue);
    }
}
