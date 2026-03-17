import { useApplicationsStore } from '../../store/applicationsStore';

export function AppDetailPanel() {
  const appDetail = useApplicationsStore((s) => s.appDetail);
  const isLoadingDetail = useApplicationsStore((s) => s.isLoadingDetail);
  const clearSelection = useApplicationsStore((s) => s.clearSelection);

  if (isLoadingDetail || !appDetail) {
    return (
      <div className="panel panel-detail">
        <div className="panel-header">
          <button className="back-btn" onClick={clearSelection}>
            Back to list
          </button>
        </div>
        <div className="panel-body loading-state">
          <div className="loading-spinner" />
          <p>Loading application details...</p>
        </div>
      </div>
    );
  }

  const hasRequired = appDetail.assignments.required.length > 0;
  const hasAvailable = appDetail.assignments.available.length > 0;
  const hasUninstall = appDetail.assignments.uninstall.length > 0;
  const hasAssignments = hasRequired || hasAvailable || hasUninstall;

  return (
    <div className="panel panel-detail">
      <div className="panel-header">
        <div className="detail-header-row">
          <button className="back-btn" onClick={clearSelection}>
            Back to list
          </button>
          <div className="detail-header-actions">
            <button className="ws-btn secondary" disabled>Export JSON</button>
          </div>
        </div>
      </div>
      <div className="panel-body">
        <div className="fluent-detail-view">
          {/* Title block */}
          <div className="fluent-title-block">
            <strong>{appDetail.displayName}</strong>
            {appDetail.description && <span>{appDetail.description}</span>}
          </div>

          {/* Status grid */}
          <div className="fluent-status-grid">
            <div className="fluent-status-card">
              <span className="fluent-status-number">
                {appDetail.assignments.required.length +
                  appDetail.assignments.available.length +
                  appDetail.assignments.uninstall.length}
              </span>
              <span className="fluent-status-label">Assignments</span>
            </div>
            <div className="fluent-status-card">
              <span className="fluent-status-number">{appDetail.platform}</span>
              <span className="fluent-status-label">Platform</span>
            </div>
            <div className="fluent-status-card">
              <span className="fluent-status-number">{appDetail.publishingState}</span>
              <span className="fluent-status-label">State</span>
            </div>
            {appDetail.sizeMB !== undefined && appDetail.sizeMB > 0 && (
              <div className="fluent-status-card">
                <span className="fluent-status-number">{appDetail.sizeMB} MB</span>
                <span className="fluent-status-label">Size</span>
              </div>
            )}
          </div>

          <div className="detail-divider" />

          {/* Properties section */}
          <section className="fluent-section">
            <div className="fluent-section-header">
              <strong>Properties</strong>
            </div>
            <div className="fluent-subsection">
              <div className="fluent-subheader">Basics</div>
              <div className="fluent-properties">
                <div className="fluent-property-row">
                  <div className="fluent-property-label">Name</div>
                  <div className="fluent-property-value">{appDetail.displayName}</div>
                </div>
                <div className="fluent-property-row">
                  <div className="fluent-property-label">Type</div>
                  <div className="fluent-property-value">{appDetail.appType}</div>
                </div>
                <div className="fluent-property-row">
                  <div className="fluent-property-label">Platform</div>
                  <div className="fluent-property-value">{appDetail.platform}</div>
                </div>
                {appDetail.publisher && (
                  <div className="fluent-property-row">
                    <div className="fluent-property-label">Publisher</div>
                    <div className="fluent-property-value">{appDetail.publisher}</div>
                  </div>
                )}
                {appDetail.developer && (
                  <div className="fluent-property-row">
                    <div className="fluent-property-label">Developer</div>
                    <div className="fluent-property-value">{appDetail.developer}</div>
                  </div>
                )}
                {appDetail.version && (
                  <div className="fluent-property-row">
                    <div className="fluent-property-label">Version</div>
                    <div className="fluent-property-value">{appDetail.version}</div>
                  </div>
                )}
                {appDetail.bundleId && (
                  <div className="fluent-property-row">
                    <div className="fluent-property-label">Bundle ID</div>
                    <div className="fluent-property-value">{appDetail.bundleId}</div>
                  </div>
                )}
                {appDetail.minimumOsVersion && (
                  <div className="fluent-property-row">
                    <div className="fluent-property-label">Minimum OS</div>
                    <div className="fluent-property-value">{appDetail.minimumOsVersion}</div>
                  </div>
                )}
                {appDetail.installCommand && (
                  <div className="fluent-property-row">
                    <div className="fluent-property-label">Install command</div>
                    <div className="fluent-property-value" style={{ fontFamily: 'monospace', fontSize: 11 }}>
                      {appDetail.installCommand}
                    </div>
                  </div>
                )}
                {appDetail.uninstallCommand && (
                  <div className="fluent-property-row">
                    <div className="fluent-property-label">Uninstall command</div>
                    <div className="fluent-property-value" style={{ fontFamily: 'monospace', fontSize: 11 }}>
                      {appDetail.uninstallCommand}
                    </div>
                  </div>
                )}
                <div className="fluent-property-row">
                  <div className="fluent-property-label">Last modified</div>
                  <div className="fluent-property-value">
                    {appDetail.lastModifiedDateTime
                      ? new Date(appDetail.lastModifiedDateTime).toLocaleString()
                      : ''}
                  </div>
                </div>
                <div className="fluent-property-row">
                  <div className="fluent-property-label">Created</div>
                  <div className="fluent-property-value">
                    {appDetail.createdDateTime
                      ? new Date(appDetail.createdDateTime).toLocaleString()
                      : ''}
                  </div>
                </div>
              </div>
            </div>
          </section>

          <div className="detail-divider" />

          {/* Assignments section */}
          <section className="fluent-section">
            <div className="fluent-section-header">
              <strong>Assignments</strong>
            </div>

            {hasRequired && (
              <div className="fluent-subsection">
                <div className="fluent-subheader">Required</div>
                <div className="fluent-table-wrap">
                  <table className="fluent-assignment-table">
                    <thead>
                      <tr>
                        <th>Group</th>
                        <th>Filter</th>
                      </tr>
                    </thead>
                    <tbody>
                      {appDetail.assignments.required.map((a, i) => (
                        <tr key={i}>
                          <td>{a.groupName}</td>
                          <td className="muted">{a.filter ?? 'None'}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            )}

            {hasAvailable && (
              <div className="fluent-subsection">
                <div className="fluent-subheader">Available</div>
                <div className="fluent-table-wrap">
                  <table className="fluent-assignment-table">
                    <thead>
                      <tr>
                        <th>Group</th>
                        <th>Filter</th>
                      </tr>
                    </thead>
                    <tbody>
                      {appDetail.assignments.available.map((a, i) => (
                        <tr key={i}>
                          <td>{a.groupName}</td>
                          <td className="muted">{a.filter ?? 'None'}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            )}

            {hasUninstall && (
              <div className="fluent-subsection">
                <div className="fluent-subheader">Uninstall</div>
                <div className="fluent-table-wrap">
                  <table className="fluent-assignment-table">
                    <thead>
                      <tr>
                        <th>Group</th>
                        <th>Filter</th>
                      </tr>
                    </thead>
                    <tbody>
                      {appDetail.assignments.uninstall.map((a, i) => (
                        <tr key={i}>
                          <td>{a.groupName}</td>
                          <td className="muted">{a.filter ?? 'None'}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            )}

            {!hasAssignments && (
              <div className="fluent-subsection">
                <p className="muted">No assignments configured</p>
              </div>
            )}
          </section>
        </div>
      </div>
    </div>
  );
}
