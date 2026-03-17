import type { ComponentType } from 'react';
import { useAppStore } from '../../store/appStore';
import { OverviewDashboard } from '../workspace/OverviewDashboard';
import { SettingsCatalogWorkspace } from '../workspace/SettingsCatalogWorkspace';
import { DetectionRemediationWorkspace } from '../workspace/DetectionRemediationWorkspace';
import { GlobalSearchResultsWorkspace } from '../workspace/GlobalSearchResultsWorkspace';
import { CacheDevWorkspace } from '../workspace/CacheDevWorkspace';
import { ApplicationsWorkspace } from '../workspace/ApplicationsWorkspace';
import { ConditionalAccessWorkspace } from '../workspace/ConditionalAccessWorkspace';
import { SecurityPostureWorkspace } from '../workspace/SecurityPostureWorkspace';
import { AssignmentExplorerWorkspace } from '../workspace/AssignmentExplorerWorkspace';
import { ScriptsHubWorkspace } from '../workspace/ScriptsHubWorkspace';
import { PolicyComparisonWorkspace } from '../workspace/PolicyComparisonWorkspace';
import { DeviceConfigWorkspace } from '../workspace/DeviceConfigWorkspace';
import { CompliancePolicyWorkspace } from '../workspace/CompliancePolicyWorkspace';
import { EndpointSecurityWorkspace } from '../workspace/EndpointSecurityWorkspace';
import { EnrollmentWorkspace } from '../workspace/EnrollmentWorkspace';

const workspaceMap: Record<string, ComponentType> = {
  'global-search': GlobalSearchResultsWorkspace,
  'overview': OverviewDashboard,
  'settings-catalog': SettingsCatalogWorkspace,
  'detection-remediation': DetectionRemediationWorkspace,
  'cache-inspector': CacheDevWorkspace,
  'applications': ApplicationsWorkspace,
  'conditional-access': ConditionalAccessWorkspace,
  'security-posture': SecurityPostureWorkspace,
  'assignment-explorer': AssignmentExplorerWorkspace,
  'scripts-hub': ScriptsHubWorkspace,
  'policy-comparison': PolicyComparisonWorkspace,
  'device-config': DeviceConfigWorkspace,
  'compliance-policy': CompliancePolicyWorkspace,
  'endpoint-security': EndpointSecurityWorkspace,
  'enrollment': EnrollmentWorkspace,
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
