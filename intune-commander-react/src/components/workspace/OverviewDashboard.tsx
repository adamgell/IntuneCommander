import { useCallback, useEffect, useMemo, useState } from 'react';
import Box from '@mui/material/Box';
import Paper from '@mui/material/Paper';
import Typography from '@mui/material/Typography';
import Chip from '@mui/material/Chip';
import LinearProgress from '@mui/material/LinearProgress';
import Button from '@mui/material/Button';
import CircularProgress from '@mui/material/CircularProgress';
import SyncIcon from '@mui/icons-material/Sync';
import { PieChart } from '@mui/x-charts/PieChart';
import PolicyIcon from '@mui/icons-material/Policy';
import SecurityIcon from '@mui/icons-material/Security';
import AppsIcon from '@mui/icons-material/Apps';
import TerminalIcon from '@mui/icons-material/Terminal';
import DevicesIcon from '@mui/icons-material/Devices';
import AppRegistrationIcon from '@mui/icons-material/AppRegistration';
import SystemUpdateIcon from '@mui/icons-material/SystemUpdate';
import MoreHorizIcon from '@mui/icons-material/MoreHoriz';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import ErrorOutlineIcon from '@mui/icons-material/ErrorOutline';
import ReplayIcon from '@mui/icons-material/Replay';
import GppGoodIcon from '@mui/icons-material/GppGood';
import GppBadIcon from '@mui/icons-material/GppBad';
import HelpOutlineIcon from '@mui/icons-material/HelpOutline';
import TimerIcon from '@mui/icons-material/Timer';
import { useAppStore } from '../../store/appStore';
import { sendCommand } from '../../bridge/bridgeClient';
import { useCacheSyncStore } from '../../store/cacheSyncStore';
import { useSettingsCatalogStore } from '../../store/settingsCatalogStore';
import { useDetectionRemediationStore } from '../../store/detectionRemediationStore';

/* ── Helpers ──────────────────────────────────────────── */

interface CacheCount {
  label: string;
  keys: string[];
  icon: React.ReactNode;
  color: string;
}

const CATEGORIES: CacheCount[] = [
  { label: 'Policies', keys: ['SettingsCatalog', 'DeviceConfigurations', 'CompliancePolicies', 'AdministrativeTemplates'], icon: <PolicyIcon />, color: '#60a5fa' },
  { label: 'Security', keys: ['EndpointSecurityIntents', 'ConditionalAccessPolicies', 'AppProtectionPolicies'], icon: <SecurityIcon />, color: '#f472b6' },
  { label: 'Applications', keys: ['Applications'], icon: <AppsIcon />, color: '#34d399' },
  { label: 'Scripts', keys: ['DeviceHealthScripts', 'DeviceManagementScripts', 'DeviceShellScripts', 'ComplianceScripts'], icon: <TerminalIcon />, color: '#a78bfa' },
  { label: 'Devices', keys: ['ManagedDevices'], icon: <DevicesIcon />, color: '#fbbf24' },
  { label: 'Enrollment', keys: ['EnrollmentConfigurations', 'AutopilotProfiles'], icon: <AppRegistrationIcon />, color: '#fb923c' },
  { label: 'Updates', keys: ['FeatureUpdateProfiles', 'QualityUpdateProfiles'], icon: <SystemUpdateIcon />, color: '#2dd4bf' },
  { label: 'Other', keys: ['AssignmentFilters', 'ScopeTags', 'NamedLocations', 'RoleDefinitions', 'TermsAndConditions', 'NotificationTemplates', 'DeviceCategories'], icon: <MoreHorizIcon />, color: '#94a3b8' },
];

function sumKeys(cacheStatus: Array<{ cacheKey: string; itemCount: number }>, keys: string[]): number {
  return cacheStatus
    .filter((s) => keys.includes(s.cacheKey))
    .reduce((sum, s) => sum + s.itemCount, 0);
}

/* ── Stat Card ────────────────────────────────────────── */

function StatCard({ label, value, icon, color }: { label: string; value: number; icon: React.ReactNode; color: string }) {
  return (
    <Paper
      variant="outlined"
      sx={{
        p: 2,
        display: 'flex',
        alignItems: 'center',
        gap: 1.5,
        borderColor: 'var(--border)',
        bgcolor: '#111827',
        transition: 'border-color 0.15s',
        '&:hover': { borderColor: color },
      }}
    >
      <Box sx={{ color, display: 'flex', fontSize: 28 }}>{icon}</Box>
      <Box>
        <Typography variant="h5" sx={{ fontWeight: 700, lineHeight: 1, color: 'var(--text-primary)' }}>
          {value.toLocaleString()}
        </Typography>
        <Typography variant="caption" sx={{ color: 'var(--text-muted)', textTransform: 'uppercase', letterSpacing: '0.04em', fontWeight: 500, fontSize: 11 }}>
          {label}
        </Typography>
      </Box>
    </Paper>
  );
}

