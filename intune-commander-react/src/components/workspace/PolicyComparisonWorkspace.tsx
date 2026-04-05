import { useEffect, useRef, useCallback, useState } from 'react';
import { usePolicyComparisonStore } from '../../store/policyComparisonStore';
import { DiffEditor, type DiffOnMount } from '@monaco-editor/react';
import type { PolicyCategory } from '../../types/policyComparison';
import type * as Monaco from 'monaco-editor';
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

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const diffNavRef = useRef<any>(null);
  const diffEditorRef = useRef<Monaco.editor.IStandaloneDiffEditor | null>(null);
  const [diffCount, setDiffCount] = useState(0);

  // Load policies on mount
  useEffect(() => {
    void loadPolicies();
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  // Update diff count when comparison result changes
  useEffect(() => {
    setDiffCount(0);
    diffNavRef.current = null;
  }, [comparisonResult]);

  const handleDiffMount: DiffOnMount = useCallback((editor, monaco) => {
    diffEditorRef.current = editor;

    // Wait for diff computation to complete, then count changes
    const modifiedEditor = editor.getModifiedEditor();
    const checkDiffs = () => {
      const changes = editor.getLineChanges();
      if (changes) {
        setDiffCount(changes.length);
        // Create diff navigator for back/forward
        diffNavRef.current = monaco.editor.createDiffNavigator(editor, {
          followsCaret: true,
          ignoreCharChanges: false,
        });
      }
    };

    // Check initially and on model content changes
    modifiedEditor.onDidChangeModelContent(checkDiffs);
    // Also check after a short delay to catch initial diff computation
    setTimeout(checkDiffs, 500);
  }, []);

  const goToNextDiff = useCallback(() => {
    if (diffNavRef.current) {
      diffNavRef.current.next();
    }
  }, []);

  const goToPrevDiff = useCallback(() => {
    if (diffNavRef.current) {
      diffNavRef.current.previous();
    }
  }, []);

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

      {/* Comparison summary + diff navigation */}
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

          {/* Diff navigation controls */}
          {diffCount > 0 && (
            <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
              <button
                className="btn btn-ghost"
                onClick={goToPrevDiff}
                disabled={diffCount === 0}
                style={{ padding: '4px 8px', fontSize: 12, minWidth: 0 }}
                title="Previous difference"
              >
                &larr; Prev
              </button>
              <span style={{ fontSize: 11, color: 'var(--text-tertiary)', minWidth: 50, textAlign: 'center' }}>
                {diffCount} {diffCount === 1 ? 'change' : 'changes'}
              </span>
              <button
                className="btn btn-ghost"
                onClick={goToNextDiff}
                disabled={diffCount === 0}
                style={{ padding: '4px 8px', fontSize: 12, minWidth: 0 }}
                title="Next difference"
              >
                Next &rarr;
              </button>
            </div>
          )}
        </div>
      )}

      {/* Monaco diff editor */}
      {comparisonResult && (
        <div style={{
          flex: 1,
          border: '1px solid var(--border)',
          borderRadius: 8,
          overflow: 'hidden',
          minHeight: 400,
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
        {comparisonResult && <span>{comparisonResult.differingProperties} settings differences found</span>}
        {isComparing && <span>Comparing policies...</span>}
      </div>
    </div>
  );
}
