using Intune.Commander.Core.Models;

namespace Intune.Commander.Core.Tests.Models;

public class OnDemandDeploymentRecordTests
{
    [Fact]
    public void RequiredProperties_MustBeSet()
    {
        var record = new OnDemandDeploymentRecord
        {
            ScriptId = "script-1",
            ScriptName = "Test Script",
            DeviceId = "device-1",
            DeviceName = "DESKTOP-001"
        };

        Assert.Equal("script-1", record.ScriptId);
        Assert.Equal("Test Script", record.ScriptName);
        Assert.Equal("device-1", record.DeviceId);
        Assert.Equal("DESKTOP-001", record.DeviceName);
    }

    [Fact]
    public void DispatchedAt_DefaultsToUtcNow()
    {
        var before = DateTimeOffset.UtcNow;
        var record = new OnDemandDeploymentRecord
        {
            ScriptId = "s1",
            ScriptName = "S",
            DeviceId = "d1",
            DeviceName = "D"
        };
        var after = DateTimeOffset.UtcNow;

        Assert.True(record.DispatchedAt >= before);
        Assert.True(record.DispatchedAt <= after);
    }

    [Fact]
    public void Succeeded_DefaultsFalse()
    {
        var record = new OnDemandDeploymentRecord
        {
            ScriptId = "s1",
            ScriptName = "S",
            DeviceId = "d1",
            DeviceName = "D"
        };

        Assert.False(record.Succeeded);
    }

    [Fact]
    public void ErrorMessage_DefaultsNull()
    {
        var record = new OnDemandDeploymentRecord
        {
            ScriptId = "s1",
            ScriptName = "S",
            DeviceId = "d1",
            DeviceName = "D"
        };

        Assert.Null(record.ErrorMessage);
    }

    [Fact]
    public void Succeeded_IsMutable()
    {
        var record = new OnDemandDeploymentRecord
        {
            ScriptId = "s1",
            ScriptName = "S",
            DeviceId = "d1",
            DeviceName = "D"
        };

        record.Succeeded = true;
        Assert.True(record.Succeeded);
    }

    [Fact]
    public void ErrorMessage_IsMutable()
    {
        var record = new OnDemandDeploymentRecord
        {
            ScriptId = "s1",
            ScriptName = "S",
            DeviceId = "d1",
            DeviceName = "D"
        };

        record.ErrorMessage = "Something failed";
        Assert.Equal("Something failed", record.ErrorMessage);
    }
}
