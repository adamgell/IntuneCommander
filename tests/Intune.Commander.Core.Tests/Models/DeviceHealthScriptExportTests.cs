using System.Text.Json;
using Intune.Commander.Core.Models;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Core.Tests.Models;

public class DeviceHealthScriptExportTests
{
    [Fact]
    public void Script_IsRequired()
    {
        var export = new DeviceHealthScriptExport
        {
            Script = new DeviceHealthScript { DisplayName = "Test" }
        };

        Assert.NotNull(export.Script);
        Assert.Equal("Test", export.Script.DisplayName);
    }

    [Fact]
    public void Assignments_DefaultsToEmptyList()
    {
        var export = new DeviceHealthScriptExport
        {
            Script = new DeviceHealthScript()
        };

        Assert.NotNull(export.Assignments);
        Assert.Empty(export.Assignments);
    }

    [Fact]
    public void JsonPropertyNames_AreCorrect()
    {
        var export = new DeviceHealthScriptExport
        {
            Script = new DeviceHealthScript { DisplayName = "Script1" },
            Assignments = [new DeviceHealthScriptAssignment()]
        };

        var json = JsonSerializer.Serialize(export);
        Assert.Contains("\"script\"", json);
        Assert.Contains("\"assignments\"", json);
    }

    [Fact]
    public void Assignments_CanBePopulated()
    {
        var export = new DeviceHealthScriptExport
        {
            Script = new DeviceHealthScript(),
            Assignments =
            [
                new DeviceHealthScriptAssignment(),
                new DeviceHealthScriptAssignment()
            ]
        };

        Assert.Equal(2, export.Assignments.Count);
    }
}
