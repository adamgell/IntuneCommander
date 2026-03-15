import { useConditionalAccessStore } from '../../store/conditionalAccessStore';

function ConditionList({ label, items }: { label: string; items: string[] }) {
  if (items.length === 0) return null;
  return (
    <div className="fluent-property-row">
      <div className="fluent-property-label">{label}</div>
      <div className="fluent-property-value">
        <div style={{ display: 'flex', flexWrap: 'wrap', gap: 4 }}>
          {items.map((item, i) => (
            <span
              key={i}
              style={{
                display: 'inline-block',
                padding: '2px 8px',
                borderRadius: 4,
                fontSize: 11,
                backgroundColor: 'rgba(255,255,255,0.06)',
                border: '1px solid var(--border)',
              }}
            >
              {item}
            </span>
          ))}
        </div>
      </div>
    </div>
  );
}

export function CaPolicyDetailPanel() {
  const policyDetail = useConditionalAccessStore((s) => s.policyDetail);
  const isLoadingDetail = useConditionalAccessStore((s) => s.isLoadingDetail);
  const clearSelection = useConditionalAccessStore((s) => s.clearSelection);

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

  const stateColor =
    policyDetail.state === 'enabled'
      ? 'var(--success, #22c55e)'
      : policyDetail.state === 'enabledForReportingButNotEnforced'
        ? 'var(--warning, #f59e0b)'
        : 'var(--text-muted)';

  const stateLabel =
    policyDetail.state === 'enabled'
      ? 'Enabled'
      : policyDetail.state === 'enabledForReportingButNotEnforced'
        ? 'Report-only'
        : 'Disabled';

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
            <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
              <strong>{policyDetail.displayName}</strong>
              <span
                style={{
                  padding: '2px 10px',
                  borderRadius: 12,
                  fontSize: 11,
                  fontWeight: 600,
                  backgroundColor: `${stateColor}20`,
                  color: stateColor,
                  border: `1px solid ${stateColor}40`,
                }}
              >
                {stateLabel}
              </span>
            </div>
            {policyDetail.description && <span>{policyDetail.description}</span>}
          </div>

          <div className="detail-divider" />

          {/* Conditions section */}
          <section className="fluent-section">
            <div className="fluent-section-header">
              <strong>Conditions</strong>
            </div>
            <div className="fluent-subsection">
              <div className="fluent-subheader">Users and groups</div>
              <div className="fluent-properties">
                <ConditionList label="Include users" items={policyDetail.conditions.includeUsers} />
                <ConditionList label="Exclude users" items={policyDetail.conditions.excludeUsers} />
                <ConditionList label="Include groups" items={policyDetail.conditions.includeGroups} />
                <ConditionList label="Exclude groups" items={policyDetail.conditions.excludeGroups} />
              </div>
            </div>

            <div className="fluent-subsection">
              <div className="fluent-subheader">Cloud apps</div>
              <div className="fluent-properties">
                <ConditionList label="Include apps" items={policyDetail.conditions.includeApplications} />
                <ConditionList label="Exclude apps" items={policyDetail.conditions.excludeApplications} />
              </div>
            </div>

            {(policyDetail.conditions.includePlatforms.length > 0 ||
              policyDetail.conditions.excludePlatforms.length > 0) && (
              <div className="fluent-subsection">
                <div className="fluent-subheader">Platforms</div>
                <div className="fluent-properties">
                  <ConditionList label="Include" items={policyDetail.conditions.includePlatforms} />
                  <ConditionList label="Exclude" items={policyDetail.conditions.excludePlatforms} />
                </div>
              </div>
            )}

            {(policyDetail.conditions.includeLocations.length > 0 ||
              policyDetail.conditions.excludeLocations.length > 0) && (
              <div className="fluent-subsection">
                <div className="fluent-subheader">Locations</div>
                <div className="fluent-properties">
                  <ConditionList label="Include" items={policyDetail.conditions.includeLocations} />
                  <ConditionList label="Exclude" items={policyDetail.conditions.excludeLocations} />
                </div>
              </div>
            )}

            {policyDetail.conditions.clientAppTypes.length > 0 && (
              <div className="fluent-subsection">
                <div className="fluent-subheader">Client apps</div>
                <div className="fluent-properties">
                  <ConditionList label="Types" items={policyDetail.conditions.clientAppTypes} />
                </div>
              </div>
            )}

            {(policyDetail.conditions.signInRiskLevels.length > 0 ||
              policyDetail.conditions.userRiskLevels.length > 0) && (
              <div className="fluent-subsection">
                <div className="fluent-subheader">Risk levels</div>
                <div className="fluent-properties">
                  <ConditionList label="Sign-in risk" items={policyDetail.conditions.signInRiskLevels} />
                  <ConditionList label="User risk" items={policyDetail.conditions.userRiskLevels} />
                </div>
              </div>
            )}
          </section>

          <div className="detail-divider" />

          {/* Grant Controls section */}
          <section className="fluent-section">
            <div className="fluent-section-header">
              <strong>Grant Controls</strong>
            </div>
            <div className="fluent-subsection">
              <div className="fluent-properties">
                <div className="fluent-property-row">
                  <div className="fluent-property-label">Operator</div>
                  <div className="fluent-property-value">{policyDetail.grantControls.operator}</div>
                </div>
                {policyDetail.grantControls.builtInControls.length > 0 && (
                  <ConditionList label="Controls" items={policyDetail.grantControls.builtInControls} />
                )}
                {policyDetail.grantControls.authenticationStrength && (
                  <div className="fluent-property-row">
                    <div className="fluent-property-label">Auth strength</div>
                    <div className="fluent-property-value">{policyDetail.grantControls.authenticationStrength}</div>
                  </div>
                )}
              </div>
            </div>
          </section>

          <div className="detail-divider" />

          {/* Session Controls section */}
          <section className="fluent-section">
            <div className="fluent-section-header">
              <strong>Session Controls</strong>
            </div>
            <div className="fluent-subsection">
              <div className="fluent-properties">
                {policyDetail.sessionControls.signInFrequency && (
                  <div className="fluent-property-row">
                    <div className="fluent-property-label">Sign-in frequency</div>
                    <div className="fluent-property-value">{policyDetail.sessionControls.signInFrequency}</div>
                  </div>
                )}
                {policyDetail.sessionControls.persistentBrowser && (
                  <div className="fluent-property-row">
                    <div className="fluent-property-label">Persistent browser</div>
                    <div className="fluent-property-value">{policyDetail.sessionControls.persistentBrowser}</div>
                  </div>
                )}
                <div className="fluent-property-row">
                  <div className="fluent-property-label">App enforced restrictions</div>
                  <div className="fluent-property-value">
                    {policyDetail.sessionControls.applicationEnforcedRestrictions ? 'Yes' : 'No'}
                  </div>
                </div>
                <div className="fluent-property-row">
                  <div className="fluent-property-label">Cloud app security</div>
                  <div className="fluent-property-value">
                    {policyDetail.sessionControls.cloudAppSecurity ? 'Yes' : 'No'}
                  </div>
                </div>
              </div>
            </div>
          </section>

          <div className="detail-divider" />

          {/* Metadata */}
          <section className="fluent-section">
            <div className="fluent-section-header">
              <strong>Properties</strong>
            </div>
            <div className="fluent-subsection">
              <div className="fluent-properties">
                <div className="fluent-property-row">
                  <div className="fluent-property-label">ID</div>
                  <div className="fluent-property-value" style={{ fontFamily: 'monospace', fontSize: 11 }}>
                    {policyDetail.id}
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
                <div className="fluent-property-row">
                  <div className="fluent-property-label">Modified</div>
                  <div className="fluent-property-value">
                    {policyDetail.modifiedDateTime
                      ? new Date(policyDetail.modifiedDateTime).toLocaleString()
                      : ''}
                  </div>
                </div>
              </div>
            </div>
          </section>
        </div>
      </div>
    </div>
  );
}
