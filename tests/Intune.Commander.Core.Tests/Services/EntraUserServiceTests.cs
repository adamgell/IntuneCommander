using Intune.Commander.Core.Services;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Core.Tests.Services;

public class EntraUserServiceTests
{
    [Fact]
    public void Service_ImplementsInterface()
    {
        Assert.True(typeof(IEntraUserService).IsAssignableFrom(typeof(EntraUserService)));
    }

    [Fact]
    public void Service_HasGraphClientConstructor()
    {
        var constructor = typeof(EntraUserService).GetConstructor([typeof(GraphServiceClient)]);
        Assert.NotNull(constructor);
    }

    [Fact]
    public void Interface_DefinesListUsersMethod()
    {
        var method = typeof(IEntraUserService).GetMethod("ListUsersAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<List<User>>), method.ReturnType);
        var parameters = method.GetParameters();
        Assert.Single(parameters);
        Assert.Equal(typeof(CancellationToken), parameters[0].ParameterType);
        Assert.True(parameters[0].HasDefaultValue);
    }

    [Fact]
    public void Interface_HasOneMethod()
    {
        var methods = typeof(IEntraUserService).GetMethods();
        Assert.Single(methods);
    }
}
