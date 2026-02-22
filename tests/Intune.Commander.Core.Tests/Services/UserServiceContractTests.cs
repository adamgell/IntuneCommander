using Intune.Commander.Core.Services;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Core.Tests.Services;

public class UserServiceContractTests
{
    [Fact]
    public void IUserService_ShouldExpose_ListUsersAsync_WithCancellationToken()
    {
        var method = typeof(IUserService).GetMethod(nameof(IUserService.ListUsersAsync));

        Assert.NotNull(method);
        Assert.Equal(typeof(Task<List<User>>), method!.ReturnType);

        var parameters = method.GetParameters();
        Assert.Single(parameters);
        Assert.Equal(typeof(CancellationToken), parameters[0].ParameterType);
    }

    [Fact]
    public void UserService_ShouldImplement_IUserService_ListUsersAsync()
    {
        var interfaceMethod = typeof(IUserService).GetMethod(nameof(IUserService.ListUsersAsync));
        var implementationMethod = typeof(UserService).GetMethod(nameof(IUserService.ListUsersAsync));

        Assert.NotNull(interfaceMethod);
        Assert.NotNull(implementationMethod);
        Assert.Equal(interfaceMethod!.ReturnType, implementationMethod!.ReturnType);

        var parameters = implementationMethod.GetParameters();
        Assert.Single(parameters);
        Assert.Equal(typeof(CancellationToken), parameters[0].ParameterType);
    }
}