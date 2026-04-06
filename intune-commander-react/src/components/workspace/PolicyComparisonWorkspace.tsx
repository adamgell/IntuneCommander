import { useEffect, useRef, useCallback, useState, useMemo } from 'react';
import { usePolicyComparisonStore } from '../../store/policyComparisonStore';
import { DiffEditor, type DiffOnMount } from '@monaco-editor/react';
import type { PolicyCategory, SettingDiffItem, DiffStatus } from '../../types/policyComparison';
import '../../styles/workspace.css';

const categories: { label: string; value: PolicyCategory }[] = [
  { label: 'Settings Catalog', value: 'settingsCatalog' },
  { label: 'Compliance', value: 'compliance' },
  { label: 'Device Configuration', value: 'deviceConfiguration' },
  { label: 'Conditional Access', value: 'conditionalAccess' },
  { label: 'App Protection', value: 'appProtection' },
  { label: 'Endpoint Security', value: 'endpointSecurity' },
];

type ViewMode = 'report' | 'json';

const statusColor: Record<DiffStatus, string> = {
  same: 'var(--success, #22c55e)',
  changed: 'var(--warning, #f59e0b)',
  onlyA: 'var(--error, #ef4444)',
  onlyB: 'var(--brand, #a78bfa)',
};

const statusLabel: Record<DiffStatus, string> = {
  same: 'Same',
  changed: 'Changed',
  onlyA: 'Only in A',
  onlyB: 'Only in B',
};

function PolicySelector({ label, selectedId, onSelect }: {
  label: string;
  selectedId: string | null;
  onSelect: (id: string | null) => void;
}) {
  const policies = usePolicyComparisonStore((s) => s.policies);
  const isLoading = usePolicyComparisonStore((s) => s.isLoadingPolicies);

  return (
    <div style={{ flex: 1, minWidth: 200 }}>
      <label style={{ fontSize: 11, fontWeight: 600, color: 'var(--text-tertiary)', textTransform: 'uppercase', letterSpacing: '0.06em', marginBottom: 4, display: 'block' }}>
        {label}
      </label>
      <select
        value={selectedId ?? ''}
        onChange={(e) => onSelect(e.target.value || null)}
        disabled={isLoading || policies.length === 0}
        style={{
          width: '100%', padding: '8px 12px', borderRadius: 8,
          border: '1px solid var(--border)',
          backgroundColor: 'var(--surface-secondary, #1f2937)',
          color: 'var(--text-primary)', fontSize: 13,
        }}
      >
        <option value="">Select a policy...</option>
        {policies.map((p) => (
          <option key={p.id} value={p.id}>{p.displayName}</option>
        ))}
      </select>
    </div>
  );
}

