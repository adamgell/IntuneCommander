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
import { useVppTokensStore } from '../../store/vppTokensStore';
import type { VppTokenListItem } from '../../types/applications';
import '../../styles/workspace.css';

const columns: GridColDef<VppTokenListItem>[] = [
  {
    field: 'organizationName',
    headerName: 'Organization',
    flex: 1.6,
    minWidth: 220,
    renderCell: (params) => (
      <div style={{ display: 'flex', flexDirection: 'column', gap: 2, lineHeight: 1.4, padding: '6px 0', whiteSpace: 'normal', wordBreak: 'break-word' }}>
        <strong style={{ fontSize: 13, color: 'var(--text-primary)' }}>{params.row.organizationName || params.row.displayName}</strong>
        <span style={{ fontSize: 11, color: 'var(--text-tertiary)' }}>{params.row.appleId || 'No Apple ID surfaced'}</span>
      </div>
    ),
  },
  { field: 'state', headerName: 'State', width: 120 },
  { field: 'appleId', headerName: 'Apple ID', flex: 1.2, minWidth: 220 },
  { field: 'expirationDateTime', headerName: 'Token expiry', width: 180, valueFormatter: (value: string) => value ? new Date(value).toLocaleString() : '' },
  { field: 'lastSyncDateTime', headerName: 'Last sync', width: 170, valueFormatter: (value: string) => value ? new Date(value).toLocaleString() : '' },
];

