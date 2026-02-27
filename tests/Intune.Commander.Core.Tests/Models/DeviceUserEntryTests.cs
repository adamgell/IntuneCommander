using Intune.Commander.Core.Models;
using Microsoft.Graph.Beta.Models;

namespace Intune.Commander.Core.Tests.Models;

public class DeviceUserEntryTests
{
    [Fact]
    public void From_WithMatchingUser_MapsDeviceAndUserFields()
    {
        var device = new ManagedDevice
        {
            Id = "device-1",
            DeviceName = "Laptop-01",
            UserId = "user-1",
            OperatingSystem = "Windows",
            ComplianceState = ComplianceState.Compliant
        };
        var user = new User
        {
            Id = "user-1",
            DisplayName = "Alex Admin",
            UserPrincipalName = "alex@contoso.com",
            Department = "IT",
            OnPremisesExtensionAttributes = new OnPremisesExtensionAttributes
            {
                ExtensionAttribute1 = "EA1"
            }
        };

        var result = DeviceUserEntry.From(device, user);

        Assert.Equal("device-1", result.DeviceId);
        Assert.Equal("Laptop-01", result.DeviceName);
        Assert.Equal("user-1", result.UserId);
        Assert.Equal("Alex Admin", result.UserDisplayName);
        Assert.Equal("alex@contoso.com", result.UserPrincipalName);
        Assert.Equal("IT", result.Department);
        Assert.Equal("Windows", result.OperatingSystem);
        Assert.Equal("Compliant", result.ComplianceState);
        Assert.Equal("EA1", result.ExtensionAttribute1);
    }

    [Fact]
    public void From_WithNoMatchingUser_LeavesUserFieldsBlank()
    {
        var device = new ManagedDevice
        {
            Id = "device-2",
            DeviceName = "Shared-Device",
            UserId = "missing-user",
            OperatingSystem = "iOS"
        };

        var result = DeviceUserEntry.From(device, null);

        Assert.Equal("device-2", result.DeviceId);
        Assert.Equal("Shared-Device", result.DeviceName);
        Assert.Equal("missing-user", result.UserId);
        Assert.Equal("", result.UserDisplayName);
        Assert.Equal("", result.UserPrincipalName);
        Assert.Equal("iOS", result.OperatingSystem);
    }
}
