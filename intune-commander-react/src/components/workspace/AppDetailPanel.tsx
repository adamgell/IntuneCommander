import { useApplicationsStore } from '../../store/applicationsStore';

function formatDateTime(value?: string) {
  return value ? new Date(value).toLocaleString() : '—';
}

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
                <div className="fluent-status-card">
                  <span className="fluent-status-number">{appDetail.isFeatured ? 'Yes' : 'No'}</span>
                  <span className="fluent-status-label">Featured</span>
                </div>
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
                {appDetail.owner && (
                  <div className="fluent-property-row">
                    <div className="fluent-property-label">Owner</div>
                    <div className="fluent-property-value">{appDetail.owner}</div>
                  </div>
                )}
                <div className="fluent-property-row">
                  <div className="fluent-property-label">Featured</div>
                  <div className="fluent-property-value">{appDetail.isFeatured ? 'Yes' : 'No'}</div>
                </div>
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
                {appDetail.installContext && (
                  <div className="fluent-property-row">
                    <div className="fluent-property-label">Install context</div>
                    <div className="fluent-property-value">{appDetail.installContext}</div>
                  </div>
                )}
                {appDetail.sizeMB !== undefined && appDetail.sizeMB > 0 && (
                  <div className="fluent-property-row">
                    <div className="fluent-property-label">Size</div>
                    <div className="fluent-property-value">{appDetail.sizeMB} MB</div>
                  </div>
                )}
                {appDetail.supersededAppCount > 0 && (
                  <div className="fluent-property-row">
                    <div className="fluent-property-label">Superseded apps</div>
                    <div className="fluent-property-value">{appDetail.supersededAppCount}</div>
                  </div>
                )}
                <div className="fluent-property-row">
                  <div className="fluent-property-label">Last modified</div>
                  <div className="fluent-property-value">{formatDateTime(appDetail.lastModifiedDateTime)}</div>
                </div>
                <div className="fluent-property-row">
                  <div className="fluent-property-label">Created</div>
                  <div className="fluent-property-value">{formatDateTime(appDetail.createdDateTime)}</div>
                </div>
              </div>
            </div>
          </section>

          <div className="detail-divider" />

          <section className="fluent-section">
            <div className="fluent-section-header">
              <strong>Links & metadata</strong>
            </div>
            <div className="fluent-subsection">
              <div className="fluent-properties">
                {appDetail.informationUrl && (
                  <div className="fluent-property-row">
                    <div className="fluent-property-label">Information URL</div>
                    <div className="fluent-property-value" style={{ wordBreak: 'break-all' }}>{appDetail.informationUrl}</div>
                  </div>
                )}
                {appDetail.privacyInformationUrl && (
                  <div className="fluent-property-row">
                    <div className="fluent-property-label">Privacy URL</div>
                    <div className="fluent-property-value" style={{ wordBreak: 'break-all' }}>{appDetail.privacyInformationUrl}</div>
                  </div>
                )}
                {appDetail.appStoreUrl && (
                  <div className="fluent-property-row">
                    <div className="fluent-property-label">App Store URL</div>
                    <div className="fluent-property-value" style={{ wordBreak: 'break-all' }}>{appDetail.appStoreUrl}</div>
                  </div>
                )}
                {appDetail.notes && (
                  <div className="fluent-property-row">
                    <div className="fluent-property-label">Notes</div>
                    <div className="fluent-property-value">{appDetail.notes}</div>
                  </div>
                )}
                <div className="fluent-property-row">
                  <div className="fluent-property-label">Categories</div>
                  <div className="fluent-property-value">
                    {appDetail.categories.length > 0 ? (
                      <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6 }}>
                        {appDetail.categories.map((category) => (
                          <span
                            key={category}
                            style={{
                              display: 'inline-flex',
                              alignItems: 'center',
                              borderRadius: 999,
                              padding: '4px 10px',
                              fontSize: 11,
                              background: 'rgba(59,130,246,0.12)',
                              color: '#93c5fd',
                              border: '1px solid rgba(59,130,246,0.24)',
                            }}
                          >
                            {category}
                          </span>
                        ))}
                      </div>
                    ) : (
                      'None'
                    )}
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
                        <th>Intent</th>
                        <th>Filter</th>
                      </tr>
                    </thead>
                    <tbody>
                      {appDetail.assignments.required.map((a, i) => (
                        <tr key={i}>
                          <td>{a.groupName}</td>
                          <td>{a.intent}</td>
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
                        <th>Intent</th>
                        <th>Filter</th>
                      </tr>
                    </thead>
                    <tbody>
                      {appDetail.assignments.available.map((a, i) => (
                        <tr key={i}>
                          <td>{a.groupName}</td>
                          <td>{a.intent}</td>
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
                        <th>Intent</th>
                        <th>Filter</th>
                      </tr>
                    </thead>
                    <tbody>
                      {appDetail.assignments.uninstall.map((a, i) => (
                        <tr key={i}>
                          <td>{a.groupName}</td>
                          <td>{a.intent}</td>
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
