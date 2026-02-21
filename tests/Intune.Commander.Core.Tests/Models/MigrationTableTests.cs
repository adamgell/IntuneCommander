using Intune.Commander.Core.Models;

namespace Intune.Commander.Core.Tests.Models;

public class MigrationTableTests
{
    [Fact]
    public void AddOrUpdate_NewEntry_AddsToList()
    {
        var table = new MigrationTable();
        var entry = new MigrationEntry
        {
            ObjectType = "DeviceConfiguration",
            OriginalId = "id-1",
            Name = "Test Policy"
        };

        table.AddOrUpdate(entry);

        Assert.Single(table.Entries);
        Assert.Equal("id-1", table.Entries[0].OriginalId);
    }

    [Fact]
    public void AddOrUpdate_ExistingEntry_UpdatesInPlace()
    {
        var table = new MigrationTable();
        var entry1 = new MigrationEntry
        {
            ObjectType = "DeviceConfiguration",
            OriginalId = "id-1",
            Name = "Test Policy"
        };
        table.AddOrUpdate(entry1);

        var entry2 = new MigrationEntry
        {
            ObjectType = "DeviceConfiguration",
            OriginalId = "id-1",
            NewId = "new-id-1",
            Name = "Updated Policy"
        };
        table.AddOrUpdate(entry2);

        Assert.Single(table.Entries);
        Assert.Equal("new-id-1", table.Entries[0].NewId);
        Assert.Equal("Updated Policy", table.Entries[0].Name);
    }

    [Fact]
    public void AddOrUpdate_DifferentObjectType_AddsSeparately()
    {
        var table = new MigrationTable();
        table.AddOrUpdate(new MigrationEntry
        {
            ObjectType = "DeviceConfiguration",
            OriginalId = "id-1",
            Name = "Policy 1"
        });
        table.AddOrUpdate(new MigrationEntry
        {
            ObjectType = "CompliancePolicy",
            OriginalId = "id-1",
            Name = "Compliance 1"
        });

        Assert.Equal(2, table.Entries.Count);
    }
}
