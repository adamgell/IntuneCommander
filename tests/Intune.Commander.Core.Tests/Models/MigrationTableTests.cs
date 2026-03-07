using Intune.Commander.Core.Models;

namespace Intune.Commander.Core.Tests.Models;

public sealed class MigrationTableTests
{
    [Fact]
    public void AddOrUpdate_ExistingEntry_UpdatesInsteadOfDuplicating()
    {
        var table = new MigrationTable();
        table.AddOrUpdate(new MigrationEntry
        {
            ObjectType = "DeviceConfiguration",
            OriginalId = "cfg-1",
            Name = "Original"
        });

        table.AddOrUpdate(new MigrationEntry
        {
            ObjectType = "DeviceConfiguration",
            OriginalId = "cfg-1",
            NewId = "new-cfg-1",
            Name = "Updated"
        });

        var entry = Assert.Single(table.Entries);
        Assert.Equal("new-cfg-1", entry.NewId);
        Assert.Equal("Updated", entry.Name);
    }
}
