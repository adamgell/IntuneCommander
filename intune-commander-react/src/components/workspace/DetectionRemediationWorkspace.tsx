import { useEffect } from 'react';
import { useDetectionRemediationStore } from '../../store/detectionRemediationStore';
import { ScriptListView } from './ScriptListView';
import { ScriptDetailDashboard } from './ScriptDetailDashboard';
import '../../styles/workspace.css';

export function DetectionRemediationWorkspace() {
  const scripts = useDetectionRemediationStore((s) => s.scripts);
  const selectedScriptId = useDetectionRemediationStore((s) => s.selectedScriptId);
  const isLoadingList = useDetectionRemediationStore((s) => s.isLoadingList);
  const hasAttemptedLoad = useDetectionRemediationStore((s) => s.hasAttemptedLoad);
  const error = useDetectionRemediationStore((s) => s.error);
  const loadScripts = useDetectionRemediationStore((s) => s.loadScripts);

  useEffect(() => {
    if (!hasAttemptedLoad && !isLoadingList) {
      void loadScripts();
    }
  }, [hasAttemptedLoad, isLoadingList, loadScripts]);

  return (
    <div className="workspace">
      <div className="workspace-toolbar">
        <div className="workspace-heading">
          <strong className="workspace-title">Detection &amp; Remediation</strong>
          <div className="workspace-stats">
            <span className="inline-stat">
              <strong>{scripts.length}</strong> scripts
            </span>
          </div>
        </div>
      </div>

      {error && <div className="workspace-error">{error}</div>}

      {selectedScriptId ? (
        <ScriptDetailDashboard />
      ) : (
        <ScriptListView />
      )}

      <div className="workspace-footer">
        <span>{scripts.length} scripts loaded</span>
        {isLoadingList && <span>Loading...</span>}
      </div>
    </div>
  );
}
