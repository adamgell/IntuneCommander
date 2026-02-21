using Intune.Commander.Core.Services;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Core.Tests.Services;

public class ComplianceScriptServiceTests
{
    [Fact]
    public void Service_ImplementsInterface()
    {
        Assert.True(typeof(IComplianceScriptService).IsAssignableFrom(typeof(ComplianceScriptService)));
    }

    [Fact]
    public void Service_HasGraphClientConstructor()
    {
        var constructor = typeof(ComplianceScriptService).GetConstructor([typeof(GraphServiceClient)]);
        Assert.NotNull(constructor);
    }

    [Fact]
    public void Interface_DefinesListMethod()
    {
        var method = typeof(IComplianceScriptService).GetMethod("ListComplianceScriptsAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<List<DeviceComplianceScript>>), method.ReturnType);
    }

    [Fact]
    public void Interface_DefinesGetMethod()
    {
        var method = typeof(IComplianceScriptService).GetMethod("GetComplianceScriptAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<DeviceComplianceScript?>), method.ReturnType);
    }

    [Fact]
    public void Interface_DefinesCreateMethod()
    {
        var method = typeof(IComplianceScriptService).GetMethod("CreateComplianceScriptAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<DeviceComplianceScript>), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(typeof(DeviceComplianceScript), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
    }

    [Fact]
    public void Interface_DefinesUpdateMethod()
    {
        var method = typeof(IComplianceScriptService).GetMethod("UpdateComplianceScriptAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<DeviceComplianceScript>), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(typeof(DeviceComplianceScript), parameters[0].ParameterType);
    }

    [Fact]
    public void Interface_DefinesDeleteMethod()
    {
        var method = typeof(IComplianceScriptService).GetMethod("DeleteComplianceScriptAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(typeof(string), parameters[0].ParameterType);
    }

    [Fact]
    public void Interface_HasFiveMethods()
    {
        // ComplianceScriptService has no GetAssignmentsAsync — only 5 CRUD methods
        var methods = typeof(IComplianceScriptService).GetMethods();
        Assert.Equal(5, methods.Length);
    }

    [Fact]
    public void Interface_AllMethodsAcceptCancellationToken()
    {
        var methods = typeof(IComplianceScriptService).GetMethods();
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
        var method = typeof(IComplianceScriptService).GetMethod("ListComplianceScriptsAsync");
        Assert.NotNull(method);
        var parameters = method.GetParameters();
        Assert.Single(parameters);
        Assert.Equal(typeof(CancellationToken), parameters[0].ParameterType);
        Assert.True(parameters[0].HasDefaultValue);
    }

    [Fact]
    public void GetMethod_AcceptsStringId()
    {
        var method = typeof(IComplianceScriptService).GetMethod("GetComplianceScriptAsync");
        Assert.NotNull(method);
        var parameters = method.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
    }

    [Fact]
    public void DeleteMethod_AcceptsStringId()
    {
        var method = typeof(IComplianceScriptService).GetMethod("DeleteComplianceScriptAsync");
        Assert.NotNull(method);
        var parameters = method.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
    }

    [Fact]
    public void Interface_DoesNotHaveGetAssignmentsMethod()
    {
        // DeviceComplianceScript does not support assignments
        var method = typeof(IComplianceScriptService).GetMethod("GetAssignmentsAsync");
        Assert.Null(method);
    }

    [Fact]
    public void Service_HasPrivateGraphClientField()
    {
        var field = typeof(ComplianceScriptService).GetField("_graphClient",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(field);
        Assert.Equal(typeof(GraphServiceClient), field.FieldType);
    }
}
