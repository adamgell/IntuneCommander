using System.Text.Json;
using Intune.Commander.Core.Services;

namespace Intune.Commander.Core.Tests.Services;

public class ExportNormalizerTests : IDisposable
{
    private readonly ExportNormalizer _sut = new();
    private readonly string _tempDir;

    public ExportNormalizerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"IntuneCommander_Norm_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }
        catch { }
    }

    [Fact]
    public void NormalizeJson_StripsVolatileFieldsAndSortsCollections()
    {
        var left = """
                   {
                     "displayName": "Policy A",
                     "id": "left-id",
                     "version": 1,
                     "assignments": [
                       { "targetGroupId": "b" },
                       { "targetGroupId": "a" }
                     ],
                     "settings": { "passwordMinimumLength": 12 },
                     "lastModifiedDateTime": "2026-03-01T00:00:00Z"
                   }
                   """;

        var right = """
                    {
                      "settings": { "passwordMinimumLength": 12 },
                      "displayName": "Policy A",
                      "id": "right-id",
                      "assignments": [
                        { "targetGroupId": "a" },
                        { "targetGroupId": "b" }
                      ],
                      "createdDateTime": "2026-03-02T00:00:00Z",
                      "version": 9
                    }
                    """;

        var normalizedLeft = _sut.NormalizeJson(left);
        var normalizedRight = _sut.NormalizeJson(right);

        Assert.Equal(normalizedLeft, normalizedRight);
        Assert.DoesNotContain("\"id\":", normalizedLeft);
        Assert.DoesNotContain("\"version\":", normalizedLeft);
        Assert.DoesNotContain("lastModifiedDateTime", normalizedLeft);
    }

    [Fact]
    public void NormalizeJson_PreservesNonVolatileFields()
    {
        var json = """{ "displayName": "Test", "passwordMinimumLength": 12, "isEnabled": true }""";

        var result = _sut.NormalizeJson(json);

        Assert.Contains("displayName", result);
        Assert.Contains("passwordMinimumLength", result);
        Assert.Contains("isEnabled", result);
    }

    [Fact]
    public void NormalizeJson_SortsKeysAlphabetically()
    {
        var json = """{ "zebra": 1, "alpha": 2, "middle": 3 }""";

        var result = _sut.NormalizeJson(json);

        var alphaIndex = result.IndexOf("alpha", StringComparison.Ordinal);
        var middleIndex = result.IndexOf("middle", StringComparison.Ordinal);
        var zebraIndex = result.IndexOf("zebra", StringComparison.Ordinal);

        Assert.True(alphaIndex < middleIndex);
        Assert.True(middleIndex < zebraIndex);
    }

    [Fact]
    public void NormalizeJson_NestedObjects_VolatileFieldsStrippedRecursively()
    {
        var json = """
                   {
                     "displayName": "Test",
                     "nested": {
                       "id": "should-be-removed",
                       "value": 42
                     }
                   }
                   """;

        var result = _sut.NormalizeJson(json);

        // Nested "id" is stripped because the normalizer strips by key name
        Assert.DoesNotContain("\"id\":", result);
        Assert.Contains("value", result);
        Assert.Contains("displayName", result);
    }

    [Fact]
    public void NormalizeJson_EmptyObject_ReturnsEmptyObject()
    {
        var result = _sut.NormalizeJson("{}");

        // Should parse without error and return valid JSON
        var parsed = JsonDocument.Parse(result);
        Assert.Equal(JsonValueKind.Object, parsed.RootElement.ValueKind);
    }

    [Fact]
    public void NormalizeJson_InvalidJson_ThrowsJsonException()
    {
        Assert.ThrowsAny<JsonException>(() => _sut.NormalizeJson("not valid json"));
    }

    [Fact]
    public async Task NormalizeDirectoryAsync_NormalizesAllJsonFiles()
    {
        var subDir = Path.Combine(_tempDir, "CompliancePolicies");
        Directory.CreateDirectory(subDir);

        File.WriteAllText(Path.Combine(subDir, "PolicyA.json"),
            """{ "id": "abc", "displayName": "A", "version": 1 }""");
        File.WriteAllText(Path.Combine(subDir, "PolicyB.json"),
            """{ "version": 2, "displayName": "B", "id": "def" }""");

        await _sut.NormalizeDirectoryAsync(_tempDir);

        var contentA = await File.ReadAllTextAsync(Path.Combine(subDir, "PolicyA.json"));
        var contentB = await File.ReadAllTextAsync(Path.Combine(subDir, "PolicyB.json"));

        Assert.DoesNotContain("\"id\":", contentA);
        Assert.DoesNotContain("\"version\":", contentA);
        Assert.DoesNotContain("\"id\":", contentB);
        Assert.DoesNotContain("\"version\":", contentB);
    }

    [Fact]
    public async Task NormalizeDirectoryAsync_InvalidPath_ThrowsDirectoryNotFound()
    {
        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => _sut.NormalizeDirectoryAsync("/nonexistent/path"));
    }

    [Fact]
    public void NormalizeJson_ArrayWithNullElements_HandledGracefully()
    {
        var json = """{ "items": [null, { "name": "a" }, null] }""";

        var result = _sut.NormalizeJson(json);

        // Should not throw — nulls are preserved
        Assert.Contains("items", result);
    }
}
