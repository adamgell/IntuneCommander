import type { ComponentType } from 'react';
import { useAppStore } from '../../store/appStore';
import { OverviewDashboard } from '../workspace/OverviewDashboard';
import { SettingsCatalogWorkspace } from '../workspace/SettingsCatalogWorkspace';
import { DetectionRemediationWorkspace } from '../workspace/DetectionRemediationWorkspace';
import { GlobalSearchResultsWorkspace } from '../workspace/GlobalSearchResultsWorkspace';
import { CacheDevWorkspace } from '../workspace/CacheDevWorkspace';
import { ApplicationsWorkspace } from '../workspace/ApplicationsWorkspace';
import { AppAssignmentsWorkspace } from '../workspace/AppAssignmentsWorkspace';
import { BulkAppAssignmentsWorkspace } from '../workspace/BulkAppAssignmentsWorkspace';
import { AppProtectionPoliciesWorkspace } from '../workspace/AppProtectionPoliciesWorkspace';
import { ConditionalAccessWorkspace } from '../workspace/ConditionalAccessWorkspace';
import { SecurityPostureWorkspace } from '../workspace/SecurityPostureWorkspace';
import { AssignmentExplorerWorkspace } from '../workspace/AssignmentExplorerWorkspace';
import { ScriptsHubWorkspace } from '../workspace/ScriptsHubWorkspace';
import { PolicyComparisonWorkspace } from '../workspace/PolicyComparisonWorkspace';
import { DeviceConfigWorkspace } from '../workspace/DeviceConfigWorkspace';
import { CompliancePolicyWorkspace } from '../workspace/CompliancePolicyWorkspace';
import { EndpointSecurityWorkspace } from '../workspace/EndpointSecurityWorkspace';
import { EnrollmentWorkspace } from '../workspace/EnrollmentWorkspace';
import { DriftDetectionWorkspace } from '../workspace/DriftDetectionWorkspace';
import { ExportImportWorkspace } from '../workspace/ExportImportWorkspace';
import { TenantAdminWorkspace } from '../workspace/TenantAdminWorkspace';
import { GroupsWorkspace } from '../workspace/GroupsWorkspace';
import { ManagedDeviceAppConfigurationsWorkspace } from '../workspace/ManagedDeviceAppConfigurationsWorkspace';
import { TargetedManagedAppConfigurationsWorkspace } from '../workspace/TargetedManagedAppConfigurationsWorkspace';
import { VppTokensWorkspace } from '../workspace/VppTokensWorkspace';

const workspaceMap: Record<string, ComponentType> = {
  'global-search': GlobalSearchResultsWorkspace,
  'overview': OverviewDashboard,
  'settings-catalog': SettingsCatalogWorkspace,
  'detection-remediation': DetectionRemediationWorkspace,
  'cache-inspector': CacheDevWorkspace,
  'applications': ApplicationsWorkspace,
  'application-assignments': AppAssignmentsWorkspace,
  'bulk-app-assignments': BulkAppAssignmentsWorkspace,
  'app-protection-policies': AppProtectionPoliciesWorkspace,
  'managed-device-app-configurations': ManagedDeviceAppConfigurationsWorkspace,
  'targeted-managed-app-configurations': TargetedManagedAppConfigurationsWorkspace,
  'vpp-tokens': VppTokensWorkspace,
  'conditional-access': ConditionalAccessWorkspace,
  'security-posture': SecurityPostureWorkspace,
  'assignment-explorer': AssignmentExplorerWorkspace,
  'scripts-hub': ScriptsHubWorkspace,
  'policy-comparison': PolicyComparisonWorkspace,
  'device-config': DeviceConfigWorkspace,
  'compliance-policy': CompliancePolicyWorkspace,
  'endpoint-security': EndpointSecurityWorkspace,
  'enrollment': EnrollmentWorkspace,
  'drift-detection': DriftDetectionWorkspace,
  'export-import': ExportImportWorkspace,
  'tenant-admin': TenantAdminWorkspace,
  'groups': GroupsWorkspace,
};

export function ContentArea() {
  const activeSidebarItem = useAppStore((s) => s.activeSidebarItem);

  const WorkspaceComponent = activeSidebarItem ? workspaceMap[activeSidebarItem] : undefined;

  if (WorkspaceComponent) {
    return (
      <main className="content-area">
        <WorkspaceComponent />
      </main>
    );
  }

  return (
    <main className="content-area">
      <div className="content-placeholder">
        <h3>{activeSidebarItem ?? 'Welcome'}</h3>
        <p>Select a workspace from the sidebar to get started.</p>
      </div>
    </main>
  );
}
