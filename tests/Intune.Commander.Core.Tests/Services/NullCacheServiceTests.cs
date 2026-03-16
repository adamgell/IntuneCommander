using Intune.Commander.Core.Services;

namespace Intune.Commander.Core.Tests.Services;

public class NullCacheServiceTests
{
    private readonly NullCacheService _sut = new();

    [Fact]
    public void IsAvailable_ReturnsFalse()
    {
        Assert.False(_sut.IsAvailable);
    }

    [Fact]
    public void Get_AlwaysReturnsCacheMiss()
    {
        Assert.Null(_sut.Get<string>("tenant", "type"));
    }

    [Fact]
    public void Set_DoesNotThrow()
    {
        _sut.Set("tenant", "type", new List<string> { "a", "b" });
    }

    [Fact]
    public void Invalidate_DoesNotThrow()
    {
        _sut.Invalidate("tenant");
        _sut.Invalidate("tenant", "type");
    }

    [Fact]
    public void CleanupExpired_ReturnsZero()
    {
        Assert.Equal(0, _sut.CleanupExpired());
    }

    [Fact]
    public void GetMetadata_ReturnsNull()
    {
        Assert.Null(_sut.GetMetadata("tenant", "type"));
    }

    [Fact]
    public async Task GetAsync_AlwaysReturnsCacheMiss()
    {
        Assert.Null(await _sut.GetAsync<string>("tenant", "type"));
    }

    [Fact]
    public async Task SetAsync_DoesNotThrow()
    {
        await _sut.SetAsync("tenant", "type", new List<string> { "a" });
    }

    [Fact]
    public async Task InvalidateAsync_DoesNotThrow()
    {
        await _sut.InvalidateAsync("tenant");
    }

    [Fact]
    public async Task GetMetadataAsync_ReturnsNull()
    {
        Assert.Null(await _sut.GetMetadataAsync("tenant", "type"));
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        using var sut = new NullCacheService();
    }
}
