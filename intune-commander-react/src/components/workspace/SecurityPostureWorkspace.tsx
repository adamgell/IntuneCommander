import { useEffect, useMemo } from 'react';
import { useSecurityPostureStore } from '../../store/securityPostureStore';
import type { ScoreCategory, SecurityGap } from '../../types/securityPosture';
import '../../styles/workspace.css';

function ScoreRing({ score }: { score: number }) {
  const radius = 54;
  const circumference = 2 * Math.PI * radius;
  const offset = circumference - (score / 100) * circumference;
  const color =
    score >= 70 ? 'var(--success, #22c55e)' :
    score >= 40 ? 'var(--warning, #f59e0b)' :
    'var(--danger, #ef4444)';

  return (
    <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 8 }}>
      <svg width="140" height="140" viewBox="0 0 140 140">
        <circle cx="70" cy="70" r={radius} fill="none" stroke="var(--border)" strokeWidth="10" />
        <circle
          cx="70" cy="70" r={radius} fill="none"
          stroke={color} strokeWidth="10"
          strokeDasharray={circumference}
          strokeDashoffset={offset}
          strokeLinecap="round"
          transform="rotate(-90 70 70)"
          style={{ transition: 'stroke-dashoffset 0.8s ease' }}
        />
        <text x="70" y="65" textAnchor="middle" fill={color} fontSize="32" fontWeight="700">
          {score}
        </text>
        <text x="70" y="85" textAnchor="middle" fill="var(--text-tertiary)" fontSize="12">
          / 100
        </text>
      </svg>
      <span style={{ fontSize: 13, color: 'var(--text-secondary)', fontWeight: 600 }}>
        Security Score
      </span>
    </div>
  );
}

function CategoryBar({ category }: { category: ScoreCategory }) {
  const pct = category.maxScore > 0 ? (category.score / category.maxScore) * 100 : 0;
  const color =
    pct >= 70 ? 'var(--success, #22c55e)' :
    pct >= 40 ? 'var(--warning, #f59e0b)' :
    'var(--danger, #ef4444)';

  return (
    <div style={{ marginBottom: 16 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 4 }}>
        <span style={{ fontSize: 13, fontWeight: 600, color: 'var(--text-primary)' }}>
          {category.category}
        </span>
        <span style={{ fontSize: 12, color: 'var(--text-tertiary)' }}>
          {category.score} / {category.maxScore}
        </span>
      </div>
      <div style={{ height: 8, borderRadius: 4, backgroundColor: 'var(--surface-secondary, #1f2937)', overflow: 'hidden' }}>
        <div style={{ height: '100%', width: `${pct}%`, backgroundColor: color, borderRadius: 4, transition: 'width 0.6s ease' }} />
      </div>
      <div style={{ display: 'flex', gap: 8, marginTop: 4, flexWrap: 'wrap' }}>
        {category.items.map((item, i) => (
          <span key={i} style={{ fontSize: 11, color: 'var(--text-tertiary)' }}>{item}</span>
        ))}
      </div>
    </div>
  );
}

function GapCard({ gap }: { gap: SecurityGap }) {
  const colors: Record<string, { bg: string; border: string; text: string }> = {
    high: { bg: 'rgba(239,68,68,0.08)', border: 'rgba(239,68,68,0.3)', text: '#ef4444' },
    medium: { bg: 'rgba(245,158,11,0.08)', border: 'rgba(245,158,11,0.3)', text: '#f59e0b' },
    low: { bg: 'rgba(59,130,246,0.08)', border: 'rgba(59,130,246,0.3)', text: '#3b82f6' },
  };
  const c = colors[gap.severity] ?? colors.low;

  return (
    <div style={{
      padding: '10px 14px',
      borderRadius: 8,
      backgroundColor: c.bg,
      border: `1px solid ${c.border}`,
      display: 'flex',
      gap: 10,
      alignItems: 'flex-start',
    }}>
      <span style={{
        fontSize: 10, fontWeight: 700, textTransform: 'uppercase',
        color: c.text, padding: '2px 6px', borderRadius: 4,
        backgroundColor: `${c.text}15`, flexShrink: 0,
      }}>
        {gap.severity}
      </span>
      <div>
        <div style={{ fontSize: 12, fontWeight: 600, color: 'var(--text-primary)', marginBottom: 2 }}>
          {gap.category}
        </div>
        <div style={{ fontSize: 12, color: 'var(--text-secondary)' }}>
          {gap.description}
        </div>
      </div>
    </div>
  );
}

