using Intune.Commander.Core.Models;
using Intune.Commander.Core.Services;

namespace Intune.Commander.Core.Tests.Services;

public sealed class DriftDetectionSeverityTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"ic-drift-severity-{Guid.NewGuid():N}");
    private readonly string _baselinePath;
    private readonly string _currentPath;
    private readonly DriftDetectionService _sut = new(new ExportNormalizer());

    public DriftDetectionSeverityTests()
    {
        _baselinePath = Path.Combine(_root, "baseline");
        _currentPath = Path.Combine(_root, "current");
        Directory.CreateDirectory(_baselinePath);
        Directory.CreateDirectory(_currentPath);
    }

    [Fact]
    public async Task CompareAsync_ReportOnlyState_ClassifiedHigh()
    {
        WritePolicy(_baselinePath, "CompliancePolicies", "PolicyA.json", """{ "state": "enabled" }""");
        WritePolicy(_currentPath, "CompliancePolicies", "PolicyA.json", """{ "state": "reportOnly" }""");

        var report = await _sut.CompareAsync(_baselinePath, _currentPath);

        var change = Assert.Single(report.Changes);
        Assert.Equal(DriftSeverity.High, change.Severity);
    }

    [Fact]
    public async Task CompareAsync_IsEnabledFalse_ClassifiedCritical()
    {
        WritePolicy(_baselinePath, "CompliancePolicies", "PolicyA.json", """{ "isEnabled": true }""");
        WritePolicy(_currentPath, "CompliancePolicies", "PolicyA.json", """{ "isEnabled": false }""");

        var report = await _sut.CompareAsync(_baselinePath, _currentPath);

        var change = Assert.Single(report.Changes);
        Assert.Equal(DriftSeverity.Critical, change.Severity);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private static void WritePolicy(string root, string folder, string fileName, string json)
    {
        var path = Path.Combine(root, folder);
        Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path, fileName), json);
    }
}
