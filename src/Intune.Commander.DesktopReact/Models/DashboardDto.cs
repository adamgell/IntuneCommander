namespace Intune.Commander.DesktopReact.Models;

public sealed record ComplianceSummaryDto(
    int CompliantDevices,
    int NonCompliantDevices,
    int InGracePeriodDevices,
    int UnknownDevices,
    int TotalManagedDevices);
