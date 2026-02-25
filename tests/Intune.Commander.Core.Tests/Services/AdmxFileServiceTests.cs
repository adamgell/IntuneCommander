using Intune.Commander.Core.Services;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Core.Tests.Services;

public class AdmxFileServiceTests
{
    [Fact]
    public void Service_ImplementsInterface()
    {
        Assert.True(typeof(IAdmxFileService).IsAssignableFrom(typeof(AdmxFileService)));
    }

    [Fact]
    public void Service_HasGraphClientConstructor()
    {
        var constructor = typeof(AdmxFileService).GetConstructor([typeof(GraphServiceClient)]);
        Assert.NotNull(constructor);
    }

    [Fact]
    public void Interface_DefinesListMethod()
    {
        var method = typeof(IAdmxFileService).GetMethod("ListAdmxFilesAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<List<GroupPolicyUploadedDefinitionFile>>), method.ReturnType);
    }

    [Fact]
    public void Interface_DefinesGetMethod()
    {
        var method = typeof(IAdmxFileService).GetMethod("GetAdmxFileAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<GroupPolicyUploadedDefinitionFile?>), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(typeof(string), parameters[0].ParameterType);
    }

    [Fact]
    public void Interface_DefinesCreateMethod()
    {
        var method = typeof(IAdmxFileService).GetMethod("CreateAdmxFileAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<GroupPolicyUploadedDefinitionFile>), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(typeof(GroupPolicyUploadedDefinitionFile), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
    }

    [Fact]
    public void Interface_DefinesDeleteMethod()
    {
        var method = typeof(IAdmxFileService).GetMethod("DeleteAdmxFileAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(typeof(string), parameters[0].ParameterType);
    }

    [Fact]
    public void Interface_AllMethodsAcceptCancellationToken()
    {
        var methods = typeof(IAdmxFileService).GetMethods();
        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            var lastParam = parameters[^1];
            Assert.Equal(typeof(CancellationToken), lastParam.ParameterType);
            Assert.True(lastParam.HasDefaultValue);
        }
    }

    [Fact]
    public void Interface_HasFourMethods()
    {
        var methods = typeof(IAdmxFileService).GetMethods();
        Assert.Equal(4, methods.Length);
    }
}
