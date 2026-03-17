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
import { useEnrollmentStore } from '../../store/enrollmentStore';
import type { EnrollmentConfigListItem } from '../../types/phase4';
import '../../styles/workspace.css';

const columns: GridColDef<EnrollmentConfigListItem>[] = [
  {
    field: 'displayName',
    headerName: 'Configuration',
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
  {
    field: 'configurationType',
    headerName: 'Type',
    width: 200,
    renderCell: (params) => (
      <span style={{
        padding: '2px 8px', borderRadius: 10, fontSize: 11, fontWeight: 600,
        backgroundColor: 'rgba(99,102,241,0.12)', color: '#818cf8',
      }}>
        {params.value}
      </span>
    ),
  },
  {
    field: 'priority',
    headerName: 'Priority',
    width: 90,
    renderCell: (params) => (
      <span style={{ color: 'var(--text-secondary)' }}>{params.value}</span>
    ),
  },
  {
    field: 'lastModifiedDateTime',
    headerName: 'Modified',
    width: 170,
    valueFormatter: (value: string) => value ? new Date(value).toLocaleString() : '',
  },
];

function EnrollmentToolbar() {
  return (
    <Toolbar style={{ gap: 8, padding: '8px 12px' }}>
      <QuickFilter defaultExpanded style={{ flex: 1 }}>
        <QuickFilterControl placeholder="Filter configurations..." size="small" style={{ minWidth: 200, maxWidth: 360 }} />
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
  const detail = useEnrollmentStore((s) => s.detail);
  const isLoading = useEnrollmentStore((s) => s.isLoadingDetail);
  const clearSelection = useEnrollmentStore((s) => s.clearSelection);

  if (isLoading) return <div className="panel panel-detail"><div style={{ padding: 20, color: 'var(--text-tertiary)' }}>Loading...</div></div>;
  if (!detail) return null;

  return (
    <div className="panel panel-detail" style={{ overflow: 'auto' }}>
      <div style={{ padding: '12px 16px', borderBottom: '1px solid var(--border)', display: 'flex', alignItems: 'center', gap: 12 }}>
        <button onClick={clearSelection} style={{ background: 'none', border: 'none', cursor: 'pointer', color: 'var(--text-tertiary)', fontSize: 18 }}>←</button>
        <div>
          <strong style={{ fontSize: 14, color: 'var(--text-primary)' }}>{detail.displayName}</strong>
          <div style={{ fontSize: 11, color: 'var(--text-tertiary)' }}>{detail.configurationType}</div>
        </div>
      </div>

      <div style={{ padding: '12px 16px', borderBottom: '1px solid var(--border)', fontSize: 12 }}>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 8 }}>
          {detail.description && <div style={{ gridColumn: '1/-1', color: 'var(--text-secondary)', marginBottom: 4 }}>{detail.description}</div>}
          <div><span style={{ color: 'var(--text-tertiary)' }}>Priority:</span> <span style={{ color: 'var(--text-secondary)' }}>{detail.priority}</span></div>
          <div><span style={{ color: 'var(--text-tertiary)' }}>Modified:</span> <span style={{ color: 'var(--text-secondary)' }}>{detail.lastModifiedDateTime ? new Date(detail.lastModifiedDateTime).toLocaleString() : '—'}</span></div>
          <div><span style={{ color: 'var(--text-tertiary)' }}>Scope Tags:</span> <span style={{ color: 'var(--text-secondary)' }}>{detail.roleScopeTagIds.join(', ') || 'Default'}</span></div>
        </div>
      </div>

      <div style={{ padding: '12px 16px' }}>
        <div style={{ fontSize: 12, fontWeight: 600, color: 'var(--text-primary)', marginBottom: 8 }}>Raw Configuration</div>
        <div style={{ border: '1px solid var(--border)', borderRadius: 8, overflow: 'hidden' }}>
          <Editor height={400} language="json" value={detail.rawJson} theme="vs-dark"
            options={{ readOnly: true, domReadOnly: true, minimap: { enabled: false }, scrollBeyondLastLine: false, fontSize: 12, wordWrap: 'on', padding: { top: 8, bottom: 8 } }} />
        </div>
      </div>
    </div>
  );
}

export function EnrollmentWorkspace() {
  const items = useEnrollmentStore((s) => s.items);
  const selectedId = useEnrollmentStore((s) => s.selectedId);
  const isLoadingList = useEnrollmentStore((s) => s.isLoadingList);
  const hasAttemptedLoad = useEnrollmentStore((s) => s.hasAttemptedLoad);
  const error = useEnrollmentStore((s) => s.error);
  const loadItems = useEnrollmentStore((s) => s.loadItems);
  const selectItem = useEnrollmentStore((s) => s.selectItem);

  useEffect(() => { if (!hasAttemptedLoad && !isLoadingList) void loadItems(); }, [hasAttemptedLoad, isLoadingList, loadItems]);

  const rowSelectionModel = useMemo(
    () => ({ type: 'include' as const, ids: new Set(selectedId ? [selectedId] : []) }), [selectedId]);

  return (
    <div className="workspace">
      <div className="workspace-toolbar">
        <div className="workspace-heading">
          <strong className="workspace-title">Enrollment & Autopilot</strong>
          <div className="workspace-stats"><span className="inline-stat">{items.length} configurations</span></div>
        </div>
      </div>
      {error && <div className="workspace-error">{error}</div>}
      <div className={`settings-columns${selectedId ? ' detail-active' : ''}`}>
        <div className="panel panel-list">
          <div className="panel-header"><strong>Configurations</strong><span>{items.length}</span></div>
          <div style={{ width: '100%' }}>
            <DataGrid<EnrollmentConfigListItem> rows={items} columns={columns} loading={isLoadingList}
              getRowId={(r) => r.id} rowSelectionModel={rowSelectionModel}
              onRowClick={(p: GridRowParams<EnrollmentConfigListItem>) => void selectItem(p.row.id)}
              getRowHeight={() => 'auto'} showToolbar slots={{ toolbar: EnrollmentToolbar }}
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
      <div className="workspace-footer"><span>{items.length} configurations</span>{isLoadingList && <span>Loading...</span>}</div>
    </div>
  );
}
