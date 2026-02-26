using Intune.Commander.Core.Services;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Core.Tests.Services;

public class ManagedDeviceServiceTests
{
    [Fact]
    public void Service_ImplementsInterface()
    {
        Assert.True(typeof(IManagedDeviceService).IsAssignableFrom(typeof(ManagedDeviceService)));
    }

    [Fact]
    public void Service_HasGraphClientConstructor()
    {
        var constructor = typeof(ManagedDeviceService).GetConstructor([typeof(GraphServiceClient)]);
        Assert.NotNull(constructor);
    }

    [Fact]
    public void Interface_DefinesListManagedDevicesMethod()
    {
        var method = typeof(IManagedDeviceService).GetMethod("ListManagedDevicesAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<List<ManagedDevice>>), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Single(parameters);
        Assert.Equal(typeof(CancellationToken), parameters[0].ParameterType);
        Assert.True(parameters[0].HasDefaultValue);
    }

    [Fact]
    public void Interface_HasOneMethod()
    {
        var methods = typeof(IManagedDeviceService).GetMethods();
        Assert.Single(methods);
    }
}
