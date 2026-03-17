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
import { useEndpointSecurityStore } from '../../store/endpointSecurityStore';
import type { EndpointSecurityListItem } from '../../types/phase4';
import '../../styles/workspace.css';

const columns: GridColDef<EndpointSecurityListItem>[] = [
  {
    field: 'displayName',
    headerName: 'Intent Name',
    flex: 2,
    minWidth: 250,
    renderCell: (params) => (
      <div style={{ display: 'flex', flexDirection: 'column', gap: 2, lineHeight: 1.4, padding: '6px 0', whiteSpace: 'normal', wordBreak: 'break-word' }}>
        <strong style={{ fontSize: 13, color: 'var(--text-primary)' }}>{params.row.displayName}</strong>
        {params.row.description && (
          <span style={{ fontSize: 11, color: 'var(--text-tertiary)', overflow: 'hidden', display: '-webkit-box', WebkitLineClamp: 2, WebkitBoxOrient: 'vertical' }}>
            {params.row.description}
          </span>
        )}
      </div>
    ),
  },
  { field: 'intentType', headerName: 'Template', width: 200 },
  {
    field: 'isAssigned',
    headerName: 'Assigned',
    width: 100,
    renderCell: (params) => (
      <span style={{
        padding: '2px 8px', borderRadius: 10, fontSize: 11, fontWeight: 600,
        backgroundColor: params.value ? 'rgba(34,197,94,0.15)' : 'rgba(107,114,128,0.15)',
        color: params.value ? '#22c55e' : '#6b7280',
      }}>
        {params.value ? 'Yes' : 'No'}
      </span>
    ),
  },
  {
    field: 'assignmentCount',
    headerName: 'Groups',
    width: 100,
    renderCell: (params) => (
      <span style={{ color: params.value > 0 ? 'var(--text-secondary)' : 'var(--text-tertiary)' }}>{params.value}</span>
    ),
  },
  {
    field: 'lastModifiedDateTime',
    headerName: 'Modified',
    width: 170,
    valueFormatter: (value: string) => value ? new Date(value).toLocaleString() : '',
  },
];

function SecurityToolbar() {
  return (
    <Toolbar style={{ gap: 8, padding: '8px 12px' }}>
      <QuickFilter defaultExpanded style={{ flex: 1 }}>
        <QuickFilterControl placeholder="Filter intents..." size="small" style={{ minWidth: 200, maxWidth: 360 }} />
        <QuickFilterClear size="small" />
      </QuickFilter>
      <ColumnsPanelTrigger
        render={<Button size="small" variant="text" startIcon={<ViewColumnIcon sx={{ fontSize: 16 }} />}
          sx={{ color: 'var(--text-secondary)', fontSize: 12, textTransform: 'none', px: 1.5, '&:hover': { backgroundColor: 'rgba(255,255,255,0.05)' } }} />}
      >
        Columns
      </ColumnsPanelTrigger>
    </Toolbar>
  );
}

