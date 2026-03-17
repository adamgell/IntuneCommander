import { useEffect } from 'react';
import { usePolicyComparisonStore } from '../../store/policyComparisonStore';
import { MonacoDiffViewer } from '../shared/MonacoDiffViewer';
import type { PolicyCategory } from '../../types/policyComparison';
import '../../styles/workspace.css';

const categories: { label: string; value: PolicyCategory }[] = [
  { label: 'Settings Catalog', value: 'settingsCatalog' },
  { label: 'Compliance', value: 'compliance' },
  { label: 'Device Configuration', value: 'deviceConfiguration' },
  { label: 'Conditional Access', value: 'conditionalAccess' },
  { label: 'App Protection', value: 'appProtection' },
  { label: 'Endpoint Security', value: 'endpointSecurity' },
];

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

  // Load policies on mount
  useEffect(() => {
    void loadPolicies();
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  return (
    <div className="workspace">
      <div className="workspace-toolbar">
        <div className="workspace-heading">
          <strong className="workspace-title">Policy Comparison</strong>
          <div className="workspace-stats">
            <span className="inline-stat" style={{ color: 'var(--text-secondary)' }}>
              Side-by-side policy diff with Monaco editor
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

      {/* Comparison summary */}
      {comparisonResult && (
        <div style={{
          display: 'flex', gap: 16, padding: '8px 4px', alignItems: 'center',
          borderBottom: '1px solid var(--border)', marginBottom: 8,
        }}>
          <div style={{ flex: 1 }}>
            <div style={{ display: 'flex', gap: 24, fontSize: 12 }}>
              <span style={{ color: 'var(--text-secondary)' }}>
                <strong style={{ color: 'var(--text-primary)' }}>{comparisonResult.policyAName}</strong> vs{' '}
                <strong style={{ color: 'var(--text-primary)' }}>{comparisonResult.policyBName}</strong>
              </span>
              <span style={{ color: 'var(--text-tertiary)' }}>
                {comparisonResult.totalProperties} properties
              </span>
              <span style={{
                color: comparisonResult.differingProperties > 0 ? 'var(--warning, #f59e0b)' : 'var(--success, #22c55e)',
                fontWeight: 600,
              }}>
                {comparisonResult.differingProperties} {comparisonResult.differingProperties === 1 ? 'difference' : 'differences'}
              </span>
            </div>
          </div>
        </div>
      )}

      {/* Monaco diff editor */}
      {comparisonResult && (
        <MonacoDiffViewer
          original={comparisonResult.normalizedJsonA}
          modified={comparisonResult.normalizedJsonB}
        />
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
        {comparisonResult && <span>{comparisonResult.differingProperties} differences found</span>}
        {isComparing && <span>Comparing policies...</span>}
      </div>
    </div>
  );
}