export function PolicyComparisonWorkspace() {
  const category = usePolicyComparisonStore((s) => s.category);
  const policyAId = usePolicyComparisonStore((s) => s.policyAId);
  const policyBId = usePolicyComparisonStore((s) => s.policyBId);
  const comparisonResult = usePolicyComparisonStore((s) => s.comparisonResult);
  const isComparing = usePolicyComparisonStore((s) => s.isComparing);
  const isLoadingPolicies = usePolicyComparisonStore((s) => s.isLoadingPolicies);
  const error = usePolicyComparisonStore((s) => s.error);
  const setCategory = usePolicyComparisonStore((s) => s.setCategory);
  const setPolicyA = usePolicyComparisonStore((s) => s.setPolicyA);
  const setPolicyB = usePolicyComparisonStore((s) => s.setPolicyB);
  const compare = usePolicyComparisonStore((s) => s.compare);
  const loadPolicies = usePolicyComparisonStore((s) => s.loadPolicies);

  const hasReport = comparisonResult?.settingsDiff != null;
  const [viewMode, setViewMode] = useState<ViewMode>('report');
  const [diffFilter, setDiffFilter] = useState<DiffStatus | null>(null);
  const [currentDiffIdx, setCurrentDiffIdx] = useState(0);
  const diffRowRefs = useRef<Map<number, HTMLTableRowElement>>(new Map());

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const diffNavRef = useRef<any>(null);
  const [diffCount, setDiffCount] = useState(0);

  useEffect(() => { void loadPolicies(); }, []); // eslint-disable-line react-hooks/exhaustive-deps

  // Reset view when comparison changes
  useEffect(() => {
    setDiffFilter(null);
    setCurrentDiffIdx(0);
    setDiffCount(0);
    diffNavRef.current = null;
    if (comparisonResult?.settingsDiff) setViewMode('report');
    else setViewMode('json');
  }, [comparisonResult]);

  // Filtered diff items (report view)
  const filteredDiff = useMemo(() => {
    if (!comparisonResult?.settingsDiff) return [];
    if (!diffFilter) return comparisonResult.settingsDiff.filter(d => d.status !== 'same');
    return comparisonResult.settingsDiff.filter(d => d.status === diffFilter);
  }, [comparisonResult, diffFilter]);

  const diffStats = useMemo(() => {
    const items = comparisonResult?.settingsDiff ?? [];
    return {
      same: items.filter(d => d.status === 'same').length,
      changed: items.filter(d => d.status === 'changed').length,
      onlyA: items.filter(d => d.status === 'onlyA').length,
      onlyB: items.filter(d => d.status === 'onlyB').length,
      total: items.length,
    };
  }, [comparisonResult]);

  // Navigation in report view
  const goToNextReportDiff = useCallback(() => {
    setCurrentDiffIdx(prev => {
      const next = Math.min(prev + 1, filteredDiff.length - 1);
      diffRowRefs.current.get(next)?.scrollIntoView({ behavior: 'smooth', block: 'center' });
      return next;
    });
  }, [filteredDiff.length]);

  const goToPrevReportDiff = useCallback(() => {
    setCurrentDiffIdx(prev => {
      const next = Math.max(prev - 1, 0);
      diffRowRefs.current.get(next)?.scrollIntoView({ behavior: 'smooth', block: 'center' });
      return next;
    });
  }, []);

  // Monaco diff editor
  const handleDiffMount: DiffOnMount = useCallback((editor, monaco) => {
    const checkDiffs = () => {
      const changes = editor.getLineChanges();
      if (changes) {
        setDiffCount(changes.length);
        diffNavRef.current = monaco.editor.createDiffNavigator(editor, {
          followsCaret: true, ignoreCharChanges: false,
        });
      }
    };
    editor.getModifiedEditor().onDidChangeModelContent(checkDiffs);
    setTimeout(checkDiffs, 500);
  }, []);

  const goToNextJson = useCallback(() => { diffNavRef.current?.next(); }, []);
  const goToPrevJson = useCallback(() => { diffNavRef.current?.previous(); }, []);

  return (
    <div className="workspace">
      <div className="workspace-toolbar">
        <div className="workspace-heading">
          <strong className="workspace-title">Policy Comparison</strong>
          <div className="workspace-stats">
            <span className="inline-stat" style={{ color: 'var(--text-secondary)' }}>
              Settings-only diff (metadata stripped)
            </span>
          </div>
        </div>
      </div>

      {/* Category selector */}
      <div className="platform-filter-bar">
        {categories.map((cat) => (
          <button
            key={cat.value}
            className={`platform-chip${category === cat.value ? ' active' : ''}`}
            onClick={() => setCategory(cat.value)}
          >
            {cat.label}
          </button>
        ))}
      </div>

      {/* Policy picker row */}
      <div style={{ display: 'flex', gap: 16, padding: '8px 4px', alignItems: 'flex-end', flexWrap: 'wrap' }}>
        <PolicySelector label="Policy A" selectedId={policyAId} onSelect={setPolicyA} />
        <div style={{ fontSize: 20, color: 'var(--text-tertiary)', paddingBottom: 8, fontWeight: 300 }}>vs</div>
        <PolicySelector label="Policy B" selectedId={policyBId} onSelect={setPolicyB} />
        <button
          className="ws-btn primary"
          onClick={() => void compare()}
          disabled={isComparing || !policyAId || !policyBId || policyAId === policyBId}
          style={{ marginBottom: 0 }}
        >
          {isComparing ? 'Comparing...' : 'Compare'}
        </button>
      </div>

      {error && <div className="workspace-error">{error}</div>}
      {isLoadingPolicies && (
        <div style={{ padding: 16, color: 'var(--text-tertiary)', fontSize: 13 }}>Loading policies...</div>
      )}

      {/* Comparison header: summary + view toggle + navigation */}
      {comparisonResult && (
        <div style={{
          display: 'flex', gap: 12, padding: '8px 4px', alignItems: 'center',
          borderBottom: '1px solid var(--border)', marginBottom: 8, flexWrap: 'wrap',
        }}>
          <div style={{ flex: 1 }}>
            <div style={{ display: 'flex', gap: 16, fontSize: 12, alignItems: 'center', flexWrap: 'wrap' }}>
              <span style={{ color: 'var(--text-secondary)' }}>
                <strong style={{ color: 'var(--text-primary)' }}>{comparisonResult.policyAName}</strong> vs{' '}
                <strong style={{ color: 'var(--text-primary)' }}>{comparisonResult.policyBName}</strong>
              </span>
              {hasReport && (
                <>
                  <span style={{ color: statusColor.changed, fontWeight: 600 }}>
                    {diffStats.changed} changed
                  </span>
                  <span style={{ color: statusColor.onlyA, fontWeight: 600 }}>
                    {diffStats.onlyA} only A
                  </span>
                  <span style={{ color: statusColor.onlyB, fontWeight: 600 }}>
                    {diffStats.onlyB} only B
                  </span>
                  <span style={{ color: 'var(--text-tertiary)' }}>
                    {diffStats.same} same
                  </span>
                </>
              )}
              {!hasReport && (
                <span style={{
                  color: comparisonResult.differingProperties > 0 ? 'var(--warning)' : 'var(--success)',
                  fontWeight: 600,
                }}>
                  {comparisonResult.differingProperties} differences
                </span>
              )}
            </div>
          </div>

          {/* View toggle */}
          {hasReport && (
            <div style={{ display: 'flex', gap: 2, background: 'var(--surface)', borderRadius: 8, padding: 2 }}>
              <button
                className={`ws-btn small${viewMode === 'report' ? ' primary' : ''}`}
                onClick={() => setViewMode('report')}
                style={{ borderRadius: 6 }}
              >Report</button>
              <button
                className={`ws-btn small${viewMode === 'json' ? ' primary' : ''}`}
                onClick={() => setViewMode('json')}
                style={{ borderRadius: 6 }}
              >Raw JSON</button>
            </div>
          )}

          {/* Navigation controls */}
          {viewMode === 'report' && filteredDiff.length > 0 && (
            <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
              <button className="ws-btn small" onClick={goToPrevReportDiff} title="Previous">
                &larr;
              </button>
              <span style={{ fontSize: 11, color: 'var(--text-tertiary)', minWidth: 40, textAlign: 'center' }}>
                {currentDiffIdx + 1}/{filteredDiff.length}
              </span>
              <button className="ws-btn small" onClick={goToNextReportDiff} title="Next">
                &rarr;
              </button>
            </div>
          )}
          {viewMode === 'json' && diffCount > 0 && (
            <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
              <button className="ws-btn small" onClick={goToPrevJson} title="Previous">
                &larr;
              </button>
              <span style={{ fontSize: 11, color: 'var(--text-tertiary)', minWidth: 40, textAlign: 'center' }}>
                {diffCount} changes
              </span>
              <button className="ws-btn small" onClick={goToNextJson} title="Next">
                &rarr;
              </button>
            </div>
          )}
        </div>
      )}

      {/* Report View */}
      {comparisonResult && viewMode === 'report' && hasReport && (
        <div style={{ flex: 1, overflow: 'auto', padding: '0 4px' }}>
          {/* Filter chips */}
          <div style={{ display: 'flex', gap: 6, marginBottom: 8 }}>
            <button
              className={`platform-chip${!diffFilter ? ' active' : ''}`}
              onClick={() => { setDiffFilter(null); setCurrentDiffIdx(0); }}
            >Differences ({diffStats.changed + diffStats.onlyA + diffStats.onlyB})</button>
            <button
              className={`platform-chip${diffFilter === 'changed' ? ' active' : ''}`}
              onClick={() => { setDiffFilter('changed'); setCurrentDiffIdx(0); }}
              style={{ color: statusColor.changed }}
            >Changed ({diffStats.changed})</button>
            <button
              className={`platform-chip${diffFilter === 'onlyA' ? ' active' : ''}`}
              onClick={() => { setDiffFilter('onlyA'); setCurrentDiffIdx(0); }}
              style={{ color: statusColor.onlyA }}
            >Only A ({diffStats.onlyA})</button>
            <button
              className={`platform-chip${diffFilter === 'onlyB' ? ' active' : ''}`}
              onClick={() => { setDiffFilter('onlyB'); setCurrentDiffIdx(0); }}
              style={{ color: statusColor.onlyB }}
            >Only B ({diffStats.onlyB})</button>
            <button
              className={`platform-chip${diffFilter === 'same' ? ' active' : ''}`}
              onClick={() => { setDiffFilter('same'); setCurrentDiffIdx(0); }}
            >Same ({diffStats.same})</button>
          </div>

          {/* Settings diff table */}
          <div style={{ border: '1px solid var(--border)', borderRadius: 8, overflow: 'hidden' }}>
            <table style={{ width: '100%', fontSize: 12, borderCollapse: 'collapse' }}>
              <thead>
                <tr style={{ background: 'var(--surface-alt)', position: 'sticky', top: 0, zIndex: 1 }}>
                  <th style={thStyle}>Status</th>
                  <th style={thStyle}>Category</th>
                  <th style={thStyle}>Setting</th>
                  <th style={thStyle}>Policy A</th>
                  <th style={thStyle}>Policy B</th>
                </tr>
              </thead>
              <tbody>
                {filteredDiff.map((item, i) => (
                  <SettingDiffRow
                    key={`${item.label}-${i}`}
                    item={item}
                    isActive={i === currentDiffIdx}
                    rowRef={(el) => {
                      if (el) diffRowRefs.current.set(i, el);
                      else diffRowRefs.current.delete(i);
                    }}
                  />
                ))}
                {filteredDiff.length === 0 && (
                  <tr>
                    <td colSpan={5} style={{ padding: 24, textAlign: 'center', color: 'var(--text-tertiary)' }}>
                      No differences in this filter
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Raw JSON View */}
      {comparisonResult && (viewMode === 'json' || !hasReport) && (
        <div style={{
          flex: 1, border: '1px solid var(--border)', borderRadius: 8,
          overflow: 'hidden', minHeight: 400,
        }}>
          <DiffEditor
            original={comparisonResult.normalizedJsonA}
            modified={comparisonResult.normalizedJsonB}
            language="json"
            theme="vs-dark"
            onMount={handleDiffMount}
            options={{
              readOnly: true,
              minimap: { enabled: false },
              scrollBeyondLastLine: false,
              fontSize: 12,
              lineNumbers: 'on',
              renderSideBySide: true,
              enableSplitViewResizing: true,
              wordWrap: 'on',
              scrollbar: { verticalScrollbarSize: 8 },
              padding: { top: 8, bottom: 8 },
            }}
          />
        </div>
      )}

      {/* Empty state */}
      {!comparisonResult && !isComparing && !error && (
        <div style={{
          flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center',
          color: 'var(--text-tertiary)', fontSize: 14,
        }}>
          Select two policies from the same category and click Compare
        </div>
      )}

      <div className="workspace-footer">
        {comparisonResult && hasReport && (
          <span>{diffStats.changed + diffStats.onlyA + diffStats.onlyB} settings differences found ({diffStats.total} total settings)</span>
        )}
        {comparisonResult && !hasReport && (
          <span>{comparisonResult.differingProperties} differences found</span>
        )}
        {isComparing && <span>Comparing policies...</span>}
      </div>
    </div>
  );
}

const thStyle: React.CSSProperties = {
  textAlign: 'left',
  padding: '8px 12px',
  borderBottom: '1px solid var(--border)',
  fontSize: 11,
  fontWeight: 600,
  color: 'var(--text-tertiary)',
  textTransform: 'uppercase',
  letterSpacing: '0.06em',
};

function SettingDiffRow({ item, isActive, rowRef }: {
  item: SettingDiffItem;
  isActive: boolean;
  rowRef: (el: HTMLTableRowElement | null) => void;
}) {
  const color = statusColor[item.status];
  return (
    <tr
      ref={rowRef}
      style={{
        borderBottom: '1px solid var(--border)',
        background: isActive ? 'var(--brand-soft, rgba(167,139,250,0.08))' : undefined,
      }}
    >
      <td style={{ padding: '6px 12px', width: 80 }}>
        <span style={{
          fontSize: 10, fontWeight: 600, color,
          padding: '2px 6px', borderRadius: 4,
          backgroundColor: `${color}18`,
          textTransform: 'uppercase',
        }}>
          {statusLabel[item.status]}
        </span>
      </td>
      <td style={{ padding: '6px 12px', color: 'var(--text-tertiary)', fontSize: 11, maxWidth: 160 }}>
        {item.category}
      </td>
      <td style={{ padding: '6px 12px', fontWeight: 500, color: 'var(--text-primary)' }}>
        {item.label}
      </td>
      <td style={{
        padding: '6px 12px',
        color: item.status === 'onlyB' ? 'var(--text-muted)' : 'var(--text-secondary)',
        fontFamily: "'Cascadia Code', 'Consolas', monospace",
        fontSize: 11,
        fontStyle: item.valueA == null ? 'italic' : undefined,
      }}>
        {item.valueA ?? '--'}
      </td>
      <td style={{
        padding: '6px 12px',
        color: item.status === 'onlyA' ? 'var(--text-muted)' : 'var(--text-secondary)',
        fontFamily: "'Cascadia Code', 'Consolas', monospace",
        fontSize: 11,
        fontStyle: item.valueB == null ? 'italic' : undefined,
      }}>
        {item.valueB ?? '--'}
      </td>
    </tr>
  );
}
