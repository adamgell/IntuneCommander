import { useState } from 'react';
import { useSettingsCatalogStore } from '../../store/settingsCatalogStore';

export function PolicyDetailPanel() {
  const policyDetail = useSettingsCatalogStore((s) => s.policyDetail);
  const isLoadingDetail = useSettingsCatalogStore((s) => s.isLoadingDetail);
  const clearSelection = useSettingsCatalogStore((s) => s.clearSelection);
  const [settingsSearch, setSettingsSearch] = useState('');

  if (isLoadingDetail || !policyDetail) {
    return (
      <div className="panel panel-detail">
        <div className="panel-header">
          <button className="back-btn" onClick={clearSelection}>
            Back to list
          </button>
        </div>
        <div className="panel-body loading-state">
          <div className="loading-spinner" />
          <p>Loading policy details...</p>
        </div>
      </div>
    );
  }

  const filteredGroups = settingsSearch
    ? policyDetail.settingGroups
        .map((group) => ({
          ...group,
          settings: group.settings.filter(
            (s) =>
              s.label.toLowerCase().includes(settingsSearch.toLowerCase()) ||
              s.value.toLowerCase().includes(settingsSearch.toLowerCase()),
          ),
        }))
        .filter((group) => group.settings.length > 0)
    : policyDetail.settingGroups;

  const totalFilteredSettings = filteredGroups.reduce(
    (sum, g) => sum + g.settings.length,
    0,
  );

  return (
    <div className="panel panel-detail">
      <div className="panel-header">
        <div className="detail-header-row">
          <button className="back-btn" onClick={clearSelection}>
            Back to list
          </button>
          <div className="detail-header-actions">
            <button className="ws-btn secondary" disabled>Export JSON</button>
            <button className="ws-btn primary" disabled>Edit policy</button>
          </div>
        </div>
      </div>
      <div className="panel-body">
        <div className="fluent-detail-view">
          {/* Title block */}
          <div className="fluent-title-block">
            <strong>{policyDetail.name}</strong>
            {policyDetail.description && <span>{policyDetail.description}</span>}
          </div>

          {/* Status grid */}
          <div className="fluent-status-grid">
            <div className="fluent-status-card">
              <span className="fluent-status-number">
                {policyDetail.assignments.included.length +
                  policyDetail.assignments.excluded.length}
              </span>
              <span className="fluent-status-label">Assignments</span>
            </div>
            <div className="fluent-status-card">
              <span className="fluent-status-number">
                {policyDetail.settingCount}
              </span>
              <span className="fluent-status-label">Settings</span>
            </div>
            <div className="fluent-status-card">
              <span className="fluent-status-number">
                {policyDetail.scopeTags.length}
              </span>
              <span className="fluent-status-label">Scope Tags</span>
            </div>
            <div className="fluent-status-card">
              <span className="fluent-status-number">
                {policyDetail.settingGroups.length}
              </span>
              <span className="fluent-status-label">Groups</span>
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
                  <div className="fluent-property-value">{policyDetail.name}</div>
                </div>
                {policyDetail.description && (
                  <div className="fluent-property-row">
                    <div className="fluent-property-label">Description</div>
                    <div className="fluent-property-value">{policyDetail.description}</div>
                  </div>
                )}
                <div className="fluent-property-row">
                  <div className="fluent-property-label">Platform</div>
                  <div className="fluent-property-value">{policyDetail.platform}</div>
                </div>
                <div className="fluent-property-row">
                  <div className="fluent-property-label">Profile type</div>
                  <div className="fluent-property-value">{policyDetail.profileType}</div>
                </div>
                <div className="fluent-property-row">
                  <div className="fluent-property-label">Technologies</div>
                  <div className="fluent-property-value">{policyDetail.technologies}</div>
                </div>
                <div className="fluent-property-row">
                  <div className="fluent-property-label">Scope tags</div>
                  <div className="fluent-property-value">
                    {policyDetail.scopeTags.join(', ')}
                  </div>
                </div>
                <div className="fluent-property-row">
                  <div className="fluent-property-label">Last modified</div>
                  <div className="fluent-property-value">
                    {policyDetail.lastModifiedDateTime
                      ? new Date(policyDetail.lastModifiedDateTime).toLocaleString()
                      : ''}
                  </div>
                </div>
                <div className="fluent-property-row">
                  <div className="fluent-property-label">Created</div>
                  <div className="fluent-property-value">
                    {policyDetail.createdDateTime
                      ? new Date(policyDetail.createdDateTime).toLocaleString()
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

            {policyDetail.assignments.included.length > 0 && (
              <div className="fluent-subsection">
                <div className="fluent-subheader">Included groups</div>
                <div className="fluent-table-wrap">
                  <table className="fluent-assignment-table">
                    <thead>
                      <tr>
                        <th>Group</th>
                        <th>Status</th>
                        <th>Filter</th>
                        <th>Filter mode</th>
                      </tr>
                    </thead>
                    <tbody>
                      {policyDetail.assignments.included.map((a, i) => (
                        <tr key={i}>
                          <td>{a.groupName}</td>
                          <td>{a.status}</td>
                          <td className="muted">{a.filter ?? 'None'}</td>
                          <td className="muted">{a.filterMode ?? 'None'}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            )}

            {policyDetail.assignments.excluded.length > 0 && (
              <div className="fluent-subsection">
                <div className="fluent-subheader">Excluded groups</div>
                <div className="fluent-table-wrap">
                  <table className="fluent-assignment-table">
                    <thead>
                      <tr>
                        <th>Group</th>
                        <th>Status</th>
                        <th>Filter</th>
                        <th>Filter mode</th>
                      </tr>
                    </thead>
                    <tbody>
                      {policyDetail.assignments.excluded.map((a, i) => (
                        <tr key={i}>
                          <td>{a.groupName}</td>
                          <td>{a.status}</td>
                          <td className="muted">{a.filter ?? 'None'}</td>
                          <td className="muted">{a.filterMode ?? 'None'}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            )}

            {policyDetail.assignments.included.length === 0 &&
              policyDetail.assignments.excluded.length === 0 && (
                <div className="fluent-subsection">
                  <p className="muted">No assignments configured</p>
                </div>
              )}
          </section>

          <div className="detail-divider" />

          {/* Settings section */}
          <section className="fluent-section">
            <div className="fluent-section-header">
              <strong>Settings</strong>
            </div>
            <div className="fluent-subsection">
              <div className="fluent-subheader">Configuration details</div>
              <div className="fluent-settings-searchbar">
                <input
                  className="settings-search"
                  type="search"
                  placeholder="Search settings and values"
                  value={settingsSearch}
                  onChange={(e) => setSettingsSearch(e.target.value)}
                />
                <div className="fluent-settings-search-status">
                  {settingsSearch
                    ? `Found ${totalFilteredSettings} matching settings across ${filteredGroups.length} groups.`
                    : `Showing all settings across ${policyDetail.settingGroups.length} groups.`}
                </div>
              </div>
              <div className="fluent-setting-groups">
                {filteredGroups.map((group) => (
                  <section key={group.name} className="fluent-setting-group-card">
                    <div className="fluent-setting-group-header">
                      <strong>{group.name}</strong>
                      <span className="fluent-setting-count">
                        {settingsSearch
                          ? `${group.settings.length} settings`
                          : `${group.settingCount} settings`}
                      </span>
                    </div>
                    <div className="fluent-properties fluent-setting-list">
                      {group.settings.map((setting, i) => (
                        <div
                          key={i}
                          className="fluent-property-row fluent-setting-item"
                        >
                          <div className="fluent-property-label">
                            {setting.label}
                          </div>
                          <div className="fluent-property-value">
                            {setting.value}
                          </div>
                        </div>
                      ))}
                    </div>
                  </section>
                ))}
                {filteredGroups.length === 0 && settingsSearch && (
                  <p className="muted">No settings match "{settingsSearch}".</p>
                )}
              </div>
            </div>
          </section>
        </div>
      </div>
    </div>
  );
}
