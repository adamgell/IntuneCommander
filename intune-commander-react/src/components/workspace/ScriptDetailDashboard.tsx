import { useState, useCallback } from 'react';
import Editor from '@monaco-editor/react';
import { DataGrid } from '@mui/x-data-grid';
import type { GridColDef } from '@mui/x-data-grid';
import Box from '@mui/material/Box';
import Paper from '@mui/material/Paper';
import Typography from '@mui/material/Typography';
import Chip from '@mui/material/Chip';
import Button from '@mui/material/Button';
import IconButton from '@mui/material/IconButton';
import Skeleton from '@mui/material/Skeleton';
import Divider from '@mui/material/Divider';
import TextField from '@mui/material/TextField';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import BugReportIcon from '@mui/icons-material/BugReport';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import WarningAmberIcon from '@mui/icons-material/WarningAmber';
import ErrorOutlineIcon from '@mui/icons-material/ErrorOutline';
import ReplayIcon from '@mui/icons-material/Replay';
import EditIcon from '@mui/icons-material/Edit';
import SaveIcon from '@mui/icons-material/Save';
import CloseIcon from '@mui/icons-material/Close';
import RocketLaunchIcon from '@mui/icons-material/RocketLaunch';
import { useDetectionRemediationStore } from '../../store/detectionRemediationStore';
import type { RunSummary, DeviceRunState } from '../../types/detectionRemediation';
import { DeployPanel } from './DeployPanel';

/* ── Metric card ─────────────────────────────────────── */

function MetricCard({ label, value, color, icon }: {
  label: string;
  value: number;
  color: string;
  icon: React.ReactNode;
}) {
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
      }}
    >
      <Box sx={{ color, display: 'flex' }}>{icon}</Box>
      <Box>
        <Typography variant="h5" sx={{ fontWeight: 700, lineHeight: 1, color }}>
          {value.toLocaleString()}
        </Typography>
        <Typography variant="caption" sx={{ color: 'var(--text-muted)', textTransform: 'uppercase', letterSpacing: '0.04em', fontWeight: 500 }}>
          {label}
        </Typography>
      </Box>
    </Paper>
  );
}

/* ── Status chip for run states ──────────────────────── */

function StateChip({ state }: { state: string }) {
  const lower = state.toLowerCase();
  if (lower.includes('noissue') || lower.includes('success') || lower === 'notapplicable')
    return <Chip label={humanize(state)} size="small" color="success" variant="outlined" sx={{ fontSize: 11 }} />;
  if (lower.includes('issue') || lower.includes('detected'))
    return <Chip label={humanize(state)} size="small" color="warning" variant="outlined" sx={{ fontSize: 11 }} />;
  if (lower.includes('error') || lower.includes('fail'))
    return <Chip label={humanize(state)} size="small" color="error" variant="outlined" sx={{ fontSize: 11 }} />;
  if (lower.includes('remediated'))
    return <Chip label={humanize(state)} size="small" color="success" variant="outlined" sx={{ fontSize: 11 }} />;
  return <Chip label={humanize(state)} size="small" variant="outlined" sx={{ fontSize: 11 }} />;
}

function humanize(s: string): string {
  return s.replace(/([a-z])([A-Z])/g, '$1 $2').replace(/^./, c => c.toUpperCase());
}

/* ── Code pane (Monaco) ───────────────────────────────── */

