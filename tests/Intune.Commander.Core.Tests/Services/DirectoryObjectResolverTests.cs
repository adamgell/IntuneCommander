using Intune.Commander.Core.Services;

namespace Intune.Commander.Core.Tests.Services;

public class DirectoryObjectResolverContractTests
{
    [Fact]
    public void Interface_HasResolveAsyncMethod()
    {
        var method = typeof(IDirectoryObjectResolver).GetMethod("ResolveAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<IReadOnlyDictionary<string, string>>), method.ReturnType);
    }

    [Fact]
    public void ResolveAsyncMethod_AcceptsIdsAndCancellationToken()
    {
        var method = typeof(IDirectoryObjectResolver).GetMethod("ResolveAsync");
        Assert.NotNull(method);
        var parameters = method.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(IEnumerable<string>), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
    }

    [Fact]
    public void DirectoryObjectResolver_ImplementsInterface()
    {
        Assert.True(typeof(IDirectoryObjectResolver).IsAssignableFrom(typeof(DirectoryObjectResolver)));
    }

    [Fact]
    public void Constructor_ThrowsOnNullGraphClient()
    {
        Assert.Throws<ArgumentNullException>(() => new DirectoryObjectResolver(null!));
    }
}
