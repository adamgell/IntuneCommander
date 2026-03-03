using Intune.Commander.Core.Models;
using Intune.Commander.Core.Services;

namespace Intune.Commander.Core.Tests.Services;

public class DriftDetectionServiceTests : IDisposable
{
    private readonly string _baselinePath;
    private readonly string _currentPath;
    private readonly DriftDetectionService _sut;

    public DriftDetectionServiceTests()
    {
        var root = Path.Combine(Path.GetTempPath(), $"IntuneCommander_Drift_{Guid.NewGuid():N}");
        _baselinePath = Path.Combine(root, "baseline");
        _currentPath = Path.Combine(root, "current");
        Directory.CreateDirectory(_baselinePath);
        Directory.CreateDirectory(_currentPath);

        _sut = new DriftDetectionService(new ExportNormalizer());
    }

    public void Dispose()
    {
        try
        {
            var root = Directory.GetParent(_baselinePath)?.FullName;
            if (!string.IsNullOrEmpty(root) && Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
        catch { }
    }

    [Fact]
    public async Task CompareAsync_NoDrift_ReturnsNoChanges()
    {
        WritePolicy(_baselinePath, "CompliancePolicies", "PolicyA.json", """
            { "displayName":"Policy A", "passwordMinimumLength":12, "id":"x1", "version":1 }
            """);
        WritePolicy(_currentPath, "CompliancePolicies", "PolicyA.json", """
            { "displayName":"Policy A", "passwordMinimumLength":12, "id":"x2", "version":2 }
            """);

        var report = await _sut.CompareAsync(_baselinePath, _currentPath);

        Assert.False(report.DriftDetected);
        Assert.Empty(report.Changes);
    }

    [Fact]
    public async Task CompareAsync_AddedPolicy_ClassifiedMedium()
    {
        WritePolicy(_currentPath, "CompliancePolicies", "PolicyA.json", """{ "displayName":"Policy A" }""");

        var report = await _sut.CompareAsync(_baselinePath, _currentPath);

        var change = Assert.Single(report.Changes);
        Assert.Equal("added", change.ChangeType);
        Assert.Equal(DriftSeverity.Medium, change.Severity);
    }

    [Fact]
    public async Task CompareAsync_DeletedPolicy_ClassifiedCritical()
    {
        WritePolicy(_baselinePath, "DeviceConfigurations", "BitLocker.json", """{ "displayName":"BitLocker Baseline" }""");

        var report = await _sut.CompareAsync(_baselinePath, _currentPath);

        var change = Assert.Single(report.Changes);
        Assert.Equal("deleted", change.ChangeType);
        Assert.Equal(DriftSeverity.Critical, change.Severity);
    }

    [Fact]
    public async Task CompareAsync_ModifiedSecuritySetting_ClassifiedCritical()
    {
        WritePolicy(_baselinePath, "CompliancePolicies", "Windows11.json", """{ "passwordMinimumLength": 12 }""");
        WritePolicy(_currentPath, "CompliancePolicies", "Windows11.json", """{ "passwordMinimumLength": 8 }""");

        var report = await _sut.CompareAsync(_baselinePath, _currentPath);

        var change = Assert.Single(report.Changes);
        Assert.Equal("modified", change.ChangeType);
        Assert.Equal(DriftSeverity.Critical, change.Severity);
        Assert.Contains(change.Fields, f => f.Path.Contains("passwordMinimumLength"));
    }

    [Fact]
    public async Task CompareAsync_AssignmentChange_ClassifiedHigh()
    {
        WritePolicy(_baselinePath, "Applications", "AppA.json", """
            { "assignments": [ { "groupId": "group-a" } ] }
            """);
        WritePolicy(_currentPath, "Applications", "AppA.json", """
            { "assignments": [ { "groupId": "group-b" } ] }
            """);

        var report = await _sut.CompareAsync(_baselinePath, _currentPath);

        var change = Assert.Single(report.Changes);
        Assert.Equal(DriftSeverity.High, change.Severity);
    }

    private static void WritePolicy(string root, string folder, string fileName, string json)
    {
        var path = Path.Combine(root, folder);
        Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path, fileName), json);
    }
}
