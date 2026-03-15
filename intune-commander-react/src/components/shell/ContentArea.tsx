import { useAppStore } from '../../store/appStore';
import { OverviewDashboard } from '../workspace/OverviewDashboard';
import { SettingsCatalogWorkspace } from '../workspace/SettingsCatalogWorkspace';
import { DetectionRemediationWorkspace } from '../workspace/DetectionRemediationWorkspace';
import { GlobalSearchResultsWorkspace } from '../workspace/GlobalSearchResultsWorkspace';
import { CacheDevWorkspace } from '../workspace/CacheDevWorkspace';

export function ContentArea() {
  const activeSidebarItem = useAppStore((s) => s.activeSidebarItem);

  if (activeSidebarItem === 'global-search') {
    return (
      <main className="content-area">
        <GlobalSearchResultsWorkspace />
      </main>
    );
  }

  if (activeSidebarItem === 'overview') {
    return (
      <main className="content-area">
        <OverviewDashboard />
      </main>
    );
  }

  if (activeSidebarItem === 'settings-catalog') {
    return (
      <main className="content-area">
        <SettingsCatalogWorkspace />
      </main>
    );
  }

  if (activeSidebarItem === 'detection-remediation') {
    return (
      <main className="content-area">
        <DetectionRemediationWorkspace />
      </main>
    );
  }

  if (activeSidebarItem === 'cache-inspector') {
    return (
      <main className="content-area">
        <CacheDevWorkspace />
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
