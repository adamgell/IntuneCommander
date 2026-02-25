using Intune.Commander.Core.Services;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Core.Tests.Services;

public class DriverUpdateProfileServiceTests
{
    [Fact]
    public void Service_ImplementsInterface()
    {
        Assert.True(typeof(IDriverUpdateProfileService).IsAssignableFrom(typeof(DriverUpdateProfileService)));
    }

    [Fact]
    public void Service_HasGraphClientConstructor()
    {
        var constructor = typeof(DriverUpdateProfileService).GetConstructor([typeof(GraphServiceClient)]);
        Assert.NotNull(constructor);
    }

    [Fact]
    public void Interface_DefinesListMethod()
    {
        var method = typeof(IDriverUpdateProfileService).GetMethod("ListDriverUpdateProfilesAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<List<WindowsDriverUpdateProfile>>), method.ReturnType);
    }

    [Fact]
    public void Interface_DefinesGetMethod()
    {
        var method = typeof(IDriverUpdateProfileService).GetMethod("GetDriverUpdateProfileAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<WindowsDriverUpdateProfile?>), method.ReturnType);
    }

    [Fact]
    public void Interface_DefinesCreateMethod()
    {
        var method = typeof(IDriverUpdateProfileService).GetMethod("CreateDriverUpdateProfileAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<WindowsDriverUpdateProfile>), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(typeof(WindowsDriverUpdateProfile), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
    }

    [Fact]
    public void Interface_DefinesUpdateMethod()
    {
        var method = typeof(IDriverUpdateProfileService).GetMethod("UpdateDriverUpdateProfileAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<WindowsDriverUpdateProfile>), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(typeof(WindowsDriverUpdateProfile), parameters[0].ParameterType);
    }

    [Fact]
    public void Interface_DefinesDeleteMethod()
    {
        var method = typeof(IDriverUpdateProfileService).GetMethod("DeleteDriverUpdateProfileAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(typeof(string), parameters[0].ParameterType);
    }

    [Fact]
    public void Interface_AllMethodsAcceptCancellationToken()
    {
        var methods = typeof(IDriverUpdateProfileService).GetMethods();
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
        var methods = typeof(IDriverUpdateProfileService).GetMethods();
        Assert.Equal(5, methods.Length);
    }

    [Fact]
    public void ListMethod_HasCancellationTokenWithDefault()
    {
        var method = typeof(IDriverUpdateProfileService).GetMethod("ListDriverUpdateProfilesAsync");
        Assert.NotNull(method);
        var parameters = method.GetParameters();
        Assert.Single(parameters);
        Assert.Equal(typeof(CancellationToken), parameters[0].ParameterType);
        Assert.True(parameters[0].HasDefaultValue);
    }

    [Fact]
    public void GetMethod_AcceptsStringId()
    {
        var method = typeof(IDriverUpdateProfileService).GetMethod("GetDriverUpdateProfileAsync");
        Assert.NotNull(method);
        var parameters = method.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
    }

    [Fact]
    public void DeleteMethod_AcceptsStringId()
    {
        var method = typeof(IDriverUpdateProfileService).GetMethod("DeleteDriverUpdateProfileAsync");
        Assert.NotNull(method);
        var parameters = method.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
    }

    [Fact]
    public void Service_HasPrivateGraphClientField()
    {
        var field = typeof(DriverUpdateProfileService).GetField("_graphClient",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(field);
        Assert.Equal(typeof(GraphServiceClient), field.FieldType);
    }
}
