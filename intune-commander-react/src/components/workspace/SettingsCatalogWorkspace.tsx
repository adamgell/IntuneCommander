import { useEffect } from 'react';
import { useSettingsCatalogStore } from '../../store/settingsCatalogStore';
import { PolicyTable } from './PolicyTable';
import { PolicyDetailPanel } from './PolicyDetailPanel';
import '../../styles/workspace.css';

export function SettingsCatalogWorkspace() {
  const policies = useSettingsCatalogStore((s) => s.policies);
  const selectedPolicyId = useSettingsCatalogStore((s) => s.selectedPolicyId);
  const isLoadingList = useSettingsCatalogStore((s) => s.isLoadingList);
  const hasAttemptedLoad = useSettingsCatalogStore((s) => s.hasAttemptedLoad);
  const error = useSettingsCatalogStore((s) => s.error);
  const loadPolicies = useSettingsCatalogStore((s) => s.loadPolicies);

  useEffect(() => {
    if (!hasAttemptedLoad && !isLoadingList) {
      void loadPolicies();
    }
  }, [hasAttemptedLoad, isLoadingList, loadPolicies]);

  return (
    <div className="workspace">
      <div className="workspace-toolbar">
        <div className="workspace-heading">
          <strong className="workspace-title">Settings Catalog</strong>
          <div className="workspace-stats">
            <span className="inline-stat">
              <strong>{policies.length}</strong> policies
            </span>
          </div>
        </div>
        <div className="workspace-actions">
          <button className="ws-btn primary" disabled>Export selected</button>
        </div>
      </div>

      {error && (
        <div className="workspace-error">{error}</div>
      )}

      <div className={`settings-columns${selectedPolicyId ? ' detail-active' : ''}`}>
        <PolicyTable />
        {selectedPolicyId && <PolicyDetailPanel />}
      </div>

      <div className="workspace-footer">
        <span>{policies.length} items loaded</span>
        {isLoadingList && <span>Loading...</span>}
      </div>
    </div>
  );
}
