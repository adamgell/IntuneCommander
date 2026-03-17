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
import { useApplicationsStore } from '../../store/applicationsStore';
import { AppDetailPanel } from './AppDetailPanel';
import type { AppListItem } from '../../types/applications';
import '../../styles/workspace.css';

const platformChips: { label: string; value: string | null }[] = [
  { label: 'All', value: null },
  { label: 'Windows', value: 'Windows' },
  { label: 'iOS', value: 'iOS' },
  { label: 'Android', value: 'Android' },
  { label: 'macOS', value: 'macOS' },
  { label: 'Web', value: 'Web' },
];

const columns: GridColDef<AppListItem>[] = [
  {
    field: 'displayName',
    headerName: 'Name',
    flex: 2,
    minWidth: 220,
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
    field: 'appType',
    headerName: 'Type',
    width: 160,
  },
  {
    field: 'platform',
    headerName: 'Platform',
    width: 120,
  },
  {
    field: 'publisher',
    headerName: 'Publisher',
    width: 150,
  },
  {
    field: 'isAssigned',
    headerName: 'Assigned',
    width: 100,
    type: 'boolean',
  },
  {
    field: 'publishingState',
    headerName: 'State',
    width: 120,
    renderCell: (params) => {
      const state = params.value as string;
      const color = state === 'Published' ? 'var(--success)' : 'var(--text-tertiary)';
      return <span style={{ color }}>{state}</span>;
    },
  },
  {
    field: 'createdDateTime',
    headerName: 'Created',
    width: 170,
    valueFormatter: (value: string) =>
      value ? new Date(value).toLocaleString() : '',
  },
  {
    field: 'lastModifiedDateTime',
    headerName: 'Modified',
    width: 170,
    valueFormatter: (value: string) =>
      value ? new Date(value).toLocaleString() : '',
  },
];

function AppToolbar() {
  return (
    <Toolbar style={{ gap: 8, padding: '8px 12px' }}>
      <QuickFilter defaultExpanded style={{ flex: 1 }}>
        <QuickFilterControl
          placeholder="Filter applications..."
          size="small"
          style={{ minWidth: 200, maxWidth: 360 }}
        />
        <QuickFilterClear size="small" />
      </QuickFilter>
      <ColumnsPanelTrigger
        render={
          <Button
            size="small"
            variant="text"
            startIcon={<ViewColumnIcon sx={{ fontSize: 16 }} />}
            sx={{
              color: 'var(--text-secondary)',
              fontSize: 12,
              textTransform: 'none',
              px: 1.5,
              '&:hover': { backgroundColor: 'rgba(255,255,255,0.05)' },
            }}
          />
        }
      >
        Columns
      </ColumnsPanelTrigger>
    </Toolbar>
  );
}

export function ApplicationsWorkspace() {
  const apps = useApplicationsStore((s) => s.apps);
  const selectedAppId = useApplicationsStore((s) => s.selectedAppId);
  const isLoadingList = useApplicationsStore((s) => s.isLoadingList);
  const hasAttemptedLoad = useApplicationsStore((s) => s.hasAttemptedLoad);
  const error = useApplicationsStore((s) => s.error);
  const platformFilter = useApplicationsStore((s) => s.platformFilter);
  const loadApps = useApplicationsStore((s) => s.loadApps);
  const selectApp = useApplicationsStore((s) => s.selectApp);
  const setPlatformFilter = useApplicationsStore((s) => s.setPlatformFilter);

  useEffect(() => {
    if (!hasAttemptedLoad && !isLoadingList) {
      void loadApps();
    }
  }, [hasAttemptedLoad, isLoadingList, loadApps]);

  const filteredApps = useMemo(() => {
    if (!platformFilter) return apps;
    return apps.filter((app) =>
      app.platform.toLowerCase().includes(platformFilter.toLowerCase()),
    );
  }, [apps, platformFilter]);

  const rowSelectionModel = useMemo(
    () => ({
      type: 'include' as const,
      ids: new Set(selectedAppId ? [selectedAppId] : []),
    }),
    [selectedAppId],
  );

  return (
    <div className="workspace">
      <div className="workspace-toolbar">
        <div className="workspace-heading">
          <strong className="workspace-title">Applications</strong>
          <div className="workspace-stats">
            <span className="inline-stat">
              <strong>{filteredApps.length}</strong> apps
              {platformFilter && ` (${platformFilter})`}
            </span>
          </div>
        </div>
        <div className="workspace-actions">
          <button className="ws-btn primary" disabled>Export selected</button>
        </div>
      </div>

      <div className="platform-filter-bar">
        {platformChips.map((chip) => (
          <button
            key={chip.label}
            className={`platform-chip${platformFilter === chip.value ? ' active' : ''}`}
            onClick={() => setPlatformFilter(chip.value)}
          >
            {chip.label}
          </button>
        ))}
      </div>

      {error && <div className="workspace-error">{error}</div>}

      <div className={`settings-columns${selectedAppId ? ' detail-active' : ''}`}>
        <div className="panel panel-list">
          <div className="panel-header">
            <strong>Application list</strong>
            <span>{filteredApps.length} apps</span>
          </div>
          <div style={{ width: '100%' }}>
            <DataGrid<AppListItem>
              rows={filteredApps}
              columns={columns}
              loading={isLoadingList}
              getRowId={(row) => row.id}
              rowSelectionModel={rowSelectionModel}
              onRowClick={(params: GridRowParams<AppListItem>) =>
                void selectApp(params.row.id)
              }
              getRowHeight={() => 'auto'}
              showToolbar
              slots={{ toolbar: AppToolbar }}
              initialState={{
                pagination: { paginationModel: { pageSize: 25 } },
                columns: {
                  columnVisibilityModel: {
                    createdDateTime: false,
                    publisher: false,
                  },
                },
              }}
              pageSizeOptions={[10, 25, 50, 100]}
              disableColumnMenu={false}
              disableRowSelectionOnClick
              sx={{
                border: 'none',
                fontSize: 12,
                '& .MuiDataGrid-columnHeaders': {
                  backgroundColor: '#111827',
                  fontSize: 11,
                  fontWeight: 600,
                  letterSpacing: '0.06em',
                  textTransform: 'uppercase',
                  color: 'var(--text-muted)',
                  borderBottom: '1px solid var(--border)',
                },
                '& .MuiDataGrid-columnHeader': {
                  '&:focus, &:focus-within': { outline: 'none' },
                },
                '& .MuiDataGrid-cell': {
                  borderBottom: '1px solid var(--border)',
                  color: 'var(--text-secondary)',
                  display: 'flex',
                  alignItems: 'center',
                  padding: '8px 12px',
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
                  borderTop: '1px solid var(--border)',
                  color: 'var(--text-tertiary)',
                  fontSize: 12,
                },
                '& .MuiTablePagination-root': {
                  color: 'var(--text-tertiary)',
                },
                '& .MuiDataGrid-overlay': {
                  backgroundColor: 'transparent',
                },
              }}
            />
          </div>
        </div>
        {selectedAppId && <AppDetailPanel />}
      </div>

      <div className="workspace-footer">
        <span>{apps.length} total apps loaded</span>
        {isLoadingList && <span>Loading...</span>}
      </div>
    </div>
  );
}
