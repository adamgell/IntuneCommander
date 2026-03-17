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
import { useAppAssignmentsStore } from '../../store/appAssignmentsStore';
import type { ApplicationAssignmentRow } from '../../types/applications';
import '../../styles/workspace.css';

const columns: GridColDef<ApplicationAssignmentRow>[] = [
  {
    field: 'appName',
    headerName: 'Application',
    flex: 2,
    minWidth: 240,
    renderCell: (params) => (
      <div style={{ display: 'flex', flexDirection: 'column', gap: 2, lineHeight: 1.4, padding: '6px 0', whiteSpace: 'normal', wordBreak: 'break-word' }}>
        <strong style={{ fontSize: 13, color: 'var(--text-primary)' }}>{params.row.appName}</strong>
        <span style={{ fontSize: 11, color: 'var(--text-tertiary)' }}>
          {params.row.appType} · {params.row.platform}
        </span>
      </div>
    ),
  },
  { field: 'publisher', headerName: 'Publisher', width: 160 },
  { field: 'assignmentType', headerName: 'Assignment Type', width: 140 },
  { field: 'targetName', headerName: 'Target', flex: 1.4, minWidth: 220 },
  { field: 'installIntent', headerName: 'Intent', width: 120 },
  { field: 'version', headerName: 'Version', width: 120 },
  {
    field: 'isExclusion',
    headerName: 'Excluded',
    width: 110,
    renderCell: (params) => (
      <span style={{ color: params.value === 'True' ? '#ef4444' : 'var(--text-tertiary)' }}>
        {params.value === 'True' ? 'Yes' : 'No'}
      </span>
    ),
  },
];

