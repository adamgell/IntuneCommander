using Intune.Commander.Core.Services;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Core.Tests.Services;

public class CloudPcProvisioningServiceTests
{
    [Fact]
    public void Service_ImplementsInterface()
    {
        Assert.True(typeof(ICloudPcProvisioningService).IsAssignableFrom(typeof(CloudPcProvisioningService)));
    }

    [Fact]
    public void Service_HasGraphClientConstructor()
    {
        var constructor = typeof(CloudPcProvisioningService).GetConstructor([typeof(GraphServiceClient)]);
        Assert.NotNull(constructor);
    }

    [Fact]
    public void Interface_DefinesListMethod()
    {
        var method = typeof(ICloudPcProvisioningService).GetMethod("ListProvisioningPoliciesAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<List<CloudPcProvisioningPolicy>>), method.ReturnType);
    }

    [Fact]
    public void Interface_DefinesGetMethod()
    {
        var method = typeof(ICloudPcProvisioningService).GetMethod("GetProvisioningPolicyAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<CloudPcProvisioningPolicy?>), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(typeof(string), parameters[0].ParameterType);
    }

    [Fact]
    public void Interface_AllMethodsAcceptCancellationToken()
    {
        var methods = typeof(ICloudPcProvisioningService).GetMethods();
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
        var methods = typeof(ICloudPcProvisioningService).GetMethods();
        Assert.Equal(2, methods.Length);
    }
}
