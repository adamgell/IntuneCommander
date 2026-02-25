using Intune.Commander.Core.Services;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Core.Tests.Services;

public class VppTokenServiceTests
{
    [Fact]
    public void Service_ImplementsInterface()
    {
        Assert.True(typeof(IVppTokenService).IsAssignableFrom(typeof(VppTokenService)));
    }

    [Fact]
    public void Service_HasGraphClientConstructor()
    {
        var constructor = typeof(VppTokenService).GetConstructor([typeof(GraphServiceClient)]);
        Assert.NotNull(constructor);
    }

    [Fact]
    public void Interface_DefinesListMethod()
    {
        var method = typeof(IVppTokenService).GetMethod("ListVppTokensAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<List<VppToken>>), method.ReturnType);
    }

    [Fact]
    public void Interface_DefinesGetMethod()
    {
        var method = typeof(IVppTokenService).GetMethod("GetVppTokenAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<VppToken?>), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Equal(typeof(string), parameters[0].ParameterType);
    }

    [Fact]
    public void Interface_AllMethodsAcceptCancellationToken()
    {
        var methods = typeof(IVppTokenService).GetMethods();
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
        var methods = typeof(IVppTokenService).GetMethods();
        Assert.Equal(2, methods.Length);
    }

    [Fact]
    public void Interface_DoesNotHaveCreateMethod()
    {
        // VPP tokens are provisioned externally — no CRUD methods
        var createMethod = typeof(IVppTokenService).GetMethod("CreateVppTokenAsync");
        Assert.Null(createMethod);
    }

    [Fact]
    public void Interface_DoesNotHaveDeleteMethod()
    {
        var deleteMethod = typeof(IVppTokenService).GetMethod("DeleteVppTokenAsync");
        Assert.Null(deleteMethod);
    }
}