function StatCard({ label, value, sub, color }: { label: string; value: number | string; sub?: string; color?: string }) {
  return (
    <div style={{
      flex: '1 1 180px',
      padding: '16px 20px',
      borderRadius: 10,
      backgroundColor: 'var(--surface-secondary, #111827)',
      border: '1px solid var(--border)',
    }}>
      <div style={{ fontSize: 11, color: 'var(--text-tertiary)', textTransform: 'uppercase', letterSpacing: '0.06em', marginBottom: 6 }}>
        {label}
      </div>
      <div style={{ fontSize: 28, fontWeight: 700, color: color ?? 'var(--text-primary)' }}>
        {value}
      </div>
      {sub && <div style={{ fontSize: 12, color: 'var(--text-tertiary)', marginTop: 2 }}>{sub}</div>}
    </div>
  );
}

export function SecurityPostureWorkspace() {
  const summary = useSecurityPostureStore((s) => s.summary);
  const detail = useSecurityPostureStore((s) => s.detail);
  const isLoadingSummary = useSecurityPostureStore((s) => s.isLoadingSummary);
  const hasAttemptedLoad = useSecurityPostureStore((s) => s.hasAttemptedLoad);
  const error = useSecurityPostureStore((s) => s.error);
  const loadSummary = useSecurityPostureStore((s) => s.loadSummary);
  const loadDetail = useSecurityPostureStore((s) => s.loadDetail);

  useEffect(() => {
    if (!hasAttemptedLoad && !isLoadingSummary) {
      void loadSummary();
      void loadDetail();
    }
  }, [hasAttemptedLoad, isLoadingSummary, loadSummary, loadDetail]);

  const sortedGaps = useMemo(() => {
    if (!summary) return [];
    const order: Record<string, number> = { high: 0, medium: 1, low: 2 };
    return [...summary.gaps].sort((a, b) => (order[a.severity] ?? 3) - (order[b.severity] ?? 3));
  }, [summary]);

  if (isLoadingSummary && !summary) {
    return (
      <div className="workspace">
        <div className="workspace-toolbar">
          <div className="workspace-heading">
            <strong className="workspace-title">Security Posture</strong>
          </div>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: 300, color: 'var(--text-tertiary)' }}>
          Loading security posture data...
        </div>
      </div>
    );
  }

  return (
    <div className="workspace">
      <div className="workspace-toolbar">
        <div className="workspace-heading">
          <strong className="workspace-title">Security Posture</strong>
          <div className="workspace-stats">
            <span className="inline-stat" style={{ color: 'var(--text-secondary)' }}>
              Cross-cutting security overview
            </span>
          </div>
        </div>
        <div className="workspace-actions">
          <button
            className="ws-btn primary"
            onClick={() => { void loadSummary(); void loadDetail(); }}
            disabled={isLoadingSummary}
          >
            Refresh
          </button>
        </div>
      </div>

      {error && <div className="workspace-error">{error}</div>}

      {summary && (
        <div style={{ display: 'flex', gap: 24, padding: '0 4px', flexWrap: 'wrap', overflow: 'auto', flex: 1 }}>
          {/* Left column: Score + Breakdown */}
          <div style={{ flex: '0 0 320px', display: 'flex', flexDirection: 'column', gap: 20 }}>
            <div style={{
              padding: 24, borderRadius: 12,
              backgroundColor: 'var(--surface-secondary, #111827)',
              border: '1px solid var(--border)',
              display: 'flex', justifyContent: 'center',
            }}>
              <ScoreRing score={summary.securityScore} />
            </div>

            <div style={{
              padding: '16px 20px', borderRadius: 12,
              backgroundColor: 'var(--surface-secondary, #111827)',
              border: '1px solid var(--border)',
            }}>
              <div style={{ fontSize: 13, fontWeight: 600, color: 'var(--text-primary)', marginBottom: 16 }}>
                Score Breakdown
              </div>
              {summary.scoreBreakdown.map((cat) => (
                <CategoryBar key={cat.category} category={cat} />
              ))}
            </div>
          </div>

          {/* Right column: Stats + Gaps + Detail tables */}
          <div style={{ flex: 1, minWidth: 400, display: 'flex', flexDirection: 'column', gap: 20 }}>
            {/* Stat cards row */}
            <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap' }}>
              <StatCard
                label="CA Policies"
                value={summary.caTotal}
                sub={`${summary.caEnabled} enabled, ${summary.caReportOnly} report-only`}
              />
              <StatCard
                label="Compliance"
                value={summary.compliancePolicies}
                sub={summary.compliancePlatforms.join(', ') || 'No platforms'}
              />
              <StatCard
                label="Endpoint Security"
                value={summary.endpointSecurityIntents}
              />
              <StatCard
                label="App Protection"
                value={summary.appProtectionPolicies}
              />
              <StatCard
                label="Auth Strengths"
                value={summary.authStrengthPolicies}
              />
              <StatCard
                label="Named Locations"
                value={summary.namedLocations}
              />
            </div>

            {/* Security gaps */}
            {sortedGaps.length > 0 && (
              <div style={{
                padding: '16px 20px', borderRadius: 12,
                backgroundColor: 'var(--surface-secondary, #111827)',
                border: '1px solid var(--border)',
              }}>
                <div style={{ fontSize: 13, fontWeight: 600, color: 'var(--text-primary)', marginBottom: 12 }}>
                  Security Gaps ({sortedGaps.length})
                </div>
                <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
                  {sortedGaps.map((gap, i) => (
                    <GapCard key={i} gap={gap} />
                  ))}
                </div>
              </div>
            )}

            {/* CA Policy summary table */}
            {detail && detail.conditionalAccessPolicies.length > 0 && (
              <div style={{
                padding: '16px 20px', borderRadius: 12,
                backgroundColor: 'var(--surface-secondary, #111827)',
                border: '1px solid var(--border)',
              }}>
                <div style={{ fontSize: 13, fontWeight: 600, color: 'var(--text-primary)', marginBottom: 12 }}>
                  Conditional Access Policies
                </div>
                <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12 }}>
                  <thead>
                    <tr style={{ borderBottom: '1px solid var(--border)' }}>
                      <th style={{ textAlign: 'left', padding: '6px 8px', color: 'var(--text-tertiary)', fontWeight: 600, fontSize: 11, textTransform: 'uppercase' }}>Name</th>
                      <th style={{ textAlign: 'left', padding: '6px 8px', color: 'var(--text-tertiary)', fontWeight: 600, fontSize: 11, textTransform: 'uppercase' }}>State</th>
                    </tr>
                  </thead>
                  <tbody>
                    {detail.conditionalAccessPolicies.map((p) => (
                      <tr key={p.id} style={{ borderBottom: '1px solid var(--border)' }}>
                        <td style={{ padding: '6px 8px', color: 'var(--text-secondary)' }}>{p.displayName}</td>
                        <td style={{ padding: '6px 8px' }}>
                          <StateChip state={p.state} />
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}

            {/* Compliance Policies */}
            {detail && detail.compliancePolicies.length > 0 && (
              <div style={{
                padding: '16px 20px', borderRadius: 12,
                backgroundColor: 'var(--surface-secondary, #111827)',
                border: '1px solid var(--border)',
              }}>
                <div style={{ fontSize: 13, fontWeight: 600, color: 'var(--text-primary)', marginBottom: 12 }}>
                  Compliance Policies
                </div>
                <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 12 }}>
                  <thead>
                    <tr style={{ borderBottom: '1px solid var(--border)' }}>
                      <th style={{ textAlign: 'left', padding: '6px 8px', color: 'var(--text-tertiary)', fontWeight: 600, fontSize: 11, textTransform: 'uppercase' }}>Name</th>
                      <th style={{ textAlign: 'left', padding: '6px 8px', color: 'var(--text-tertiary)', fontWeight: 600, fontSize: 11, textTransform: 'uppercase' }}>Platform</th>
                    </tr>
                  </thead>
                  <tbody>
                    {detail.compliancePolicies.map((p) => (
                      <tr key={p.id} style={{ borderBottom: '1px solid var(--border)' }}>
                        <td style={{ padding: '6px 8px', color: 'var(--text-secondary)' }}>{p.displayName}</td>
                        <td style={{ padding: '6px 8px', color: 'var(--text-tertiary)' }}>{p.platform}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </div>
      )}

      <div className="workspace-footer">
        {summary && <span>Security score: {summary.securityScore}/100</span>}
        {isLoadingSummary && <span>Refreshing...</span>}
      </div>
    </div>
  );
}

function StateChip({ state }: { state: string }) {
  let color = 'var(--text-tertiary)';
  let label = state;
  if (state === 'Enabled' || state === 'enabled') {
    color = 'var(--success, #22c55e)';
    label = 'Enabled';
  } else if (state.includes('Report') || state.includes('report')) {
    color = 'var(--warning, #f59e0b)';
    label = 'Report-only';
  } else if (state === 'Disabled' || state === 'disabled') {
    color = 'var(--text-muted, #6b7280)';
    label = 'Disabled';
  }
  return (
    <span style={{
      display: 'inline-block', padding: '2px 8px', borderRadius: 10, fontSize: 11,
      fontWeight: 600, backgroundColor: `${color}20`, color, border: `1px solid ${color}40`,
    }}>
      {label}
    </span>
  );
}
