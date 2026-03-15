import { useEffect, useMemo } from 'react';
import {
  DataGrid,
  type GridColDef,
  type GridRowParams,
  Toolbar,
  QuickFilter,
  QuickFilterControl,
  QuickFilterClear,
  ColumnsPanelTrigger,
} from '@mui/x-data-grid';
import Button from '@mui/material/Button';
import ViewColumnIcon from '@mui/icons-material/ViewColumn';
import Editor from '@monaco-editor/react';
import { useScriptsHubStore } from '../../store/scriptsHubStore';
import type { ScriptListItem, ScriptType } from '../../types/scriptsHub';
import '../../styles/workspace.css';

const typeChips: { label: string; value: ScriptType | null }[] = [
  { label: 'All', value: null },
  { label: 'PowerShell', value: 'powershell' },
  { label: 'Shell', value: 'shell' },
  { label: 'Compliance', value: 'compliance' },
  { label: 'Health', value: 'health' },
];

function TypeBadge({ type }: { type: ScriptType }) {
  const config: Record<ScriptType, { color: string; label: string }> = {
    powershell: { color: '#3b82f6', label: 'PowerShell' },
    shell: { color: '#22c55e', label: 'Shell' },
    compliance: { color: '#f59e0b', label: 'Compliance' },
    health: { color: '#a855f7', label: 'Health' },
  };
  const c = config[type] ?? { color: '#6b7280', label: type };
  return (
    <span style={{
      display: 'inline-block', padding: '2px 8px', borderRadius: 10, fontSize: 11,
      fontWeight: 600, backgroundColor: `${c.color}20`, color: c.color,
      border: `1px solid ${c.color}40`,
    }}>
      {c.label}
    </span>
  );
}

const columns: GridColDef<ScriptListItem>[] = [
  {
    field: 'displayName',
    headerName: 'Script Name',
    flex: 2,
    minWidth: 250,
    renderCell: (params) => (
      <div style={{ display: 'flex', flexDirection: 'column', gap: 2, lineHeight: 1.4, padding: '6px 0', whiteSpace: 'normal', wordBreak: 'break-word' }}>
        <strong style={{ fontSize: 13, color: 'var(--text-primary)' }}>{params.row.displayName}</strong>
        {params.row.description && (
          <span style={{ fontSize: 11, color: 'var(--text-tertiary)', lineHeight: 1.3, overflow: 'hidden', display: '-webkit-box', WebkitLineClamp: 2, WebkitBoxOrient: 'vertical' }}>
            {params.row.description}
          </span>
        )}
      </div>
    ),
  },
  {
    field: 'scriptType',
    headerName: 'Type',
    width: 130,
    renderCell: (params) => <TypeBadge type={params.value as ScriptType} />,
  },
  { field: 'platform', headerName: 'Platform', width: 120 },
  { field: 'runAsAccount', headerName: 'Run As', width: 100 },
  {
    field: 'hasRemediation',
    headerName: 'Remediation',
    width: 120,
    renderCell: (params) => params.value === true ? 'Yes' : params.value === false ? 'No' : '—',
  },
  {
    field: 'lastModifiedDateTime',
    headerName: 'Modified',
    width: 170,
    valueFormatter: (value: string) => value ? new Date(value).toLocaleString() : '',
  },
];

function ScriptToolbar() {
  return (
    <Toolbar style={{ gap: 8, padding: '8px 12px' }}>
      <QuickFilter defaultExpanded style={{ flex: 1 }}>
        <QuickFilterControl placeholder="Filter scripts..." size="small" style={{ minWidth: 200, maxWidth: 360 }} />
        <QuickFilterClear size="small" />
      </QuickFilter>
      <ColumnsPanelTrigger
        render={
          <Button size="small" variant="text" startIcon={<ViewColumnIcon sx={{ fontSize: 16 }} />}
            sx={{ color: 'var(--text-secondary)', fontSize: 12, textTransform: 'none', px: 1.5, '&:hover': { backgroundColor: 'rgba(255,255,255,0.05)' } }} />
        }
      >
        Columns
      </ColumnsPanelTrigger>
    </Toolbar>
  );
}

