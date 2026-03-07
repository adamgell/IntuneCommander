using Intune.Commander.Core.Services;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.DirectoryObjects.GetByIds;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using NSubstitute;

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

    [Fact]
    public async Task ResolveAsync_WhenKiotaRequestFails_ReturnsUnresolvedResult()
    {
        var requestAdapter = Substitute.For<IRequestAdapter>();
        requestAdapter.BaseUrl.Returns("https://graph.microsoft.com/beta");
        requestAdapter.SerializationWriterFactory.Returns(Substitute.For<ISerializationWriterFactory>());
        requestAdapter.When(adapter => adapter.SendAsync(
                Arg.Any<RequestInformation>(),
                Arg.Any<ParsableFactory<GetByIdsPostResponse>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>(),
                Arg.Any<CancellationToken>())
            ).Do(_ => throw new ApiException("boom"));

        var graphClient = new GraphServiceClient(requestAdapter);
        var sut = new DirectoryObjectResolver(graphClient);

        var result = await sut.ResolveAsync([Guid.NewGuid().ToString()], CancellationToken.None);

        Assert.Empty(result);
    }
}