function CodePane({ title, code, editing, onChange }: {
  title: string;
  code: string;
  editing: boolean;
  onChange?: (value: string) => void;
}) {
  const lineCount = code ? code.split('\n').length : 0;
  const height = Math.min(Math.max(lineCount * 19, 120), 480);

  return (
    <Paper
      variant="outlined"
      sx={{
        borderColor: editing ? 'var(--accent)' : 'var(--border)',
        bgcolor: '#1e1e1e',
        overflow: 'hidden',
        flex: 1,
        minWidth: 0,
      }}
    >
      <Box sx={{
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        px: 2,
        py: 1.5,
        bgcolor: editing ? '#1a1a2e' : '#101828',
        borderBottom: '1px solid rgba(255,255,255,0.08)',
      }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <Typography variant="subtitle2" sx={{ fontWeight: 600, color: '#f9fafb' }}>
            {title}
          </Typography>
          {editing && (
            <Chip label="EDITING" size="small" color="warning" sx={{ fontSize: 10, height: 20 }} />
          )}
        </Box>
        <Typography variant="caption" sx={{ color: 'var(--text-muted)' }}>
          {lineCount ? `${lineCount} lines` : 'Empty'}
        </Typography>
      </Box>
      <Editor
        height={height}
        language="powershell"
        value={code || '# No script content'}
        theme="vs-dark"
        onChange={(v) => onChange?.(v ?? '')}
        options={{
          readOnly: !editing,
          domReadOnly: !editing,
          minimap: { enabled: false },
          scrollBeyondLastLine: false,
          fontSize: 12,
          lineNumbers: 'on',
          folding: true,
          wordWrap: 'on',
          renderLineHighlight: editing ? 'line' : 'none',
          overviewRulerLanes: 0,
          hideCursorInOverviewRuler: !editing,
          scrollbar: { verticalScrollbarSize: 8 },
          padding: { top: 8, bottom: 8 },
        }}
      />
    </Paper>
  );
}

/* ── Property row ────────────────────────────────────── */

function PropRow({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <Box sx={{
      display: 'grid',
      gridTemplateColumns: 'minmax(140px, 25%) 1fr',
      gap: 2,
      py: 1.25,
      borderBottom: '1px solid var(--border)',
    }}>
      <Typography variant="body2" sx={{ color: 'var(--text-tertiary)', fontSize: 13 }}>
        {label}
      </Typography>
      <Typography variant="body2" sx={{ color: 'var(--text-secondary)', fontSize: 13 }}>
        {value}
      </Typography>
    </Box>
  );
}

/* ── Run summary metrics row ─────────────────────────── */

function RunSummaryMetrics({ summary }: { summary: RunSummary }) {
  return (
    <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(150px, 1fr))', gap: 1.5 }}>
      <MetricCard
        label="No issues"
        value={summary.noIssueDetectedCount}
        color="var(--success)"
        icon={<CheckCircleIcon fontSize="small" />}
      />
      <MetricCard
        label="Detected"
        value={summary.issueDetectedCount}
        color="var(--warning)"
        icon={<WarningAmberIcon fontSize="small" />}
      />
      <MetricCard
        label="Remediated"
        value={summary.issueRemediatedCount}
        color="var(--success)"
        icon={<CheckCircleIcon fontSize="small" />}
      />
      <MetricCard
        label="Reoccurred"
        value={summary.issueReoccurredCount}
        color="var(--warning)"
        icon={<ReplayIcon fontSize="small" />}
      />
      <MetricCard
        label="Errors"
        value={summary.errorDeviceCount}
        color="var(--error)"
        icon={<ErrorOutlineIcon fontSize="small" />}
      />
    </Box>
  );
}

/* ── Device run states DataGrid ─────────────────────── */

const dgSx = {
  border: 'none',
  '& .MuiDataGrid-columnHeaders': { bgcolor: '#0f172a' },
  '& .MuiDataGrid-columnHeaderTitle': {
    fontSize: 11, fontWeight: 600, letterSpacing: '0.06em', textTransform: 'uppercase',
    color: 'var(--text-muted)',
  },
  '& .MuiDataGrid-cell': {
    borderBottom: '1px solid var(--border)', color: 'var(--text-secondary)', fontSize: 12,
    py: 1.5,
  },
  '& .MuiDataGrid-row:hover': { bgcolor: 'rgba(255,255,255,0.03)' },
  '& .MuiDataGrid-footerContainer': { borderTop: '1px solid var(--border)' },
  '& .MuiTablePagination-root': { color: 'var(--text-muted)' },
  '& .MuiDataGrid-toolbarContainer': { px: 1, py: 0.5 },
  '& .MuiDataGrid-scrollbarFiller--header': { bgcolor: '#0f172a' },
};