function ScriptDetailView() {
  const detail = useScriptsHubStore((s) => s.scriptDetail);
  const isLoading = useScriptsHubStore((s) => s.isLoadingDetail);
  const clearSelection = useScriptsHubStore((s) => s.clearSelection);

  if (isLoading) {
    return (
      <div className="panel panel-detail">
        <div style={{ padding: 20, color: 'var(--text-tertiary)' }}>Loading script...</div>
      </div>
    );
  }

  if (!detail) return null;

  const scriptLines = (detail.scriptContent || '').split('\n').length;
  const remediationLines = (detail.remediationScriptContent || '').split('\n').length;

  return (
    <div className="panel panel-detail" style={{ overflow: 'auto' }}>
      <div style={{ padding: '12px 16px', borderBottom: '1px solid var(--border)', display: 'flex', alignItems: 'center', gap: 12 }}>
        <button
          onClick={clearSelection}
          style={{ background: 'none', border: 'none', cursor: 'pointer', color: 'var(--text-tertiary)', fontSize: 18 }}
        >
          ←
        </button>
        <div>
          <strong style={{ fontSize: 14, color: 'var(--text-primary)' }}>{detail.displayName}</strong>
          <div style={{ fontSize: 11, color: 'var(--text-tertiary)' }}>
            <TypeBadge type={detail.scriptType} /> · {detail.platform}
          </div>
        </div>
      </div>

      {/* Script metadata */}
      <div style={{ padding: '12px 16px', borderBottom: '1px solid var(--border)' }}>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 8, fontSize: 12 }}>
          {detail.description && (
            <div style={{ gridColumn: '1 / -1', color: 'var(--text-secondary)', marginBottom: 4 }}>{detail.description}</div>
          )}
          <div><span style={{ color: 'var(--text-tertiary)' }}>Run As:</span> <span style={{ color: 'var(--text-secondary)' }}>{detail.runAsAccount ?? '—'}</span></div>
          <div><span style={{ color: 'var(--text-tertiary)' }}>32-bit:</span> <span style={{ color: 'var(--text-secondary)' }}>{detail.runAs32Bit ? 'Yes' : 'No'}</span></div>
          <div><span style={{ color: 'var(--text-tertiary)' }}>Signature Check:</span> <span style={{ color: 'var(--text-secondary)' }}>{detail.enforceSignatureCheck ? 'Yes' : 'No'}</span></div>
          <div><span style={{ color: 'var(--text-tertiary)' }}>Modified:</span> <span style={{ color: 'var(--text-secondary)' }}>{detail.lastModifiedDateTime ? new Date(detail.lastModifiedDateTime).toLocaleString() : '—'}</span></div>
        </div>
      </div>

      {/* Assignments */}
      {detail.assignments.length > 0 && (
        <div style={{ padding: '12px 16px', borderBottom: '1px solid var(--border)' }}>
          <div style={{ fontSize: 12, fontWeight: 600, color: 'var(--text-primary)', marginBottom: 8 }}>
            Assignments ({detail.assignments.length})
          </div>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6 }}>
            {detail.assignments.map((a, i) => (
              <span key={i} style={{
                fontSize: 11, padding: '3px 8px', borderRadius: 12,
                backgroundColor: a.targetKind === 'Exclude' ? 'rgba(239,68,68,0.1)' : 'rgba(59,130,246,0.1)',
                color: a.targetKind === 'Exclude' ? '#ef4444' : '#3b82f6',
                border: `1px solid ${a.targetKind === 'Exclude' ? 'rgba(239,68,68,0.3)' : 'rgba(59,130,246,0.3)'}`,
              }}>
                {a.targetKind === 'Exclude' ? '- ' : ''}{a.target}
              </span>
            ))}
          </div>
        </div>
      )}

      {/* Script content */}
      <div style={{ padding: '12px 16px' }}>
        <div style={{ fontSize: 12, fontWeight: 600, color: 'var(--text-primary)', marginBottom: 8 }}>
          {detail.scriptType === 'health' ? 'Detection Script' : 'Script Content'}
        </div>
        <div style={{ border: '1px solid var(--border)', borderRadius: 8, overflow: 'hidden' }}>
          <Editor
            height={Math.min(Math.max(scriptLines * 18 + 20, 150), 400)}
            language={detail.language}
            value={detail.scriptContent || '# No script content'}
            theme="vs-dark"
            options={{
              readOnly: true,
              domReadOnly: true,
              minimap: { enabled: false },
              scrollBeyondLastLine: false,
              fontSize: 12,
              lineNumbers: 'on',
              folding: true,
              wordWrap: 'on',
              renderLineHighlight: 'none',
              overviewRulerLanes: 0,
              scrollbar: { verticalScrollbarSize: 8 },
              padding: { top: 8, bottom: 8 },
            }}
          />
        </div>
      </div>

      {/* Remediation script (health scripts only) */}
      {detail.remediationScriptContent && (
        <div style={{ padding: '0 16px 16px' }}>
          <div style={{ fontSize: 12, fontWeight: 600, color: 'var(--text-primary)', marginBottom: 8 }}>
            Remediation Script
          </div>
          <div style={{ border: '1px solid var(--border)', borderRadius: 8, overflow: 'hidden' }}>
            <Editor
              height={Math.min(Math.max(remediationLines * 18 + 20, 150), 400)}
              language={detail.language}
              value={detail.remediationScriptContent}
              theme="vs-dark"
              options={{
                readOnly: true,
                domReadOnly: true,
                minimap: { enabled: false },
                scrollBeyondLastLine: false,
                fontSize: 12,
                lineNumbers: 'on',
                folding: true,
                wordWrap: 'on',
                renderLineHighlight: 'none',
                overviewRulerLanes: 0,
                scrollbar: { verticalScrollbarSize: 8 },
                padding: { top: 8, bottom: 8 },
              }}
            />
          </div>
        </div>
      )}
    </div>
  );
}

