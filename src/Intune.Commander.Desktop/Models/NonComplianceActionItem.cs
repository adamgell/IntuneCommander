namespace Intune.Commander.Desktop.Models;

/// <summary>
/// Represents a single non-compliance action from a compliance policy's
/// scheduledActionsForRule → scheduledActionConfigurations.
/// </summary>
public sealed record NonComplianceActionItem(
    string ActionType,
    int GracePeriodHours,
    string NotificationTemplateId)
{
    /// <summary>Human-readable grace period display.</summary>
    public string GracePeriodDisplay => GracePeriodHours switch
    {
        0 => "Immediately",
        < 24 => $"{GracePeriodHours} hour(s)",
        _ => GracePeriodHours % 24 == 0
            ? $"{GracePeriodHours / 24} day(s)"
            : $"{GracePeriodHours / 24}d {GracePeriodHours % 24}h"
    };

    /// <summary>Whether a notification template is configured (non-empty GUID).</summary>
    public bool HasNotificationTemplate =>
        !string.IsNullOrEmpty(NotificationTemplateId) &&
        NotificationTemplateId != "00000000-0000-0000-0000-000000000000";

    /// <summary>Human-readable action type.</summary>
    public string ActionTypeDisplay => ActionType?.ToLowerInvariant() switch
    {
        "block" => "Mark device non-compliant",
        "notification" => "Send email to end user",
        "retire" => "Retire the device",
        "wipe" => "Wipe the device",
        "pushnotification" => "Send push notification",
        "remotelock" => "Remotely lock device",
        "removeresourceaccessprofiles" => "Remove resource access",
        "noaction" => "No action",
        _ => ActionType ?? "Unknown"
    };
}