/* ── Script Health Card ───────────────────────────────── */

function HealthCard({ label, value, color, icon }: { label: string; value: number; color: string; icon: React.ReactNode }) {
  return (
    <Paper variant="outlined" sx={{ p: 2, display: 'flex', alignItems: 'center', gap: 1.5, borderColor: 'var(--border)', bgcolor: '#111827' }}>
      <Box sx={{ color, display: 'flex' }}>{icon}</Box>
      <Box>
        <Typography variant="h6" sx={{ fontWeight: 700, lineHeight: 1, color }}>{value.toLocaleString()}</Typography>
        <Typography variant="caption" sx={{ color: 'var(--text-muted)', fontSize: 11 }}>{label}</Typography>
      </Box>
    </Paper>
  );
}

/* ── Main component ──────────────────────────────────── */

export function OverviewDashboard() {
  const activeProfile = useAppStore((s) => s.activeProfile);
  const cacheStatus = useCacheSyncStore((s) => s.cacheStatus);
  const loadStatus = useCacheSyncStore((s) => s.loadStatus);
  const policies = useSettingsCatalogStore((s) => s.policies);
  const loadPolicies = useSettingsCatalogStore((s) => s.loadPolicies);
  const scripts = useDetectionRemediationStore((s) => s.scripts);
  const loadScripts = useDetectionRemediationStore((s) => s.loadScripts);
  const setSidebarItem = useAppStore((s) => s.setSidebarItem);
  const syncAll = useCacheSyncStore((s) => s.syncAll);
  const isSyncing = useCacheSyncStore((s) => s.isSyncing);

  // Compliance posture from bridge
  const [compliance, setCompliance] = useState<{
    compliantDevices: number;
    nonCompliantDevices: number;
    inGracePeriodDevices: number;
    unknownDevices: number;
    totalManagedDevices: number;
  } | null>(null);

  const loadCompliance = useCallback(() => {
    sendCommand<{
      compliantDevices: number;
      nonCompliantDevices: number;
      inGracePeriodDevices: number;
      unknownDevices: number;
      totalManagedDevices: number;
    }>('dashboard.complianceSummary').then(setCompliance).catch((err) => {
      console.warn('[Dashboard] Failed to load compliance summary:', err instanceof Error ? err.message : err);
    });
  }, []);

  useEffect(() => {
    loadStatus();
    if (policies.length === 0) loadPolicies();
    if (scripts.length === 0) loadScripts();
    loadCompliance();
  }, [loadStatus, loadPolicies, loadScripts, loadCompliance, policies.length, scripts.length]);

  // Background sync: refresh all dashboard data when sync completes
  const handleSync = useCallback(async () => {
    await syncAll();
    // Silently reload all data sources
    loadStatus();
    loadPolicies();
    loadScripts();
    loadCompliance();
  }, [syncAll, loadStatus, loadPolicies, loadScripts, loadCompliance]);

  // Category counts from cache
  const categoryCounts = useMemo(
    () => CATEGORIES.map((cat) => ({ ...cat, count: sumKeys(cacheStatus, cat.keys) })),
    [cacheStatus],
  );

  // Pie data: platform distribution from settings catalog
  const platformPieData = useMemo(() => {
    const counts: Record<string, number> = {};
    for (const p of policies) {
      const platform = p.platform || 'Unknown';
      counts[platform] = (counts[platform] || 0) + 1;
    }
    const colors = ['#60a5fa', '#f472b6', '#34d399', '#fbbf24', '#a78bfa', '#fb923c', '#2dd4bf', '#94a3b8'];
    return Object.entries(counts)
      .sort((a, b) => b[1] - a[1])
      .map(([label, value], i) => ({ id: i, value, label, color: colors[i % colors.length] }));
  }, [policies]);

  // Script health aggregation
  const scriptHealth = useMemo(() => {
    let noIssue = 0, detected = 0, remediated = 0, reoccurred = 0;
    for (const s of scripts) {
      noIssue += s.noIssueDetectedCount ?? 0;
      detected += s.issueDetectedCount ?? 0;
      remediated += s.issueRemediatedCount ?? 0;
      reoccurred += s.issueReoccurredCount ?? 0;
    }
    return { noIssue, detected, remediated, reoccurred };
  }, [scripts]);

  // Assignment coverage
  const assignmentCoverage = useMemo(() => {
    const total = policies.length;
    const assigned = policies.filter((p) => p.isAssigned).length;
    return { total, assigned, pct: total > 0 ? Math.round((assigned / total) * 100) : 0 };
  }, [policies]);

  // Recently modified items (top 10)
  const recentItems = useMemo(() => {
    const items: Array<{ name: string; type: string; modified: string; workspace: string }> = [];
    for (const p of policies) {
      items.push({ name: p.name, type: 'Settings Catalog', modified: p.lastModifiedDateTime, workspace: 'settings-catalog' });
    }
    for (const s of scripts) {
      items.push({ name: s.displayName, type: 'Health Script', modified: s.lastModifiedDateTime, workspace: 'detection-remediation' });
    }
    return items
      .filter((i) => i.modified)
      .sort((a, b) => new Date(b.modified).getTime() - new Date(a.modified).getTime())
      .slice(0, 10);
  }, [policies, scripts]);

  return (
    <div className="workspace">
      <div className="workspace-toolbar">
        <div className="workspace-heading">
          <strong className="workspace-title">Tenant Overview</strong>
        </div>
        <Button
          variant="outlined"
          size="small"
          startIcon={isSyncing ? <CircularProgress size={14} color="inherit" /> : <SyncIcon />}
          onClick={handleSync}
          disabled={isSyncing}
          sx={{ textTransform: 'none', color: 'var(--text-secondary)', borderColor: 'var(--border)', fontSize: 12 }}
        >
          {isSyncing ? 'Syncing...' : 'Sync Data'}
        </Button>
      </div>

      <Box sx={{ display: 'grid', gap: 2.5, px: 2, pb: 3 }}>
        {/* Row 1: Tenant info bar */}
        {activeProfile && (
          <Paper variant="outlined" sx={{ borderColor: 'var(--border)', bgcolor: '#111827', px: 2.5, py: 1.5 }}>
            <Box sx={{ display: 'flex', gap: 3, alignItems: 'center', flexWrap: 'wrap' }}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <Typography variant="caption" sx={{ color: 'var(--text-muted)', textTransform: 'uppercase', fontSize: 10, letterSpacing: '0.06em' }}>Profile</Typography>
                <Typography variant="body2" sx={{ fontWeight: 600, fontSize: 13 }}>{activeProfile.name}</Typography>
              </Box>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <Typography variant="caption" sx={{ color: 'var(--text-muted)', textTransform: 'uppercase', fontSize: 10, letterSpacing: '0.06em' }}>Tenant</Typography>
                <Typography variant="body2" sx={{ fontSize: 12, fontFamily: 'monospace', color: 'var(--text-secondary)' }}>{activeProfile.tenantId}</Typography>
              </Box>
              <Chip label={activeProfile.cloud} size="small" variant="outlined" sx={{ fontSize: 11, height: 22 }} />
              <Chip label={activeProfile.authMethod} size="small" variant="outlined" sx={{ fontSize: 11, height: 22 }} />
            </Box>
          </Paper>
        )}

        {/* Row 2: Stat cards */}
        <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 1.5 }}>
          {categoryCounts.map((cat) => (
            <StatCard key={cat.label} label={cat.label} value={cat.count} icon={cat.icon} color={cat.color} />
          ))}
        </Box>

        {/* Row 3: Compliance Posture */}
        {compliance && compliance.totalManagedDevices > 0 && (
          <Box>
            <Typography variant="subtitle2" sx={{ fontWeight: 600, mb: 1, fontSize: 13 }}>Compliance Posture</Typography>
            <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 1.5 }}>
              <HealthCard label="Compliant" value={compliance.compliantDevices} color="#34d399" icon={<GppGoodIcon fontSize="small" />} />
              <HealthCard label="Non-compliant" value={compliance.nonCompliantDevices} color="#ef5350" icon={<GppBadIcon fontSize="small" />} />
              <HealthCard label="In grace period" value={compliance.inGracePeriodDevices} color="#fbbf24" icon={<TimerIcon fontSize="small" />} />
              <HealthCard label="Unknown" value={compliance.unknownDevices} color="#94a3b8" icon={<HelpOutlineIcon fontSize="small" />} />
            </Box>
          </Box>
        )}

        {/* Row 4: Platform Distribution */}
        <Paper variant="outlined" sx={{ borderColor: 'var(--border)', bgcolor: '#111827', p: 2 }}>
          <Typography variant="subtitle2" sx={{ fontWeight: 600, mb: 1, fontSize: 13 }}>Platform Distribution</Typography>
          {platformPieData.length > 0 ? (
            <PieChart
              series={[{
                data: platformPieData,
                innerRadius: 50,
                outerRadius: 110,
                paddingAngle: 2,
                cornerRadius: 4,
              }]}
              height={280}
              margin={{ top: 10, bottom: 10, left: 10, right: 160 }}
            />
          ) : (
            <Box sx={{ height: 260, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
              <Typography variant="body2" sx={{ color: 'var(--text-muted)' }}>Load Settings Catalog to see platforms</Typography>
            </Box>
          )}
        </Paper>

        {/* Row 5: Script Health Summary */}
        {scripts.length > 0 && (
          <Box>
            <Typography variant="subtitle2" sx={{ fontWeight: 600, mb: 1, fontSize: 13 }}>Script Health Summary</Typography>
            <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 1.5 }}>
              <HealthCard label="Devices without issues" value={scriptHealth.noIssue} color="#34d399" icon={<CheckCircleIcon fontSize="small" />} />
              <HealthCard label="Devices with issues" value={scriptHealth.detected} color="#ef5350" icon={<ErrorOutlineIcon fontSize="small" />} />
              <HealthCard label="Issues fixed" value={scriptHealth.remediated} color="#60a5fa" icon={<CheckCircleIcon fontSize="small" />} />
              <HealthCard label="Issues reoccurred" value={scriptHealth.reoccurred} color="#fb923c" icon={<ReplayIcon fontSize="small" />} />
            </Box>
          </Box>
        )}

        {/* Row 5: Assignment Coverage */}
        {policies.length > 0 && (
          <Paper variant="outlined" sx={{ borderColor: 'var(--border)', bgcolor: '#111827', p: 2 }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
              <Typography variant="subtitle2" sx={{ fontWeight: 600, fontSize: 13 }}>Assignment Coverage</Typography>
              <Typography variant="body2" sx={{ color: 'var(--text-secondary)', fontSize: 12 }}>
                <strong>{assignmentCoverage.assigned}</strong> of {assignmentCoverage.total} Settings Catalog policies assigned ({assignmentCoverage.pct}%)
              </Typography>
            </Box>
            <LinearProgress
              variant="determinate"
              value={assignmentCoverage.pct}
              sx={{
                height: 8,
                borderRadius: 4,
                bgcolor: 'rgba(255,255,255,0.06)',
                '& .MuiLinearProgress-bar': {
                  borderRadius: 4,
                  bgcolor: assignmentCoverage.pct > 75 ? '#34d399' : assignmentCoverage.pct > 40 ? '#fbbf24' : '#ef5350',
                },
              }}
            />
          </Paper>
        )}

        {/* Row 8: Recently Modified */}
        {recentItems.length > 0 && (
          <Paper variant="outlined" sx={{ borderColor: 'var(--border)', bgcolor: '#111827' }}>
            <Box sx={{ px: 2, py: 1.5, borderBottom: '1px solid var(--border)' }}>
              <Typography variant="subtitle2" sx={{ fontWeight: 600, fontSize: 13 }}>Recently Modified</Typography>
            </Box>
            <Box>
              {recentItems.map((item, i) => (
                <Box
                  key={`${item.type}-${item.name}-${i}`}
                  onClick={() => setSidebarItem(item.workspace)}
                  sx={{
                    display: 'grid',
                    gridTemplateColumns: '1fr 140px 160px',
                    gap: 2,
                    px: 2,
                    py: 1.25,
                    borderBottom: i < recentItems.length - 1 ? '1px solid var(--border)' : 'none',
                    cursor: 'pointer',
                    '&:hover': { bgcolor: 'rgba(255,255,255,0.03)' },
                  }}
                >
                  <Typography variant="body2" sx={{ fontSize: 13, fontWeight: 500, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                    {item.name}
                  </Typography>
                  <Chip label={item.type} size="small" variant="outlined" sx={{ fontSize: 10, height: 20, justifySelf: 'start' }} />
                  <Typography variant="caption" sx={{ color: 'var(--text-muted)', justifySelf: 'end' }}>
                    {new Date(item.modified).toLocaleDateString(undefined, { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })}
                  </Typography>
                </Box>
              ))}
            </Box>
          </Paper>
        )}
      </Box>
    </div>
  );
}