function TokenToolbar() {
  return (
    <Toolbar style={{ gap: 8, padding: '8px 12px' }}>
      <QuickFilter defaultExpanded style={{ flex: 1 }}>
        <QuickFilterControl placeholder="Filter VPP tokens..." size="small" style={{ minWidth: 200, maxWidth: 360 }} />
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
  const detail = useVppTokensStore((s) => s.detail);
  const isLoading = useVppTokensStore((s) => s.isLoadingDetail);
  const clearSelection = useVppTokensStore((s) => s.clearSelection);

  if (isLoading || !detail) {
    return (
      <div className="panel panel-detail">
        <div className="panel-header">
          <button className="back-btn" onClick={clearSelection}>Back to list</button>
        </div>
        <div className="panel-body loading-state">
          <div className="loading-spinner" />
          <p>Loading VPP token details...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="panel panel-detail">
      <div className="panel-header">
        <div className="detail-header-row">
          <button className="back-btn" onClick={clearSelection}>Back to list</button>
        </div>
      </div>
      <div className="panel-body">
        <div className="fluent-detail-view">
          <div className="fluent-title-block">
            <strong>{detail.organizationName || detail.displayName}</strong>
            <span>{detail.appleId || detail.displayName}</span>
          </div>

          <section className="fluent-section">
            <div className="fluent-section-header"><strong>Properties</strong></div>
            <div className="fluent-subsection">
              <div className="fluent-properties">
                <div className="fluent-property-row"><div className="fluent-property-label">State</div><div className="fluent-property-value">{detail.state || '—'}</div></div>
                <div className="fluent-property-row"><div className="fluent-property-label">Token expiry</div><div className="fluent-property-value">{detail.expirationDateTime ? new Date(detail.expirationDateTime).toLocaleString() : '—'}</div></div>
                <div className="fluent-property-row"><div className="fluent-property-label">Token account type</div><div className="fluent-property-value">{detail.vppTokenAccountType || '—'}</div></div>
                <div className="fluent-property-row"><div className="fluent-property-label">Last sync</div><div className="fluent-property-value">{detail.lastSyncDateTime ? new Date(detail.lastSyncDateTime).toLocaleString() : '—'}</div></div>
                <div className="fluent-property-row"><div className="fluent-property-label">Last sync status</div><div className="fluent-property-value">{detail.lastSyncStatus || '—'}</div></div>
                <div className="fluent-property-row"><div className="fluent-property-label">Country/region</div><div className="fluent-property-value">{detail.countryOrRegion || '—'}</div></div>
                <div className="fluent-property-row"><div className="fluent-property-label">Location</div><div className="fluent-property-value">{detail.locationName || '—'}</div></div>
                <div className="fluent-property-row"><div className="fluent-property-label">Automatically update apps</div><div className="fluent-property-value">{detail.automaticallyUpdateApps ? 'Yes' : 'No'}</div></div>
                <div className="fluent-property-row"><div className="fluent-property-label">Scope tags</div><div className="fluent-property-value">{detail.roleScopeTagIds.join(', ') || 'Default'}</div></div>
              </div>
            </div>
          </section>
        </div>
      </div>
    </div>
  );
}

export function VppTokensWorkspace() {
  const items = useVppTokensStore((s) => s.items);
  const selectedId = useVppTokensStore((s) => s.selectedId);
  const isLoadingList = useVppTokensStore((s) => s.isLoadingList);
  const hasAttemptedLoad = useVppTokensStore((s) => s.hasAttemptedLoad);
  const error = useVppTokensStore((s) => s.error);
  const loadItems = useVppTokensStore((s) => s.loadItems);
  const selectItem = useVppTokensStore((s) => s.selectItem);

  useEffect(() => {
    if (!hasAttemptedLoad && !isLoadingList) {
      void loadItems();
    }
  }, [hasAttemptedLoad, isLoadingList, loadItems]);

  const rowSelectionModel = useMemo(
    () => ({ type: 'include' as const, ids: new Set(selectedId ? [selectedId] : []) }),
    [selectedId],
  );

  return (
    <div className="workspace">
      <div className="workspace-toolbar">
        <div className="workspace-heading">
          <strong className="workspace-title">VPP Tokens</strong>
          <div className="workspace-stats"><span className="inline-stat">{items.length} tokens</span></div>
        </div>
      </div>
      {error && <div className="workspace-error">{error}</div>}
      <div className={`settings-columns${selectedId ? ' detail-active' : ''}`}>
        <div className="panel panel-list">
          <div className="panel-header"><strong>Tokens</strong><span>{items.length}</span></div>
          <div style={{ width: '100%' }}>
            <DataGrid<VppTokenListItem>
              rows={items}
              columns={columns}
              loading={isLoadingList}
              getRowId={(row) => row.id}
              rowSelectionModel={rowSelectionModel}
              onRowClick={(params: GridRowParams<VppTokenListItem>) => void selectItem(params.row.id)}
              getRowHeight={() => 'auto'}
              showToolbar
              slots={{ toolbar: TokenToolbar }}
              initialState={{ pagination: { paginationModel: { pageSize: 25 } } }}
              pageSizeOptions={[10, 25, 50, 100]}
              disableRowSelectionOnClick
              sx={{
                border: 'none',
                fontSize: 12,
                '& .MuiDataGrid-columnHeaders': { backgroundColor: '#111827', fontSize: 11, fontWeight: 600, letterSpacing: '0.06em', textTransform: 'uppercase', color: 'var(--text-muted)', borderBottom: '1px solid var(--border)' },
                '& .MuiDataGrid-columnHeader': { '&:focus, &:focus-within': { outline: 'none' } },
                '& .MuiDataGrid-cell': { borderBottom: '1px solid var(--border)', color: 'var(--text-secondary)', display: 'flex', alignItems: 'center', padding: '8px 12px', '&:focus, &:focus-within': { outline: 'none' } },
                '& .MuiDataGrid-row': { cursor: 'pointer', '&:hover': { backgroundColor: 'rgba(255,255,255,0.02)' }, '&.Mui-selected': { backgroundColor: 'var(--brand-soft)', '&:hover': { backgroundColor: 'var(--brand-soft)' } } },
                '& .MuiDataGrid-footerContainer': { borderTop: '1px solid var(--border)', color: 'var(--text-tertiary)', fontSize: 12 },
                '& .MuiTablePagination-root': { color: 'var(--text-tertiary)' },
                '& .MuiDataGrid-overlay': { backgroundColor: 'transparent' },
              }}
            />
          </div>
        </div>
        {selectedId && <DetailView />}
      </div>
      <div className="workspace-footer"><span>{items.length} tokens</span>{isLoadingList && <span>Loading...</span>}</div>
    </div>
  );
}
