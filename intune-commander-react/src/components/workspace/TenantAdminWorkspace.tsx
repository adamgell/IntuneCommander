import '../../styles/workspace.css';

export function TenantAdminWorkspace() {
  return (
    <div className="workspace">
      <div className="workspace-toolbar">
        <div className="workspace-heading">
          <strong className="workspace-title">Tenant Admin</strong>
          <div className="workspace-stats">
            <span className="inline-stat" style={{ color: 'var(--text-secondary)' }}>
              Manage scope tags, roles, branding, terms &amp; conditions, and more
            </span>
          </div>
        </div>
      </div>

      <div style={{
        flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center',
        color: 'var(--text-tertiary)', fontSize: 14,
      }}>
        Tenant Admin workspace — coming in Phase 5
      </div>

      <div className="workspace-footer" />
    </div>
  );
}
