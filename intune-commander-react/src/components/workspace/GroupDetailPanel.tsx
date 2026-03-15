import { useGroupsStore } from '../../store/groupsStore';
import type { GroupMemberInfo, GroupAssignedObject } from '../../types/groups';

function MemberTypeIcon({ type }: { type: string }) {
  const label = type === 'User' ? 'U' : type === 'Device' ? 'D' : 'G';
  const color = type === 'User' ? 'var(--brand, #3b82f6)' : type === 'Device' ? 'var(--success, #22c55e)' : 'var(--warning, #f59e0b)';
  return (
    <span style={{
      display: 'inline-flex', alignItems: 'center', justifyContent: 'center',
      width: 22, height: 22, borderRadius: 4, fontSize: 10, fontWeight: 700,
      backgroundColor: `${color}22`, color, flexShrink: 0,
    }}>
      {label}
    </span>
  );
}

function MembersTable({ members }: { members: GroupMemberInfo[] }) {
  if (members.length === 0) {
    return <p className="muted">No members found</p>;
  }

  return (
    <div className="fluent-table-wrap">
      <table className="fluent-assignment-table">
        <thead>
          <tr>
            <th style={{ width: 36 }}>Type</th>
            <th>Name</th>
            <th>Info</th>
            <th>Status</th>
          </tr>
        </thead>
        <tbody>
          {members.slice(0, 100).map((m) => (
            <tr key={m.id}>
              <td><MemberTypeIcon type={m.memberType} /></td>
              <td>{m.displayName}</td>
              <td className="muted">{m.secondaryInfo || m.tertiaryInfo || '—'}</td>
              <td>
                <span style={{
                  color: m.status === 'Enabled' || m.status === 'Managed'
                    ? 'var(--success, #22c55e)'
                    : 'var(--text-tertiary)',
                }}>
                  {m.status || '—'}
                </span>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      {members.length > 100 && (
        <div style={{ padding: '8px 12px', fontSize: 11, color: 'var(--text-tertiary)' }}>
          Showing first 100 of {members.length} members
        </div>
      )}
    </div>
  );
}

function AssignmentsTable({ assignments }: { assignments: GroupAssignedObject[] }) {
  if (assignments.length === 0) {
    return <p className="muted">No Intune objects assigned to this group</p>;
  }

  return (
    <div className="fluent-table-wrap">
      <table className="fluent-assignment-table">
        <thead>
          <tr>
            <th>Object</th>
            <th>Type</th>
            <th>Platform</th>
            <th>Intent</th>
          </tr>
        </thead>
        <tbody>
          {assignments.map((a, i) => (
            <tr key={`${a.objectId}-${i}`}>
              <td>{a.displayName}</td>
              <td className="muted">{a.objectType}</td>
              <td className="muted">{a.platform || '—'}</td>
              <td>
                <span style={{
                  color: a.isExclusion ? 'var(--error, #ef4444)' : 'var(--text-secondary)',
                }}>
                  {a.isExclusion ? `Exclude` : a.assignmentIntent || 'Include'}
                </span>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export function GroupDetailPanel() {
  const groupDetail = useGroupsStore((s) => s.groupDetail);
  const isLoadingDetail = useGroupsStore((s) => s.isLoadingDetail);
  const clearSelection = useGroupsStore((s) => s.clearSelection);

  if (isLoadingDetail || !groupDetail) {
    return (
      <div className="panel panel-detail">
        <div className="panel-header">
          <button className="back-btn" onClick={clearSelection}>
            Back to list
          </button>
        </div>
        <div className="panel-body loading-state">
          <div className="loading-spinner" />
          <p>Loading group details...</p>
        </div>
      </div>
    );
  }

  const { memberCounts } = groupDetail;

  return (
    <div className="panel panel-detail">
      <div className="panel-header">
        <div className="detail-header-row">
          <button className="back-btn" onClick={clearSelection}>
            Back to list
          </button>
        </div>
      </div>
      <div className="panel-body">
        <div className="fluent-detail-view">
          {/* Title block */}
          <div className="fluent-title-block">
            <strong>{groupDetail.displayName}</strong>
            {groupDetail.description && <span>{groupDetail.description}</span>}
          </div>

          {/* Status grid */}
          <div className="fluent-status-grid">
            <div className="fluent-status-card">
              <span className="fluent-status-number">{memberCounts.total}</span>
              <span className="fluent-status-label">Members</span>
            </div>
            <div className="fluent-status-card">
              <span className="fluent-status-number">{memberCounts.users}</span>
              <span className="fluent-status-label">Users</span>
            </div>
            <div className="fluent-status-card">
              <span className="fluent-status-number">{memberCounts.devices}</span>
              <span className="fluent-status-label">Devices</span>
            </div>
            <div className="fluent-status-card">
              <span className="fluent-status-number">{groupDetail.assignments.length}</span>
              <span className="fluent-status-label">Assignments</span>
            </div>
          </div>

          <div className="detail-divider" />

          {/* Properties section */}
          <section className="fluent-section">
            <div className="fluent-section-header">
              <strong>Properties</strong>
            </div>
            <div className="fluent-subsection">
              <div className="fluent-properties">
                <div className="fluent-property-row">
                  <div className="fluent-property-label">Group type</div>
                  <div className="fluent-property-value">{groupDetail.groupType}</div>
                </div>
                <div className="fluent-property-row">
                  <div className="fluent-property-label">Security enabled</div>
                  <div className="fluent-property-value">{groupDetail.securityEnabled ? 'Yes' : 'No'}</div>
                </div>
                <div className="fluent-property-row">
                  <div className="fluent-property-label">Mail enabled</div>
                  <div className="fluent-property-value">{groupDetail.mailEnabled ? 'Yes' : 'No'}</div>
                </div>
                {groupDetail.mail && (
                  <div className="fluent-property-row">
                    <div className="fluent-property-label">Mail</div>
                    <div className="fluent-property-value">{groupDetail.mail}</div>
                  </div>
                )}
                {groupDetail.membershipRuleProcessingState && (
                  <div className="fluent-property-row">
                    <div className="fluent-property-label">Rule processing</div>
                    <div className="fluent-property-value">
                      <span style={{
                        color: groupDetail.membershipRuleProcessingState === 'On'
                          ? 'var(--success, #22c55e)' : 'var(--text-tertiary)',
                      }}>
                        {groupDetail.membershipRuleProcessingState}
                      </span>
                    </div>
                  </div>
                )}
                <div className="fluent-property-row">
                  <div className="fluent-property-label">Created</div>
                  <div className="fluent-property-value">
                    {groupDetail.createdDateTime
                      ? new Date(groupDetail.createdDateTime).toLocaleString()
                      : ''}
                  </div>
                </div>
              </div>
            </div>
          </section>

          {/* Membership rule */}
          {groupDetail.membershipRule && (
            <>
              <div className="detail-divider" />
              <section className="fluent-section">
                <div className="fluent-section-header">
                  <strong>Dynamic Membership Rule</strong>
                </div>
                <div className="fluent-subsection">
                  <pre style={{
                    backgroundColor: 'var(--surface-secondary, #1f2937)',
                    border: '1px solid var(--border)',
                    borderRadius: 8,
                    padding: 12,
                    fontSize: 12,
                    fontFamily: "'Cascadia Code', 'Fira Code', 'Consolas', monospace",
                    color: 'var(--text-primary)',
                    whiteSpace: 'pre-wrap',
                    wordBreak: 'break-word',
                    lineHeight: 1.5,
                    margin: 0,
                    overflowX: 'auto',
                  }}>
                    {groupDetail.membershipRule}
                  </pre>
                </div>
              </section>
            </>
          )}

          <div className="detail-divider" />

          {/* Members section */}
          <section className="fluent-section">
            <div className="fluent-section-header">
              <strong>Members ({memberCounts.total})</strong>
            </div>
            <div className="fluent-subsection">
              {memberCounts.nestedGroups > 0 && (
                <div style={{ fontSize: 11, color: 'var(--text-tertiary)', marginBottom: 8 }}>
                  {memberCounts.users} users, {memberCounts.devices} devices, {memberCounts.nestedGroups} nested groups
                </div>
              )}
              <MembersTable members={groupDetail.members} />
            </div>
          </section>

          <div className="detail-divider" />

          {/* Assignments section */}
          <section className="fluent-section">
            <div className="fluent-section-header">
              <strong>Intune Assignments ({groupDetail.assignments.length})</strong>
            </div>
            <div className="fluent-subsection">
              <AssignmentsTable assignments={groupDetail.assignments} />
            </div>
          </section>
        </div>
      </div>
    </div>
  );
}
