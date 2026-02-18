using IntuneManager.Core.Models;
using Microsoft.Graph.Beta.Models;

namespace IntuneManager.Core.Services;

public interface IExportService
{
    Task ExportDeviceConfigurationAsync(DeviceConfiguration config, string outputPath, MigrationTable migrationTable, CancellationToken cancellationToken = default);
    Task ExportDeviceConfigurationsAsync(IEnumerable<DeviceConfiguration> configs, string outputPath, CancellationToken cancellationToken = default);

    Task ExportCompliancePolicyAsync(DeviceCompliancePolicy policy, IReadOnlyList<DeviceCompliancePolicyAssignment> assignments, string outputPath, MigrationTable migrationTable, CancellationToken cancellationToken = default);
    Task ExportCompliancePoliciesAsync(IEnumerable<(DeviceCompliancePolicy Policy, IReadOnlyList<DeviceCompliancePolicyAssignment> Assignments)> policies, string outputPath, CancellationToken cancellationToken = default);

    Task ExportApplicationAsync(MobileApp app, IReadOnlyList<MobileAppAssignment> assignments, string outputPath, MigrationTable migrationTable, CancellationToken cancellationToken = default);
    Task ExportApplicationsAsync(IEnumerable<(MobileApp App, IReadOnlyList<MobileAppAssignment> Assignments)> apps, string outputPath, CancellationToken cancellationToken = default);

    Task ExportEndpointSecurityIntentAsync(DeviceManagementIntent intent, IReadOnlyList<DeviceManagementIntentAssignment> assignments, string outputPath, MigrationTable migrationTable, CancellationToken cancellationToken = default);
    Task ExportEndpointSecurityIntentsAsync(IEnumerable<(DeviceManagementIntent Intent, IReadOnlyList<DeviceManagementIntentAssignment> Assignments)> intents, string outputPath, CancellationToken cancellationToken = default);

    Task ExportAdministrativeTemplateAsync(GroupPolicyConfiguration template, IReadOnlyList<GroupPolicyConfigurationAssignment> assignments, string outputPath, MigrationTable migrationTable, CancellationToken cancellationToken = default);
    Task ExportAdministrativeTemplatesAsync(IEnumerable<(GroupPolicyConfiguration Template, IReadOnlyList<GroupPolicyConfigurationAssignment> Assignments)> templates, string outputPath, CancellationToken cancellationToken = default);

    Task ExportEnrollmentConfigurationAsync(DeviceEnrollmentConfiguration configuration, string outputPath, MigrationTable migrationTable, CancellationToken cancellationToken = default);
    Task ExportEnrollmentConfigurationsAsync(IEnumerable<DeviceEnrollmentConfiguration> configurations, string outputPath, CancellationToken cancellationToken = default);

    Task ExportAppProtectionPolicyAsync(ManagedAppPolicy policy, string outputPath, MigrationTable migrationTable, CancellationToken cancellationToken = default);
    Task ExportAppProtectionPoliciesAsync(IEnumerable<ManagedAppPolicy> policies, string outputPath, CancellationToken cancellationToken = default);

    Task ExportManagedDeviceAppConfigurationAsync(ManagedDeviceMobileAppConfiguration configuration, string outputPath, MigrationTable migrationTable, CancellationToken cancellationToken = default);
    Task ExportManagedDeviceAppConfigurationsAsync(IEnumerable<ManagedDeviceMobileAppConfiguration> configurations, string outputPath, CancellationToken cancellationToken = default);

    Task ExportTargetedManagedAppConfigurationAsync(TargetedManagedAppConfiguration configuration, string outputPath, MigrationTable migrationTable, CancellationToken cancellationToken = default);
    Task ExportTargetedManagedAppConfigurationsAsync(IEnumerable<TargetedManagedAppConfiguration> configurations, string outputPath, CancellationToken cancellationToken = default);

    Task ExportTermsAndConditionsAsync(TermsAndConditions termsAndConditions, string outputPath, MigrationTable migrationTable, CancellationToken cancellationToken = default);
    Task ExportTermsAndConditionsCollectionAsync(IEnumerable<TermsAndConditions> termsCollection, string outputPath, CancellationToken cancellationToken = default);

    Task SaveMigrationTableAsync(MigrationTable table, string outputPath, CancellationToken cancellationToken = default);
}
