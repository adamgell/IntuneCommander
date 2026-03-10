using Intune.Commander.Core.Services;

namespace Intune.Commander.Core.Tests.Services;

public sealed class ExportNormalizerDirectoryTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"ic-normalizer-{Guid.NewGuid():N}");

    public ExportNormalizerDirectoryTests()
    {
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public async Task NormalizeDirectoryAsync_IgnoresMigrationTable()
    {
        var sut = new ExportNormalizer();
        var migrationTablePath = Path.Combine(_tempDir, "migration-table.json");
        await File.WriteAllTextAsync(migrationTablePath, """{ "entries": [{ "originalId": "keep-me" }] }""");

        await sut.NormalizeDirectoryAsync(_tempDir);

        var json = await File.ReadAllTextAsync(migrationTablePath);
        Assert.Contains("keep-me", json);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }
}