const runStateColumns: GridColDef<DeviceRunState>[] = [
  {
    field: 'deviceName',
    headerName: 'DEVICE',
    flex: 1.2,
    minWidth: 180,
    renderCell: (params) => (
      <div style={{ display: 'flex', flexDirection: 'column', gap: 1, lineHeight: 1.4, padding: '4px 0' }}>
        <strong style={{ fontSize: 12 }}>{params.row.deviceName}</strong>
        {params.row.userPrincipalName && (
          <span style={{ fontSize: 11, color: 'var(--text-muted)' }}>{params.row.userPrincipalName}</span>
        )}
      </div>
    ),
  },
  {
    field: 'detectionState',
    headerName: 'DETECTION',
    width: 130,
    renderCell: (params) => <StateChip state={params.value} />,
  },
  {
    field: 'remediationState',
    headerName: 'REMEDIATION',
    width: 140,
    renderCell: (params) => <StateChip state={params.value} />,
  },
  {
    field: 'preRemediationDetectionScriptOutput',
    headerName: 'PRE-DETECTION OUTPUT',
    flex: 1,
    minWidth: 160,
    renderCell: (params) => (
      <span style={{ fontSize: 11, whiteSpace: 'pre-wrap', lineHeight: 1.3 }}>
        {params.value || '—'}
      </span>
    ),
  },
  {
    field: 'postRemediationDetectionScriptOutput',
    headerName: 'POST-DETECTION OUTPUT',
    flex: 1,
    minWidth: 160,
    renderCell: (params) => (
      <span style={{ fontSize: 11, whiteSpace: 'pre-wrap', lineHeight: 1.3 }}>
        {params.value || '—'}
      </span>
    ),
  },
  {
    field: 'preRemediationDetectionScriptError',
    headerName: 'PRE-DETECTION ERROR',
    flex: 1,
    minWidth: 150,
    renderCell: (params) => params.value ? (
      <span style={{ fontSize: 11, color: 'var(--error)', whiteSpace: 'pre-wrap', lineHeight: 1.3 }}>{params.value}</span>
    ) : <span style={{ color: 'var(--text-muted)' }}>—</span>,
  },
  {
    field: 'remediationScriptError',
    headerName: 'REMEDIATION ERROR',
    flex: 1,
    minWidth: 150,
    renderCell: (params) => params.value ? (
      <span style={{ fontSize: 11, color: 'var(--error)', whiteSpace: 'pre-wrap', lineHeight: 1.3 }}>{params.value}</span>
    ) : <span style={{ color: 'var(--text-muted)' }}>—</span>,
  },
  {
    field: 'lastStateUpdateDateTime',
    headerName: 'UPDATED',
    width: 160,
    valueFormatter: (value: string) => value ? new Date(value).toLocaleString() : '—',
  },
];

function DeviceRunStatesGrid({ states }: { states: DeviceRunState[] }) {
  const rows = states.map((ds, i) => ({ ...ds, id: `${ds.deviceName}-${ds.lastStateUpdateDateTime ?? i}` }));

  return (
    <Paper variant="outlined" sx={{ borderColor: 'var(--border)', bgcolor: '#111827' }}>
      <Box sx={{
        display: 'flex', justifyContent: 'space-between', alignItems: 'center',
        px: 2, py: 1.5, borderBottom: '1px solid var(--border)',
      }}>
        <Typography variant="subtitle2" sx={{ fontWeight: 600 }}>
          Device run states
        </Typography>
        <Typography variant="caption" sx={{ color: 'var(--text-muted)' }}>
          {states.length} devices
        </Typography>
      </Box>
      {states.length === 0 ? (
        <Box sx={{ px: 2, py: 4, textAlign: 'center' }}>
          <Typography variant="body2" sx={{ color: 'var(--text-muted)' }}>
            No device run states available
          </Typography>
        </Box>
      ) : (
        <Box sx={{ height: Math.min(states.length * 52 + 110, 450) }}>
          <DataGrid
            rows={rows}
            columns={runStateColumns}
            getRowHeight={() => 'auto'}
            pageSizeOptions={[25, 50, 100]}
            initialState={{
              pagination: { paginationModel: { pageSize: 25 } },
              sorting: { sortModel: [{ field: 'lastStateUpdateDateTime', sort: 'desc' }] },
            }}
            showToolbar
            slotProps={{
              toolbar: { showQuickFilter: true },
            }}
            sx={dgSx}
          />
        </Box>
      )}
    </Paper>
  );
}