function AssignmentToolbar() {
  return (
    <Toolbar style={{ gap: 8, padding: '8px 12px' }}>
      <QuickFilter defaultExpanded style={{ flex: 1 }}>
        <QuickFilterControl placeholder="Filter application assignments..." size="small" style={{ minWidth: 200, maxWidth: 360 }} />
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

function formatDateTime(value: string) {
  return value ? new Date(value).toLocaleString() : '—';
}

function DetailView() {
  const detail = useAppAssignmentsStore((s) => s.detail);
  const isLoading = useAppAssignmentsStore((s) => s.isLoadingDetail);
  const clearSelection = useAppAssignmentsStore((s) => s.clearSelection);

  if (isLoading || !detail) {
    return (
      <div className="panel panel-detail">
        <div className="panel-header">
          <button className="back-btn" onClick={clearSelection}>Back to list</button>
        </div>
        <div className="panel-body loading-state">
          <div className="loading-spinner" />
          <p>Loading assignment details...</p>
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
            <strong>{detail.appName}</strong>
            <span>{detail.appType} · {detail.platform}</span>
          </div>

          <section className="fluent-section">
            <div className="fluent-section-header"><strong>App information</strong></div>
            <div className="fluent-subsection">
              <div className="fluent-properties">
                <div className="fluent-property-row"><div className="fluent-property-label">Publisher</div><div className="fluent-property-value">{detail.publisher || '—'}</div></div>
                <div className="fluent-property-row"><div className="fluent-property-label">Version</div><div className="fluent-property-value">{detail.version || '—'}</div></div>
                <div className="fluent-property-row"><div className="fluent-property-label">Bundle ID</div><div className="fluent-property-value">{detail.bundleId || '—'}</div></div>
                <div className="fluent-property-row"><div className="fluent-property-label">Package ID</div><div className="fluent-property-value">{detail.packageId || '—'}</div></div>
                <div className="fluent-property-row"><div className="fluent-property-label">Featured</div><div className="fluent-property-value">{detail.isFeatured}</div></div>
                <div className="fluent-property-row"><div className="fluent-property-label">Created</div><div className="fluent-property-value">{formatDateTime(detail.createdDate)}</div></div>
                <div className="fluent-property-row"><div className="fluent-property-label">Last modified</div><div className="fluent-property-value">{formatDateTime(detail.lastModified)}</div></div>
                <div className="fluent-property-row"><div className="fluent-property-label">Description</div><div className="fluent-property-value">{detail.description || '—'}</div></div>
              </div>
            </div>
          </section>

          <div className="detail-divider" />

          <section className="fluent-section">
            <div className="fluent-section-header"><strong>Assignment</strong></div>
            <div className="fluent-subsection">
              <div className="fluent-properties">
                <div className="fluent-property-row"><div className="fluent-property-label">Assignment type</div><div className="fluent-property-value">{detail.assignmentType}</div></div>
                <div className="fluent-property-row"><div className="fluent-property-label">Target</div><div className="fluent-property-value">{detail.targetName || 'None'}</div></div>
                <div className="fluent-property-row"><div className="fluent-property-label">Target group ID</div><div className="fluent-property-value">{detail.targetGroupId || '—'}</div></div>
                <div className="fluent-property-row"><div className="fluent-property-label">Intent</div><div className="fluent-property-value">{detail.installIntent || '—'}</div></div>
                <div className="fluent-property-row"><div className="fluent-property-label">Excluded</div><div className="fluent-property-value">{detail.isExclusion}</div></div>
                <div className="fluent-property-row"><div className="fluent-property-label">Assignment settings</div><div className="fluent-property-value">{detail.assignmentSettings || 'N/A'}</div></div>
              </div>
            </div>
          </section>

          <div className="detail-divider" />

          <section className="fluent-section">
            <div className="fluent-section-header"><strong>Requirements & links</strong></div>
            <div className="fluent-subsection">
              <div className="fluent-properties">
                <div className="fluent-property-row"><div className="fluent-property-label">Minimum OS</div><div className="fluent-property-value">{detail.minimumOsVersion || '—'}</div></div>
                <div className="fluent-property-row"><div className="fluent-property-label">Minimum free disk space (MB)</div><div className="fluent-property-value">{detail.minimumFreeDiskSpaceMB || '—'}</div></div>
                <div className="fluent-property-row"><div className="fluent-property-label">Minimum memory (MB)</div><div className="fluent-property-value">{detail.minimumMemoryMB || '—'}</div></div>
                <div className="fluent-property-row"><div className="fluent-property-label">Minimum processors</div><div className="fluent-property-value">{detail.minimumProcessors || '—'}</div></div>
                <div className="fluent-property-row"><div className="fluent-property-label">Information URL</div><div className="fluent-property-value">{detail.informationUrl || '—'}</div></div>
                <div className="fluent-property-row"><div className="fluent-property-label">Privacy URL</div><div className="fluent-property-value">{detail.privacyUrl || '—'}</div></div>
                <div className="fluent-property-row"><div className="fluent-property-label">App Store URL</div><div className="fluent-property-value">{detail.appStoreUrl || '—'}</div></div>
                <div className="fluent-property-row"><div className="fluent-property-label">Categories</div><div className="fluent-property-value">{detail.categories || '—'}</div></div>
                <div className="fluent-property-row"><div className="fluent-property-label">Notes</div><div className="fluent-property-value">{detail.notes || '—'}</div></div>
              </div>
            </div>
          </section>
        </div>
      </div>
    </div>
  );
}

export function AppAssignmentsWorkspace() {
  const items = useAppAssignmentsStore((s) => s.items);
  const selectedId = useAppAssignmentsStore((s) => s.selectedId);
  const isLoadingList = useAppAssignmentsStore((s) => s.isLoadingList);
  const hasAttemptedLoad = useAppAssignmentsStore((s) => s.hasAttemptedLoad);
  const error = useAppAssignmentsStore((s) => s.error);
  const loadItems = useAppAssignmentsStore((s) => s.loadItems);
  const selectItem = useAppAssignmentsStore((s) => s.selectItem);

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
          <strong className="workspace-title">Application Assignments</strong>
          <div className="workspace-stats"><span className="inline-stat">{items.length} flattened rows</span></div>
        </div>
      </div>
      {error && <div className="workspace-error">{error}</div>}
      <div className={`settings-columns${selectedId ? ' detail-active' : ''}`}>
        <div className="panel panel-list">
          <div className="panel-header"><strong>Assignments</strong><span>{items.length}</span></div>
          <div style={{ width: '100%' }}>
            <DataGrid<ApplicationAssignmentRow>
              rows={items}
              columns={columns}
              loading={isLoadingList}
              getRowId={(row) => row.id}
              rowSelectionModel={rowSelectionModel}
              onRowClick={(params: GridRowParams<ApplicationAssignmentRow>) => void selectItem(params.row.id)}
              getRowHeight={() => 'auto'}
              showToolbar
              slots={{ toolbar: AssignmentToolbar }}
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
      <div className="workspace-footer"><span>{items.length} assignment rows</span>{isLoadingList && <span>Loading...</span>}</div>
    </div>
  );
}
