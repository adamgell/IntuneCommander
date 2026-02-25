using Intune.Commander.Core.Services;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Core.Tests.Services;

public class DeviceManagementScriptServiceTests
{
    [Fact]
    public void Service_ImplementsInterface()
    {
        Assert.True(typeof(IDeviceManagementScriptService).IsAssignableFrom(typeof(DeviceManagementScriptService)));
    }

    [Fact]
    public void Service_HasGraphClientConstructor()
    {
        var constructor = typeof(DeviceManagementScriptService).GetConstructor([typeof(GraphServiceClient)]);
        Assert.NotNull(constructor);
    }

    [Fact]
    public void Interface_DefinesListMethod()
    {
        var method = typeof(IDeviceManagementScriptService).GetMethod("ListDeviceManagementScriptsAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<List<DeviceManagementScript>>), method.ReturnType);
    }

    [Fact]
    public void Interface_DefinesGetMethod()
    {
        var method = typeof(IDeviceManagementScriptService).GetMethod("GetDeviceManagementScriptAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<DeviceManagementScript?>), method.ReturnType);
    }

    [Fact]
    public void Interface_DefinesCreateMethod()
    {
        var method = typeof(IDeviceManagementScriptService).GetMethod("CreateDeviceManagementScriptAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<DeviceManagementScript>), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(typeof(DeviceManagementScript), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
    }

    [Fact]
    public void Interface_DefinesUpdateMethod()
    {
        var method = typeof(IDeviceManagementScriptService).GetMethod("UpdateDeviceManagementScriptAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<DeviceManagementScript>), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(typeof(DeviceManagementScript), parameters[0].ParameterType);
    }

    [Fact]
    public void Interface_DefinesDeleteMethod()
    {
        var method = typeof(IDeviceManagementScriptService).GetMethod("DeleteDeviceManagementScriptAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(typeof(string), parameters[0].ParameterType);
    }

    [Fact]
    public void Interface_DefinesGetAssignmentsMethod()
    {
        var method = typeof(IDeviceManagementScriptService).GetMethod("GetAssignmentsAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<List<DeviceManagementScriptAssignment>>), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
    }

    [Fact]
    public void Interface_DefinesAssignScriptMethod()
    {
        var method = typeof(IDeviceManagementScriptService).GetMethod("AssignScriptAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal(typeof(List<DeviceManagementScriptAssignment>), parameters[1].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[2].ParameterType);
    }

    [Fact]
    public void Interface_HasSevenMethods()
    {
        var methods = typeof(IDeviceManagementScriptService).GetMethods();
        Assert.Equal(7, methods.Length);
    }

    [Fact]
    public void Interface_AllMethodsAcceptCancellationToken()
    {
        var methods = typeof(IDeviceManagementScriptService).GetMethods();
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
        var method = typeof(IDeviceManagementScriptService).GetMethod("ListDeviceManagementScriptsAsync");
        Assert.NotNull(method);
        var parameters = method.GetParameters();
        Assert.Single(parameters);
        Assert.Equal(typeof(CancellationToken), parameters[0].ParameterType);
        Assert.True(parameters[0].HasDefaultValue);
    }

    [Fact]
    public void GetMethod_AcceptsStringId()
    {
        var method = typeof(IDeviceManagementScriptService).GetMethod("GetDeviceManagementScriptAsync");
        Assert.NotNull(method);
        var parameters = method.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
    }

    [Fact]
    public void DeleteMethod_AcceptsStringId()
    {
        var method = typeof(IDeviceManagementScriptService).GetMethod("DeleteDeviceManagementScriptAsync");
        Assert.NotNull(method);
        var parameters = method.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
    }

    [Fact]
    public void GetAssignmentsMethod_AcceptsStringId()
    {
        var method = typeof(IDeviceManagementScriptService).GetMethod("GetAssignmentsAsync");
        Assert.NotNull(method);
        var parameters = method.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
    }

    [Fact]
    public void Service_HasPrivateGraphClientField()
    {
        var field = typeof(DeviceManagementScriptService).GetField("_graphClient",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(field);
        Assert.Equal(typeof(GraphServiceClient), field.FieldType);
    }
}
