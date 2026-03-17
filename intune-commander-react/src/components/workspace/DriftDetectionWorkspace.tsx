import '../../styles/workspace.css';

export function DriftDetectionWorkspace() {
  return (
    <div className="workspace">
      <div className="workspace-toolbar">
        <div className="workspace-heading">
          <strong className="workspace-title">Drift Detection</strong>
          <div className="workspace-stats">
            <span className="inline-stat" style={{ color: 'var(--text-secondary)' }}>
              Compare live tenant configuration against a saved baseline
            </span>
          </div>
        </div>
      </div>

      <div style={{
        flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center',
        color: 'var(--text-tertiary)', fontSize: 14,
      }}>
        Drift Detection workspace — coming in Phase 5
      </div>

      <div className="workspace-footer" />
    </div>
  );
}
