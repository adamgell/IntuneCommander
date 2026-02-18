using IntuneManager.Core.Models;
using Microsoft.Graph.Beta.Models;

namespace IntuneManager.Core.Services;

public interface IImportService
{
    Task<DeviceConfiguration?> ReadDeviceConfigurationAsync(string filePath, CancellationToken cancellationToken = default);
    Task<List<DeviceConfiguration>> ReadDeviceConfigurationsFromFolderAsync(string folderPath, CancellationToken cancellationToken = default);
    Task<MigrationTable> ReadMigrationTableAsync(string folderPath, CancellationToken cancellationToken = default);
    Task<DeviceConfiguration> ImportDeviceConfigurationAsync(DeviceConfiguration config, MigrationTable migrationTable, CancellationToken cancellationToken = default);

    Task<CompliancePolicyExport?> ReadCompliancePolicyAsync(string filePath, CancellationToken cancellationToken = default);
    Task<List<CompliancePolicyExport>> ReadCompliancePoliciesFromFolderAsync(string folderPath, CancellationToken cancellationToken = default);
    Task<DeviceCompliancePolicy> ImportCompliancePolicyAsync(CompliancePolicyExport export, MigrationTable migrationTable, CancellationToken cancellationToken = default);

    Task<EndpointSecurityExport?> ReadEndpointSecurityIntentAsync(string filePath, CancellationToken cancellationToken = default);
    Task<List<EndpointSecurityExport>> ReadEndpointSecurityIntentsFromFolderAsync(string folderPath, CancellationToken cancellationToken = default);
    Task<DeviceManagementIntent> ImportEndpointSecurityIntentAsync(EndpointSecurityExport export, MigrationTable migrationTable, CancellationToken cancellationToken = default);

    Task<AdministrativeTemplateExport?> ReadAdministrativeTemplateAsync(string filePath, CancellationToken cancellationToken = default);
    Task<List<AdministrativeTemplateExport>> ReadAdministrativeTemplatesFromFolderAsync(string folderPath, CancellationToken cancellationToken = default);
    Task<GroupPolicyConfiguration> ImportAdministrativeTemplateAsync(AdministrativeTemplateExport export, MigrationTable migrationTable, CancellationToken cancellationToken = default);

    Task<DeviceEnrollmentConfiguration?> ReadEnrollmentConfigurationAsync(string filePath, CancellationToken cancellationToken = default);
    Task<List<DeviceEnrollmentConfiguration>> ReadEnrollmentConfigurationsFromFolderAsync(string folderPath, CancellationToken cancellationToken = default);
    Task<DeviceEnrollmentConfiguration> ImportEnrollmentConfigurationAsync(DeviceEnrollmentConfiguration configuration, MigrationTable migrationTable, CancellationToken cancellationToken = default);

    Task<ManagedAppPolicy?> ReadAppProtectionPolicyAsync(string filePath, CancellationToken cancellationToken = default);
    Task<List<ManagedAppPolicy>> ReadAppProtectionPoliciesFromFolderAsync(string folderPath, CancellationToken cancellationToken = default);
    Task<ManagedAppPolicy> ImportAppProtectionPolicyAsync(ManagedAppPolicy policy, MigrationTable migrationTable, CancellationToken cancellationToken = default);

    Task<ManagedDeviceMobileAppConfiguration?> ReadManagedDeviceAppConfigurationAsync(string filePath, CancellationToken cancellationToken = default);
    Task<List<ManagedDeviceMobileAppConfiguration>> ReadManagedDeviceAppConfigurationsFromFolderAsync(string folderPath, CancellationToken cancellationToken = default);
    Task<ManagedDeviceMobileAppConfiguration> ImportManagedDeviceAppConfigurationAsync(ManagedDeviceMobileAppConfiguration configuration, MigrationTable migrationTable, CancellationToken cancellationToken = default);

    Task<TargetedManagedAppConfiguration?> ReadTargetedManagedAppConfigurationAsync(string filePath, CancellationToken cancellationToken = default);
    Task<List<TargetedManagedAppConfiguration>> ReadTargetedManagedAppConfigurationsFromFolderAsync(string folderPath, CancellationToken cancellationToken = default);
    Task<TargetedManagedAppConfiguration> ImportTargetedManagedAppConfigurationAsync(TargetedManagedAppConfiguration configuration, MigrationTable migrationTable, CancellationToken cancellationToken = default);

    Task<TermsAndConditions?> ReadTermsAndConditionsAsync(string filePath, CancellationToken cancellationToken = default);
    Task<List<TermsAndConditions>> ReadTermsAndConditionsFromFolderAsync(string folderPath, CancellationToken cancellationToken = default);
    Task<TermsAndConditions> ImportTermsAndConditionsAsync(TermsAndConditions termsAndConditions, MigrationTable migrationTable, CancellationToken cancellationToken = default);
}
