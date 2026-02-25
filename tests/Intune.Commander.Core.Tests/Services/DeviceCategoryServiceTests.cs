using Intune.Commander.Core.Services;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Core.Tests.Services;

public class DeviceCategoryServiceTests
{
    [Fact]
    public void Service_ImplementsInterface()
    {
        Assert.True(typeof(IDeviceCategoryService).IsAssignableFrom(typeof(DeviceCategoryService)));
    }

    [Fact]
    public void Service_HasGraphClientConstructor()
    {
        var constructor = typeof(DeviceCategoryService).GetConstructor([typeof(GraphServiceClient)]);
        Assert.NotNull(constructor);
    }

    [Fact]
    public void Service_HasPrivateGraphClientField()
    {
        var field = typeof(DeviceCategoryService).GetField("_graphClient",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(field);
        Assert.Equal(typeof(GraphServiceClient), field.FieldType);
    }

    [Fact]
    public void Interface_DefinesListMethod()
    {
        var method = typeof(IDeviceCategoryService).GetMethod("ListDeviceCategoriesAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<List<DeviceCategory>>), method.ReturnType);
    }

    [Fact]
    public void Interface_DefinesGetMethod()
    {
        var method = typeof(IDeviceCategoryService).GetMethod("GetDeviceCategoryAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<DeviceCategory?>), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
    }

    [Fact]
    public void Interface_DefinesCreateMethod()
    {
        var method = typeof(IDeviceCategoryService).GetMethod("CreateDeviceCategoryAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<DeviceCategory>), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(DeviceCategory), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
    }

    [Fact]
    public void Interface_DefinesUpdateMethod()
    {
        var method = typeof(IDeviceCategoryService).GetMethod("UpdateDeviceCategoryAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<DeviceCategory>), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(DeviceCategory), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
    }

    [Fact]
    public void Interface_DefinesDeleteMethod()
    {
        var method = typeof(IDeviceCategoryService).GetMethod("DeleteDeviceCategoryAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
    }

    [Fact]
    public void Interface_HasFiveMethods()
    {
        var methods = typeof(IDeviceCategoryService).GetMethods();
        Assert.Equal(5, methods.Length);
    }

    [Fact]
    public void Interface_AllMethodsAcceptCancellationToken()
    {
        var methods = typeof(IDeviceCategoryService).GetMethods();
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
        var method = typeof(IDeviceCategoryService).GetMethod("ListDeviceCategoriesAsync");
        Assert.NotNull(method);
        var parameters = method.GetParameters();
        Assert.Single(parameters);
        Assert.Equal(typeof(CancellationToken), parameters[0].ParameterType);
        Assert.True(parameters[0].HasDefaultValue);
    }

    [Fact]
    public void DeleteMethod_AcceptsStringId()
    {
        var method = typeof(IDeviceCategoryService).GetMethod("DeleteDeviceCategoryAsync");
        Assert.NotNull(method);
        var parameters = method.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
    }
}
