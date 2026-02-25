using Intune.Commander.Core.Services;

namespace Intune.Commander.Core.Tests.Services;

public class GraphPatchHelperTests
{
    [Fact]
    public async Task PatchWithGetFallbackAsync_NonNullPatchResult_ReturnsPatchResult()
    {
        var patchResult = new TestEntity { Name = "patched" };

        var result = await GraphPatchHelper.PatchWithGetFallbackAsync(
            patchResult,
            () => Task.FromResult<TestEntity?>(new TestEntity { Name = "fallback" }),
            "test entity");

        Assert.Same(patchResult, result);
        Assert.Equal("patched", result.Name);
    }

    [Fact]
    public async Task PatchWithGetFallbackAsync_NullPatchResult_ReturnsFallbackResult()
    {
        var fallback = new TestEntity { Name = "fallback" };

        var result = await GraphPatchHelper.PatchWithGetFallbackAsync<TestEntity>(
            null,
            () => Task.FromResult<TestEntity?>(fallback),
            "test entity");

        Assert.Same(fallback, result);
        Assert.Equal("fallback", result.Name);
    }

    [Fact]
    public async Task PatchWithGetFallbackAsync_NullPatchAndNullFallback_ThrowsInvalidOperationException()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => GraphPatchHelper.PatchWithGetFallbackAsync<TestEntity>(
                null,
                () => Task.FromResult<TestEntity?>(null),
                "my entity"));

        Assert.Contains("my entity", ex.Message);
    }

    [Fact]
    public async Task PatchWithGetFallbackAsync_FallbackNotCalledWhenPatchSucceeds()
    {
        var patchResult = new TestEntity { Name = "patched" };
        var fallbackCalled = false;

        await GraphPatchHelper.PatchWithGetFallbackAsync(
            patchResult,
            () => { fallbackCalled = true; return Task.FromResult<TestEntity?>(null); },
            "test entity");

        Assert.False(fallbackCalled);
    }

    private sealed class TestEntity
    {
        public string Name { get; set; } = "";
    }
}