function DetailView() {
  const detail = useEndpointSecurityStore((s) => s.detail);
  const isLoading = useEndpointSecurityStore((s) => s.isLoadingDetail);
  const clearSelection = useEndpointSecurityStore((s) => s.clearSelection);

  if (isLoading) return <div className="panel panel-detail"><div style={{ padding: 20, color: 'var(--text-tertiary)' }}>Loading...</div></div>;
  if (!detail) return null;

  return (
    <div className="panel panel-detail" style={{ overflow: 'auto' }}>
      <div style={{ padding: '12px 16px', borderBottom: '1px solid var(--border)', display: 'flex', alignItems: 'center', gap: 12 }}>
        <button onClick={clearSelection} style={{ background: 'none', border: 'none', cursor: 'pointer', color: 'var(--text-tertiary)', fontSize: 18 }}>←</button>
        <div>
          <strong style={{ fontSize: 14, color: 'var(--text-primary)' }}>{detail.displayName}</strong>
          <div style={{ fontSize: 11, color: 'var(--text-tertiary)' }}>{detail.intentType}</div>
        </div>
      </div>

      <div style={{ padding: '12px 16px', borderBottom: '1px solid var(--border)', fontSize: 12 }}>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 8 }}>
          {detail.description && <div style={{ gridColumn: '1/-1', color: 'var(--text-secondary)', marginBottom: 4 }}>{detail.description}</div>}
          <div><span style={{ color: 'var(--text-tertiary)' }}>Assigned:</span> <span style={{ color: detail.isAssigned ? '#22c55e' : 'var(--text-secondary)' }}>{detail.isAssigned ? 'Yes' : 'No'}</span></div>
          <div><span style={{ color: 'var(--text-tertiary)' }}>Scope Tags:</span> <span style={{ color: 'var(--text-secondary)' }}>{detail.roleScopeTagIds.join(', ') || 'Default'}</span></div>
        </div>
      </div>

      {detail.assignments.length > 0 && (
        <div style={{ padding: '12px 16px', borderBottom: '1px solid var(--border)' }}>
          <div style={{ fontSize: 12, fontWeight: 600, color: 'var(--text-primary)', marginBottom: 8 }}>Assignments ({detail.assignments.length})</div>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6 }}>
            {detail.assignments.map((a, i) => (
              <span key={i} style={{ fontSize: 11, padding: '3px 8px', borderRadius: 12,
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

      <div style={{ padding: '12px 16px' }}>
        <div style={{ fontSize: 12, fontWeight: 600, color: 'var(--text-primary)', marginBottom: 8 }}>Raw Intent</div>
        <div style={{ border: '1px solid var(--border)', borderRadius: 8, overflow: 'hidden' }}>
          <Editor height={400} language="json" value={detail.rawJson} theme="vs-dark"
            options={{ readOnly: true, domReadOnly: true, minimap: { enabled: false }, scrollBeyondLastLine: false, fontSize: 12, wordWrap: 'on', padding: { top: 8, bottom: 8 } }} />
        </div>
      </div>
    </div>
  );
}

export function EndpointSecurityWorkspace() {
  const items = useEndpointSecurityStore((s) => s.items);
  const selectedId = useEndpointSecurityStore((s) => s.selectedId);
  const isLoadingList = useEndpointSecurityStore((s) => s.isLoadingList);
  const hasAttemptedLoad = useEndpointSecurityStore((s) => s.hasAttemptedLoad);
  const error = useEndpointSecurityStore((s) => s.error);
  const loadItems = useEndpointSecurityStore((s) => s.loadItems);
  const selectItem = useEndpointSecurityStore((s) => s.selectItem);

  useEffect(() => { if (!hasAttemptedLoad && !isLoadingList) void loadItems(); }, [hasAttemptedLoad, isLoadingList, loadItems]);

  const rowSelectionModel = useMemo(
    () => ({ type: 'include' as const, ids: new Set(selectedId ? [selectedId] : []) }), [selectedId]);

  return (
    <div className="workspace">
      <div className="workspace-toolbar">
        <div className="workspace-heading">
          <strong className="workspace-title">Endpoint Security</strong>
          <div className="workspace-stats"><span className="inline-stat">{items.length} intents</span></div>
        </div>
      </div>
      {error && <div className="workspace-error">{error}</div>}
      <div className={`settings-columns${selectedId ? ' detail-active' : ''}`}>
        <div className="panel panel-list">
          <div className="panel-header"><strong>Security Intents</strong><span>{items.length}</span></div>
          <div style={{ width: '100%' }}>
            <DataGrid<EndpointSecurityListItem> rows={items} columns={columns} loading={isLoadingList}
              getRowId={(r) => r.id} rowSelectionModel={rowSelectionModel}
              onRowClick={(p: GridRowParams<EndpointSecurityListItem>) => void selectItem(p.row.id)}
              getRowHeight={() => 'auto'} showToolbar slots={{ toolbar: SecurityToolbar }}
              initialState={{ pagination: { paginationModel: { pageSize: 25 } } }}
              pageSizeOptions={[10, 25, 50, 100]} disableRowSelectionOnClick
              sx={{ border: 'none', fontSize: 12,
                '& .MuiDataGrid-columnHeaders': { backgroundColor: '#111827', fontSize: 11, fontWeight: 600, letterSpacing: '0.06em', textTransform: 'uppercase', color: 'var(--text-muted)', borderBottom: '1px solid var(--border)' },
                '& .MuiDataGrid-columnHeader': { '&:focus, &:focus-within': { outline: 'none' } },
                '& .MuiDataGrid-cell': { borderBottom: '1px solid var(--border)', color: 'var(--text-secondary)', display: 'flex', alignItems: 'center', padding: '8px 12px', '&:focus, &:focus-within': { outline: 'none' } },
                '& .MuiDataGrid-row': { cursor: 'pointer', '&:hover': { backgroundColor: 'rgba(255,255,255,0.02)' }, '&.Mui-selected': { backgroundColor: 'var(--brand-soft)', '&:hover': { backgroundColor: 'var(--brand-soft)' } } },
                '& .MuiDataGrid-footerContainer': { borderTop: '1px solid var(--border)', color: 'var(--text-tertiary)', fontSize: 12 },
                '& .MuiTablePagination-root': { color: 'var(--text-tertiary)' },
                '& .MuiDataGrid-overlay': { backgroundColor: 'transparent' },
              }} />
          </div>
        </div>
        {selectedId && <DetailView />}
      </div>
      <div className="workspace-footer"><span>{items.length} intents</span>{isLoadingList && <span>Loading...</span>}</div>
    </div>
  );
}