/* ── Assignments panel ───────────────────────────────── */

function AssignmentsPanel() {
  const detail = useDetectionRemediationStore((s) => s.scriptDetail);
  if (!detail) return null;

  return (
    <Paper variant="outlined" sx={{ borderColor: 'var(--border)', bgcolor: '#111827' }}>
      <Box sx={{ px: 2, py: 1.5, borderBottom: '1px solid var(--border)' }}>
        <Typography variant="subtitle2" sx={{ fontWeight: 600 }}>
          Assignments
        </Typography>
        <Typography variant="caption" sx={{ color: 'var(--text-muted)' }}>
          {detail.assignments.length} group(s)
        </Typography>
      </Box>
      {detail.assignments.length === 0 ? (
        <Box sx={{ px: 2, py: 3, textAlign: 'center' }}>
          <Typography variant="body2" sx={{ color: 'var(--text-muted)' }}>
            No assignments configured
          </Typography>
        </Box>
      ) : (
        <Box sx={{ px: 2, py: 1 }}>
          {detail.assignments.map((a, i) => (
            <Box key={i} sx={{ py: 1.25, borderBottom: i < detail.assignments.length - 1 ? '1px solid var(--border)' : 'none' }}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 0.5 }}>
                <Typography variant="body2" sx={{ fontSize: 13, fontWeight: 500 }}>
                  {a.target}
                </Typography>
                <Chip
                  label={a.targetKind}
                  size="small"
                  color={a.targetKind === 'Exclude' ? 'error' : 'default'}
                  variant="outlined"
                  sx={{ fontSize: 10, height: 20 }}
                />
              </Box>
              <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                <Typography variant="caption" sx={{ color: 'var(--text-tertiary)' }}>
                  {a.schedule}
                </Typography>
                {a.runRemediation && (
                  <Chip label="Remediation on" size="small" color="success" variant="outlined" sx={{ fontSize: 10, height: 18 }} />
                )}
              </Box>
            </Box>
          ))}
        </Box>
      )}
    </Paper>
  );
}

/* ── Loading skeleton ────────────────────────────────── */

function DetailSkeleton() {
  return (
    <Box sx={{ display: 'grid', gap: 2 }}>
      <Skeleton variant="rectangular" height={48} sx={{ borderRadius: 1 }} />
      <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(5, 1fr)', gap: 1.5 }}>
        {Array.from({ length: 5 }).map((_, i) => (
          <Skeleton key={i} variant="rectangular" height={72} sx={{ borderRadius: 1 }} />
        ))}
      </Box>
      <Skeleton variant="rectangular" height={200} sx={{ borderRadius: 1 }} />
      <Box sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 2 }}>
        <Skeleton variant="rectangular" height={240} sx={{ borderRadius: 1 }} />
        <Skeleton variant="rectangular" height={240} sx={{ borderRadius: 1 }} />
      </Box>
    </Box>
  );
}

/* ── Editable text field (dark themed) ─────────────── */

const editFieldSx = {
  '& .MuiOutlinedInput-root': {
    color: 'var(--text-primary)',
    fontSize: 13,
    '& fieldset': { borderColor: 'var(--border)' },
    '&:hover fieldset': { borderColor: 'var(--text-muted)' },
    '&.Mui-focused fieldset': { borderColor: 'var(--accent)' },
  },
  '& .MuiInputLabel-root': { color: 'var(--text-muted)', fontSize: 13 },
  '& .MuiInputLabel-root.Mui-focused': { color: 'var(--accent)' },
};

/* ── Main dashboard ──────────────────────────────────── */

interface EditableFields {
  displayName: string;
  description: string;
  detectionScript: string;
  remediationScript: string;
}

