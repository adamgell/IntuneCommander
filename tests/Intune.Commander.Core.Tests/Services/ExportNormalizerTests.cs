using Intune.Commander.Core.Services;

namespace Intune.Commander.Core.Tests.Services;

public class ExportNormalizerTests
{
    private readonly ExportNormalizer _sut = new();

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
}
