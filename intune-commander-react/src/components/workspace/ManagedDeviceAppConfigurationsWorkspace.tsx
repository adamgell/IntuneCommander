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
import { useManagedDeviceAppConfigurationsStore } from '../../store/managedDeviceAppConfigurationsStore';
import type { ManagedDeviceAppConfigurationListItem } from '../../types/applications';
import '../../styles/workspace.css';

const columns: GridColDef<ManagedDeviceAppConfigurationListItem>[] = [
  {
    field: 'displayName',
    headerName: 'Configuration',
    flex: 2,
    minWidth: 240,
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
  { field: 'configurationType', headerName: 'Type', width: 220 },
  { field: 'version', headerName: 'Version', width: 110 },
  { field: 'targetedMobileAppCount', headerName: 'Targeted apps', width: 120 },
  { field: 'assignmentCount', headerName: 'Assignments', width: 120 },
  { field: 'lastModifiedDateTime', headerName: 'Modified', width: 170, valueFormatter: (value: string) => value ? new Date(value).toLocaleString() : '' },
];

function ConfigToolbar() {
  return (
    <Toolbar style={{ gap: 8, padding: '8px 12px' }}>
      <QuickFilter defaultExpanded style={{ flex: 1 }}>
        <QuickFilterControl placeholder="Filter managed device app configurations..." size="small" style={{ minWidth: 200, maxWidth: 360 }} />
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
  const detail = useManagedDeviceAppConfigurationsStore((s) => s.detail);
  const isLoading = useManagedDeviceAppConfigurationsStore((s) => s.isLoadingDetail);
  const clearSelection = useManagedDeviceAppConfigurationsStore((s) => s.clearSelection);

  if (isLoading || !detail) {
    return (
      <div className="panel panel-detail">
        <div className="panel-header">
          <button className="back-btn" onClick={clearSelection}>Back to list</button>
        </div>
        <div className="panel-body loading-state">
          <div className="loading-spinner" />
          <p>Loading configuration details...</p>
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
            <strong>{detail.displayName}</strong>
            <span>{detail.configurationType}</span>
          </div>

          <section className="fluent-section">
            <div className="fluent-section-header"><strong>Properties</strong></div>
            <div className="fluent-subsection">
              <div className="fluent-properties">
                <div className="fluent-property-row"><div className="fluent-property-label">Description</div><div className="fluent-property-value">{detail.description || '—'}</div></div>
                <div className="fluent-property-row"><div className="fluent-property-label">Version</div><div className="fluent-property-value">{detail.version || '—'}</div></div>
                <div className="fluent-property-row"><div className="fluent-property-label">Created</div><div className="fluent-property-value">{detail.createdDateTime ? new Date(detail.createdDateTime).toLocaleString() : '—'}</div></div>
                <div className="fluent-property-row"><div className="fluent-property-label">Last modified</div><div className="fluent-property-value">{detail.lastModifiedDateTime ? new Date(detail.lastModifiedDateTime).toLocaleString() : '—'}</div></div>
                <div className="fluent-property-row"><div className="fluent-property-label">OData type</div><div className="fluent-property-value">{detail.odataType}</div></div>
                <div className="fluent-property-row"><div className="fluent-property-label">Scope tags</div><div className="fluent-property-value">{detail.roleScopeTagIds.join(', ') || 'Default'}</div></div>
              </div>
            </div>
          </section>

          <div className="detail-divider" />

          <section className="fluent-section">
            <div className="fluent-section-header"><strong>Targeted mobile apps</strong></div>
            <div className="fluent-subsection">
              {detail.targetedMobileApps.length > 0 ? (
                <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6 }}>
                  {detail.targetedMobileApps.map((appId) => (
                    <span key={appId} style={{ fontSize: 11, padding: '4px 10px', borderRadius: 999, backgroundColor: 'rgba(59,130,246,0.12)', color: '#93c5fd', border: '1px solid rgba(59,130,246,0.24)' }}>
                      {appId}
                    </span>
                  ))}
                </div>
              ) : (
                <p className="muted">No targeted mobile apps surfaced.</p>
              )}
            </div>
          </section>

          <div className="detail-divider" />

          <section className="fluent-section">
            <div className="fluent-section-header"><strong>Assignments</strong></div>
            <div className="fluent-subsection">
              {detail.assignments.length > 0 ? (
                <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6 }}>
                  {detail.assignments.map((assignment, index) => (
                    <span key={`${assignment.target}-${index}`} style={{
                      fontSize: 11,
                      padding: '4px 10px',
                      borderRadius: 999,
                      backgroundColor: assignment.targetKind === 'Exclude' ? 'rgba(239,68,68,0.12)' : 'rgba(59,130,246,0.12)',
                      color: assignment.targetKind === 'Exclude' ? '#fca5a5' : '#93c5fd',
                      border: `1px solid ${assignment.targetKind === 'Exclude' ? 'rgba(239,68,68,0.24)' : 'rgba(59,130,246,0.24)'}`,
                    }}>
                      {assignment.targetKind === 'Exclude' ? 'Exclude · ' : 'Include · '}
                      {assignment.target}
                    </span>
                  ))}
                </div>
              ) : (
                <p className="muted">No assignments configured.</p>
              )}
            </div>
          </section>
        </div>
      </div>
    </div>
  );
}

export function ManagedDeviceAppConfigurationsWorkspace() {
  const items = useManagedDeviceAppConfigurationsStore((s) => s.items);
  const selectedId = useManagedDeviceAppConfigurationsStore((s) => s.selectedId);
  const isLoadingList = useManagedDeviceAppConfigurationsStore((s) => s.isLoadingList);
  const hasAttemptedLoad = useManagedDeviceAppConfigurationsStore((s) => s.hasAttemptedLoad);
  const error = useManagedDeviceAppConfigurationsStore((s) => s.error);
  const loadItems = useManagedDeviceAppConfigurationsStore((s) => s.loadItems);
  const selectItem = useManagedDeviceAppConfigurationsStore((s) => s.selectItem);

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
          <strong className="workspace-title">Managed Device App Configurations</strong>
          <div className="workspace-stats"><span className="inline-stat">{items.length} configurations</span></div>
        </div>
      </div>
      {error && <div className="workspace-error">{error}</div>}
      <div className={`settings-columns${selectedId ? ' detail-active' : ''}`}>
        <div className="panel panel-list">
          <div className="panel-header"><strong>Configurations</strong><span>{items.length}</span></div>
          <div style={{ width: '100%' }}>
            <DataGrid<ManagedDeviceAppConfigurationListItem>
              rows={items}
              columns={columns}
              loading={isLoadingList}
              getRowId={(row) => row.id}
              rowSelectionModel={rowSelectionModel}
              onRowClick={(params: GridRowParams<ManagedDeviceAppConfigurationListItem>) => void selectItem(params.row.id)}
              getRowHeight={() => 'auto'}
              showToolbar
              slots={{ toolbar: ConfigToolbar }}
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
      <div className="workspace-footer"><span>{items.length} configurations</span>{isLoadingList && <span>Loading...</span>}</div>
    </div>
  );
}