export function ScriptDetailDashboard() {
  const detail = useDetectionRemediationStore((s) => s.scriptDetail);
  const isLoadingDetail = useDetectionRemediationStore((s) => s.isLoadingDetail);
  const isSaving = useDetectionRemediationStore((s) => s.isSaving);
  const clearSelection = useDetectionRemediationStore((s) => s.clearSelection);
  const saveScript = useDetectionRemediationStore((s) => s.saveScript);

  const [editing, setEditing] = useState(false);
  const [deployMode, setDeployMode] = useState(false);
  const [editFields, setEditFields] = useState<EditableFields | null>(null);

  const enterEditMode = useCallback(() => {
    if (!detail) return;
    setEditFields({
      displayName: detail.displayName,
      description: detail.description ?? '',
      detectionScript: detail.detectionScript,
      remediationScript: detail.remediationScript,
    });
    setEditing(true);
  }, [detail]);

  const cancelEdit = useCallback(() => {
    setEditing(false);
    setEditFields(null);
  }, []);

  const updateField = useCallback((field: keyof EditableFields, value: string) => {
    setEditFields((prev) => prev ? { ...prev, [field]: value } : prev);
  }, []);

  const handleSave = useCallback(async () => {
    if (!detail || !editFields) return;
    if (!editFields.displayName.trim()) return;
    const ok = await saveScript({
      id: detail.id,
      displayName: editFields.displayName !== detail.displayName ? editFields.displayName : undefined,
      description: editFields.description !== (detail.description ?? '') ? editFields.description : undefined,
      detectionScript: editFields.detectionScript,
      remediationScript: editFields.remediationScript,
    });
    if (ok) {
      setEditing(false);
      setEditFields(null);
    }
  }, [detail, editFields, saveScript]);

  if (isLoadingDetail || !detail) {
    return (
      <Box sx={{ display: 'grid', gap: 2 }}>
        <Button
          startIcon={<ArrowBackIcon />}
          onClick={clearSelection}
          sx={{ justifySelf: 'start', color: 'var(--text-secondary)', textTransform: 'none' }}
        >
          Back to scripts
        </Button>
        <DetailSkeleton />
      </Box>
    );
  }

  if (deployMode) {
    return (
      <DeployPanel
        scriptId={detail.id}
        scriptName={detail.displayName}
        onBack={() => setDeployMode(false)}
      />
    );
  }

  return (
    <Box sx={{ display: 'grid', gap: 2.5 }}>
      {/* Header row */}
      <Box sx={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', gap: 2 }}>
        <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 2, flex: 1, minWidth: 0 }}>
          <IconButton onClick={clearSelection} size="small" sx={{ color: 'var(--text-secondary)', mt: 0.5 }} disabled={editing}>
            <ArrowBackIcon />
          </IconButton>
          {editing && editFields ? (
            <Box sx={{ flex: 1, display: 'grid', gap: 1.5 }}>
              <TextField
                label="Script name"
                value={editFields.displayName}
                onChange={(e) => updateField('displayName', e.target.value)}
                size="small"
                fullWidth
                sx={editFieldSx}
              />
              <TextField
                label="Description"
                value={editFields.description}
                onChange={(e) => updateField('description', e.target.value)}
                size="small"
                fullWidth
                multiline
                maxRows={3}
                sx={editFieldSx}
              />
            </Box>
          ) : (
            <Box>
              <Typography variant="h6" sx={{ fontWeight: 600, lineHeight: 1.3 }}>
                {detail.displayName}
              </Typography>
              {detail.description && (
                <Typography variant="body2" sx={{ color: 'var(--text-tertiary)', mt: 0.25 }}>
                  {detail.description}
                </Typography>
              )}
            </Box>
          )}
        </Box>
        <Box sx={{ display: 'flex', gap: 1, alignItems: 'center', flexShrink: 0 }}>
          {editing ? (
            <>
              <Button
                variant="outlined"
                size="small"
                startIcon={<CloseIcon />}
                onClick={cancelEdit}
                disabled={isSaving}
                sx={{ textTransform: 'none', borderColor: 'var(--border)', color: 'var(--text-secondary)' }}
              >
                Cancel
              </Button>
              <Button
                variant="contained"
                size="small"
                startIcon={<SaveIcon />}
                onClick={handleSave}
                disabled={isSaving}
                sx={{ textTransform: 'none' }}
              >
                {isSaving ? 'Saving...' : 'Save to Intune'}
              </Button>
            </>
          ) : (
            <>
              {detail.isGlobal && <Chip label="Microsoft Managed" size="small" color="info" variant="outlined" />}
              <Chip
                label={detail.runAsAccount}
                size="small"
                variant="outlined"
                icon={<BugReportIcon sx={{ fontSize: '14px !important' }} />}
              />
              {!detail.isGlobal && (
                <>
                  <Button
                    variant="outlined"
                    size="small"
                    startIcon={<EditIcon />}
                    onClick={enterEditMode}
                    sx={{ textTransform: 'none', ml: 1 }}
                  >
                    Edit
                  </Button>
                  <Button
                    variant="outlined"
                    size="small"
                    color="warning"
                    startIcon={<RocketLaunchIcon />}
                    onClick={() => setDeployMode(true)}
                    sx={{ textTransform: 'none' }}
                  >
                    Deploy on Demand
                  </Button>
                </>
              )}
            </>
          )}
        </Box>
      </Box>

      {/* Edit mode banner */}
      {editing && (
        <Paper
          sx={{
            px: 2, py: 1.5,
            bgcolor: 'rgba(255, 152, 0, 0.08)',
            border: '1px solid rgba(255, 152, 0, 0.3)',
            borderRadius: 1,
          }}
        >
          <Typography variant="body2" sx={{ color: '#ffb74d', fontSize: 13 }}>
            You are editing this script. Changes will be pushed live to Intune when you click Save.
          </Typography>
        </Paper>
      )}

      {/* Run summary metrics */}
      {detail.runSummary && <RunSummaryMetrics summary={detail.runSummary} />}

      {/* Device run states — full width DataGrid */}
      <DeviceRunStatesGrid states={detail.deviceRunStates} />

      {/* 2-column: Properties + Assignments */}
      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: '1fr 1fr' }, gap: 2, alignItems: 'start' }}>
        {/* Script properties */}
        <Paper variant="outlined" sx={{ borderColor: 'var(--border)', bgcolor: '#111827', p: 2 }}>
          <Typography variant="subtitle2" sx={{ fontWeight: 600, mb: 1.5 }}>
            Properties
          </Typography>
          <Box sx={{ borderTop: '1px solid var(--border)' }}>
            <PropRow label="Publisher" value={detail.publisher || '—'} />
            <PropRow label="Version" value={detail.version || '—'} />
            <PropRow label="Run as" value={detail.runAsAccount} />
            <PropRow label="32-bit" value={detail.runAs32Bit ? 'Yes' : 'No'} />
            <PropRow label="Signature check" value={detail.enforceSignatureCheck ? 'Enabled' : 'Disabled'} />
            <PropRow
              label="Created"
              value={detail.createdDateTime ? new Date(detail.createdDateTime).toLocaleString() : '—'}
            />
            <PropRow
              label="Modified"
              value={detail.lastModifiedDateTime ? new Date(detail.lastModifiedDateTime).toLocaleString() : '—'}
            />
          </Box>
        </Paper>

        {/* Assignments */}
        <AssignmentsPanel />
      </Box>

      <Divider sx={{ borderColor: 'var(--border)' }} />

      {/* Code panes side-by-side */}
      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: '1fr 1fr' }, gap: 2 }}>
        <CodePane
          title="Detection script"
          code={editing ? (editFields?.detectionScript ?? '') : detail.detectionScript}
          editing={editing}
          onChange={(v) => updateField('detectionScript', v)}
        />
        <CodePane
          title="Remediation script"
          code={editing ? (editFields?.remediationScript ?? '') : detail.remediationScript}
          editing={editing}
          onChange={(v) => updateField('remediationScript', v)}
        />
      </Box>
    </Box>
  );
}
