using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;
using Microsoft.Kiota.Abstractions;

namespace Intune.Commander.Core.Services;

public class ManagedDeviceService(GraphServiceClient graphClient) : IManagedDeviceService
{
    private readonly GraphServiceClient _graphClient = graphClient;

    private static readonly string[] ManagedDeviceSelect =
    [
        "id", "deviceName", "userId", "userDisplayName", "userPrincipalName", "emailAddress",
        "managedDeviceOwnerType", "managementState", "enrolledDateTime", "lastSyncDateTime",
        "operatingSystem", "osVersion", "complianceState", "jailBroken",
        "managementAgent", "model", "manufacturer", "serialNumber", "imei",
        "azureADDeviceId", "deviceRegistrationState", "deviceCategoryDisplayName",
        "isSupervised", "isEncrypted", "partnerReportedThreatState",
        "wiFiMacAddress", "ethernetMacAddress", "totalStorageSpaceInBytes", "freeStorageSpaceInBytes"
    ];

    public async Task<List<ManagedDevice>> ListManagedDevicesAsync(CancellationToken cancellationToken = default)
    {
        var result = new List<ManagedDevice>();

        var response = await _graphClient.DeviceManagement.ManagedDevices.GetAsync(req =>
        {
            req.QueryParameters.Top = 999;
            req.QueryParameters.Select = ManagedDeviceSelect;
        }, cancellationToken);

        while (response != null)
        {
            if (response.Value != null)
                result.AddRange(response.Value);

            if (!string.IsNullOrEmpty(response.OdataNextLink))
            {
                response = await _graphClient.DeviceManagement.ManagedDevices
                    .WithUrl(response.OdataNextLink)
                    .GetAsync(cancellationToken: cancellationToken);
            }
            else
            {
                break;
            }
        }

        return result;
    }
}
