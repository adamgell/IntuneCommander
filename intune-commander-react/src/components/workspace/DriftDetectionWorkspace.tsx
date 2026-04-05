import { useMemo } from 'react';
import { useDriftDetectionStore } from '../../store/driftDetectionStore';
import type { DriftChange, DriftSeverity } from '../../types/driftDetection';
import '../../styles/workspace.css';

const severityColor: Record<DriftSeverity, string> = {
  Critical: 'var(--danger, #ef4444)',
  High: 'var(--warning, #f59e0b)',
  Medium: 'var(--brand, #3b82f6)',
  Low: 'var(--text-tertiary)',
};

const changeTypeIcon: Record<string, string> = {
  Added: '+',
  Modified: '~',
  Deleted: '-',
};

export function DriftDetectionWorkspace() {
  const {
    baselinePath, currentPath, report, isComparing, error,
    severityFilter, objectTypeFilter,
    pickBaselineFolder, pickCurrentFolder, runComparison,
    setSeverityFilter, setObjectTypeFilter, clearReport,
  } = useDriftDetectionStore();

  const objectTypes = useMemo(() => {
    if (!report) return [];
    return [...new Set(report.changes.map(c => c.objectType))].sort();
  }, [report]);

  const filteredChanges = useMemo(() => {
    if (!report) return [];
    return report.changes.filter(c => {
      if (severityFilter && c.severity !== severityFilter) return false;
      if (objectTypeFilter && c.objectType !== objectTypeFilter) return false;
      return true;
    });
  }, [report, severityFilter, objectTypeFilter]);

  const canCompare = baselinePath && currentPath && !isComparing;

  return (
    <div className="workspace">
      <div className="workspace-toolbar">
        <div className="workspace-heading">
          <strong className="workspace-title">Drift Detection</strong>
          <div className="workspace-stats">
            {report ? (
              <>
                <span className="inline-stat" style={{ color: severityColor.Critical }}>
                  {report.summary.critical} critical
                </span>
                <span className="inline-stat" style={{ color: severityColor.High }}>
                  {report.summary.high} high
                </span>
                <span className="inline-stat" style={{ color: severityColor.Medium }}>
                  {report.summary.medium} medium
                </span>
                <span className="inline-stat">
                  {report.summary.low} low
                </span>
              </>
            ) : (
              <span className="inline-stat" style={{ color: 'var(--text-secondary)' }}>
                Compare two export folders to detect configuration drift
              </span>
            )}
          </div>
        </div>
        <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
          {report && (
            <button className="ws-btn" onClick={clearReport}>Clear</button>
          )}
        </div>
      </div>

      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', overflow: 'auto', padding: '0 16px 16px' }}>
        {/* Folder Pickers */}
        <div style={{ display: 'flex', gap: 16, marginBottom: 16 }}>
          <div style={{ flex: 1 }}>
            <label style={{ fontSize: 11, color: 'var(--text-secondary)', marginBottom: 4, display: 'block' }}>Baseline Folder</label>
            <div style={{ display: 'flex', gap: 8 }}>
              <button className="ws-btn" onClick={pickBaselineFolder}>Browse...</button>
              <span style={{ fontSize: 12, color: 'var(--text-tertiary)', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', flex: 1, lineHeight: '32px' }}>
                {baselinePath ?? 'No folder selected'}
              </span>
            </div>
          </div>
          <div style={{ flex: 1 }}>
            <label style={{ fontSize: 11, color: 'var(--text-secondary)', marginBottom: 4, display: 'block' }}>Current Folder</label>
            <div style={{ display: 'flex', gap: 8 }}>
              <button className="ws-btn" onClick={pickCurrentFolder}>Browse...</button>
              <span style={{ fontSize: 12, color: 'var(--text-tertiary)', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', flex: 1, lineHeight: '32px' }}>
                {currentPath ?? 'No folder selected'}
              </span>
            </div>
          </div>
          <div style={{ display: 'flex', alignItems: 'flex-end' }}>
            <button
              className="ws-btn primary"
              disabled={!canCompare}
              onClick={runComparison}
            >
              {isComparing ? 'Comparing...' : 'Compare'}
            </button>
          </div>
        </div>

        {error && (
          <div style={{ color: 'var(--danger, #ef4444)', fontSize: 13, marginBottom: 12, padding: '8px 12px', background: 'var(--danger-bg, rgba(239,68,68,0.1))', borderRadius: 6 }}>
            {error}
          </div>
        )}

        {/* Filters */}
        {report && report.changes.length > 0 && (
          <div style={{ display: 'flex', gap: 6, marginBottom: 12, flexWrap: 'wrap' }}>
            <button
              className={`platform-chip${!severityFilter ? ' active' : ''}`}
              onClick={() => setSeverityFilter(null)}
            >All</button>
            {(['Critical', 'High', 'Medium', 'Low'] as DriftSeverity[]).map(sev => (
              <button
                key={sev}
                className={`platform-chip${severityFilter === sev ? ' active' : ''}`}
                style={{ color: severityColor[sev] }}
                onClick={() => setSeverityFilter(severityFilter === sev ? null : sev)}
              >{sev}</button>
            ))}
            <span style={{ width: 1, background: 'var(--border)', margin: '0 4px' }} />
            <button
              className={`platform-chip${!objectTypeFilter ? ' active' : ''}`}
              onClick={() => setObjectTypeFilter(null)}
            >All Types</button>
            {objectTypes.map(type => (
              <button
                key={type}
                className={`platform-chip${objectTypeFilter === type ? ' active' : ''}`}
                onClick={() => setObjectTypeFilter(objectTypeFilter === type ? null : type)}
              >{type}</button>
            ))}
          </div>
        )}

        {/* Results */}
        {report && !report.driftDetected && (
          <div style={{ textAlign: 'center', color: 'var(--success, #22c55e)', fontSize: 14, padding: 32 }}>
            No drift detected — configurations match.
          </div>
        )}

        {filteredChanges.map((change, i) => (
          <DriftChangeCard key={`${change.objectType}-${change.name}-${i}`} change={change} />
        ))}
      </div>

      <div className="workspace-footer">
        {report && (
          <span style={{ fontSize: 11, color: 'var(--text-tertiary)' }}>
            Scanned at {new Date(report.scanTime).toLocaleString()} — {report.changes.length} changes found
          </span>
        )}
      </div>
    </div>
  );
}

function DriftChangeCard({ change }: { change: DriftChange }) {
  const color = severityColor[change.severity];
  return (
    <div style={{
      border: '1px solid var(--border)',
      borderRadius: 6,
      marginBottom: 8,
      overflow: 'hidden',
    }}>
      <div style={{
        display: 'flex',
        alignItems: 'center',
        gap: 8,
        padding: '8px 12px',
        background: 'var(--surface-secondary)',
      }}>
        <span style={{
          fontFamily: 'monospace',
          fontWeight: 700,
          fontSize: 14,
          color: change.changeType === 'Deleted' ? 'var(--danger)' : change.changeType === 'Added' ? 'var(--success)' : 'var(--warning)',
          width: 18,
          textAlign: 'center',
        }}>
          {changeTypeIcon[change.changeType] ?? '?'}
        </span>
        <span style={{ fontSize: 13, fontWeight: 600, color: 'var(--text-primary)', flex: 1 }}>
          {change.name}
        </span>
        <span style={{ fontSize: 11, color: 'var(--text-tertiary)' }}>{change.objectType}</span>
        <span style={{
          fontSize: 11, fontWeight: 600, color,
          padding: '1px 6px', borderRadius: 3,
          backgroundColor: `${color}15`,
        }}>{change.severity}</span>
      </div>
      {change.fields.length > 0 && (
        <div style={{ padding: '4px 12px 8px', fontSize: 12 }}>
          {change.fields.map((f, j) => (
            <div key={j} style={{ display: 'flex', gap: 8, padding: '3px 0', borderTop: j > 0 ? '1px solid var(--border)' : undefined }}>
              <span style={{ fontFamily: 'monospace', color: 'var(--text-secondary)', minWidth: 180 }}>{f.path}</span>
              <span style={{ color: 'var(--danger, #ef4444)' }}>{formatValue(f.baseline)}</span>
              <span style={{ color: 'var(--text-tertiary)' }}>&rarr;</span>
              <span style={{ color: 'var(--success, #22c55e)' }}>{formatValue(f.current)}</span>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

function formatValue(val: unknown): string {
  if (val === null || val === undefined) return '(none)';
  if (typeof val === 'string') return val;
  return JSON.stringify(val);
}