export function ScriptsHubWorkspace() {
  const scripts = useScriptsHubStore((s) => s.scripts);
  const selectedScriptId = useScriptsHubStore((s) => s.selectedScriptId);
  const isLoadingList = useScriptsHubStore((s) => s.isLoadingList);
  const hasAttemptedLoad = useScriptsHubStore((s) => s.hasAttemptedLoad);
  const error = useScriptsHubStore((s) => s.error);
  const typeFilter = useScriptsHubStore((s) => s.typeFilter);
  const loadScripts = useScriptsHubStore((s) => s.loadScripts);
  const selectScript = useScriptsHubStore((s) => s.selectScript);
  const setTypeFilter = useScriptsHubStore((s) => s.setTypeFilter);

  useEffect(() => {
    if (!hasAttemptedLoad && !isLoadingList) {
      void loadScripts();
    }
  }, [hasAttemptedLoad, isLoadingList, loadScripts]);

  const filteredScripts = useMemo(() => {
    if (!typeFilter) return scripts;
    return scripts.filter((s) => s.scriptType === typeFilter);
  }, [scripts, typeFilter]);

  const counts = useMemo(() => ({
    powershell: scripts.filter((s) => s.scriptType === 'powershell').length,
    shell: scripts.filter((s) => s.scriptType === 'shell').length,
    compliance: scripts.filter((s) => s.scriptType === 'compliance').length,
    health: scripts.filter((s) => s.scriptType === 'health').length,
  }), [scripts]);

  const rowSelectionModel = useMemo(
    () => ({ type: 'include' as const, ids: new Set(selectedScriptId ? [selectedScriptId] : []) }),
    [selectedScriptId],
  );

  return (
    <div className="workspace">
      <div className="workspace-toolbar">
        <div className="workspace-heading">
          <strong className="workspace-title">Scripts Hub</strong>
          <div className="workspace-stats">
            <span className="inline-stat">{counts.powershell} PS</span>
            <span className="inline-stat">{counts.shell} Shell</span>
            <span className="inline-stat">{counts.compliance} Compliance</span>
            <span className="inline-stat">{counts.health} Health</span>
          </div>
        </div>
      </div>

      <div className="platform-filter-bar">
        {typeChips.map((chip) => (
          <button
            key={chip.label}
            className={`platform-chip${typeFilter === chip.value ? ' active' : ''}${chip.value === null && typeFilter === null ? ' active' : ''}`}
            onClick={() => setTypeFilter(chip.value)}
          >
            {chip.label}
          </button>
        ))}
      </div>

      {error && <div className="workspace-error">{error}</div>}

      <div className={`settings-columns${selectedScriptId ? ' detail-active' : ''}`}>
        <div className="panel panel-list">
          <div className="panel-header">
            <strong>Script list</strong>
            <span>{filteredScripts.length} scripts</span>
          </div>
          <div style={{ width: '100%' }}>
            <DataGrid<ScriptListItem>
              rows={filteredScripts}
              columns={columns}
              loading={isLoadingList}
              getRowId={(row) => row.id}
              rowSelectionModel={rowSelectionModel}
              onRowClick={(params: GridRowParams<ScriptListItem>) =>
                void selectScript(params.row.id, params.row.scriptType)
              }
              getRowHeight={() => 'auto'}
              showToolbar
              slots={{ toolbar: ScriptToolbar }}
              initialState={{
                pagination: { paginationModel: { pageSize: 25 } },
                columns: { columnVisibilityModel: { hasRemediation: false } },
              }}
              pageSizeOptions={[10, 25, 50, 100]}
              disableColumnMenu={false}
              disableRowSelectionOnClick
              sx={{
                border: 'none', fontSize: 12,
                '& .MuiDataGrid-columnHeaders': {
                  backgroundColor: '#111827', fontSize: 11, fontWeight: 600,
                  letterSpacing: '0.06em', textTransform: 'uppercase',
                  color: 'var(--text-muted)', borderBottom: '1px solid var(--border)',
                },
                '& .MuiDataGrid-columnHeader': { '&:focus, &:focus-within': { outline: 'none' } },
                '& .MuiDataGrid-cell': {
                  borderBottom: '1px solid var(--border)', color: 'var(--text-secondary)',
                  display: 'flex', alignItems: 'center', padding: '8px 12px',
                  '&:focus, &:focus-within': { outline: 'none' },
                },
                '& .MuiDataGrid-row': {
                  cursor: 'pointer',
                  '&:hover': { backgroundColor: 'rgba(255,255,255,0.02)' },
                  '&.Mui-selected': {
                    backgroundColor: 'var(--brand-soft)',
                    '&:hover': { backgroundColor: 'var(--brand-soft)' },
                  },
                },
                '& .MuiDataGrid-footerContainer': {
                  borderTop: '1px solid var(--border)', color: 'var(--text-tertiary)', fontSize: 12,
                },
                '& .MuiTablePagination-root': { color: 'var(--text-tertiary)' },
                '& .MuiDataGrid-overlay': { backgroundColor: 'transparent' },
              }}
            />
          </div>
        </div>
        {selectedScriptId && <ScriptDetailView />}
      </div>

      <div className="workspace-footer">
        <span>{scripts.length} total scripts</span>
        {isLoadingList && <span>Loading...</span>}
      </div>
    </div>
  );
}
