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

    [Fact]
    public async Task CompareAsync_MinSeverityFilters_LowChangesExcluded()
    {
        WritePolicy(_baselinePath, "CompliancePolicies", "PolicyA.json", """{ "displayName": "Old Name" }""");
        WritePolicy(_currentPath, "CompliancePolicies", "PolicyA.json", """{ "displayName": "New Name" }""");

        var report = await _sut.CompareAsync(_baselinePath, _currentPath, DriftSeverity.High);

        Assert.False(report.DriftDetected);
        Assert.Empty(report.Changes);
    }

    [Fact]
    public async Task CompareAsync_ObjectTypeFilter_OnlyMatchingTypesIncluded()
    {
        WritePolicy(_baselinePath, "CompliancePolicies", "PolicyA.json", """{ "displayName": "Policy" }""");
        WritePolicy(_baselinePath, "DeviceConfigurations", "ConfigA.json", """{ "displayName": "Config" }""");
        // Both deleted — but filter to only CompliancePolicy

        var report = await _sut.CompareAsync(_baselinePath, _currentPath, objectTypes: ["CompliancePolicy"]);

        var change = Assert.Single(report.Changes);
        Assert.Equal("CompliancePolicy", change.ObjectType);
    }

    [Fact]
    public async Task CompareAsync_InvalidBaselinePath_ThrowsDirectoryNotFound()
    {
        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => _sut.CompareAsync("/nonexistent/path", _currentPath));
    }

    [Fact]
    public async Task CompareAsync_InvalidCurrentPath_ThrowsDirectoryNotFound()
    {
        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => _sut.CompareAsync(_baselinePath, "/nonexistent/path"));
    }

    [Fact]
    public async Task CompareAsync_MigrationTableIgnored()
    {
        // migration-table.json should be excluded from comparison
        WritePolicy(_baselinePath, ".", "migration-table.json", """{ "entries": [] }""");

        var report = await _sut.CompareAsync(_baselinePath, _currentPath);

        Assert.False(report.DriftDetected);
        Assert.Empty(report.Changes);
    }

    [Fact]
    public async Task CompareAsync_MultipleChanges_SummaryCountsCorrect()
    {
        // deleted => critical
        WritePolicy(_baselinePath, "DeviceConfigurations", "BitLocker.json", """{ "displayName": "BitLocker" }""");
        // added => medium
        WritePolicy(_currentPath, "CompliancePolicies", "NewPolicy.json", """{ "displayName": "New" }""");
        // modified displayName => low
        WritePolicy(_baselinePath, "Applications", "App.json", """{ "displayName": "Old" }""");
        WritePolicy(_currentPath, "Applications", "App.json", """{ "displayName": "New" }""");

        var report = await _sut.CompareAsync(_baselinePath, _currentPath);

        Assert.True(report.DriftDetected);
        Assert.Equal(3, report.Changes.Count);
        Assert.Equal(1, report.Summary.Critical);
        Assert.Equal(1, report.Summary.Medium);
        Assert.Equal(1, report.Summary.Low);
    }

    [Fact]
    public async Task CompareAsync_DisplayNameChange_ClassifiedLow()
    {
        WritePolicy(_baselinePath, "CompliancePolicies", "PolicyA.json", """{ "displayName": "Old Name" }""");
        WritePolicy(_currentPath, "CompliancePolicies", "PolicyA.json", """{ "displayName": "New Name" }""");

        var report = await _sut.CompareAsync(_baselinePath, _currentPath);

        var change = Assert.Single(report.Changes);
        Assert.Equal(DriftSeverity.Low, change.Severity);
    }

    [Fact]
    public async Task CompareAsync_CancellationToken_Respected()
    {
        WritePolicy(_baselinePath, "CompliancePolicies", "PolicyA.json", """{ "displayName": "A" }""");
        WritePolicy(_currentPath, "CompliancePolicies", "PolicyA.json", """{ "displayName": "B" }""");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _sut.CompareAsync(_baselinePath, _currentPath, cancellationToken: cts.Token));
    }

    [Fact]
    public async Task CompareAsync_EmptyDirectories_NoDrift()
    {
        var report = await _sut.CompareAsync(_baselinePath, _currentPath);

        Assert.False(report.DriftDetected);
        Assert.Empty(report.Changes);
    }

    private static void WritePolicy(string root, string folder, string fileName, string json)
    {
        var path = Path.Combine(root, folder);
        Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path, fileName), json);
    }
}
