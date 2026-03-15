import { useMemo } from 'react';
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
import { useSettingsCatalogStore } from '../../store/settingsCatalogStore';
import type { PolicyListItem } from '../../types/settingsCatalog';

const columns: GridColDef<PolicyListItem>[] = [
  {
    field: 'name',
    headerName: 'Name',
    flex: 2,
    minWidth: 220,
    renderCell: (params) => (
      <div style={{ display: 'flex', flexDirection: 'column', gap: 2, lineHeight: 1.4, padding: '6px 0', whiteSpace: 'normal', wordBreak: 'break-word' }}>
        <strong style={{ fontSize: 13, color: 'var(--text-primary)' }}>{params.row.name}</strong>
        {params.row.description && (
          <span style={{ fontSize: 11, color: 'var(--text-tertiary)', lineHeight: 1.3, overflow: 'hidden', display: '-webkit-box', WebkitLineClamp: 2, WebkitBoxOrient: 'vertical' }}>
            {params.row.description}
          </span>
        )}
      </div>
    ),
  },
  {
    field: 'platform',
    headerName: 'Platform',
    width: 140,
  },
  {
    field: 'profileType',
    headerName: 'Profile type',
    flex: 1,
    minWidth: 160,
  },
  {
    field: 'technologies',
    headerName: 'Technologies',
    width: 140,
  },
  {
    field: 'isAssigned',
    headerName: 'Assigned',
    width: 100,
    type: 'boolean',
  },
  {
    field: 'settingCount',
    headerName: 'Settings',
    width: 90,
    type: 'number',
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
  {
    field: 'scopeTag',
    headerName: 'Scope tag',
    width: 120,
    renderCell: (params) => (
      <span className="status-pill">{params.value}</span>
    ),
  },
];

function CustomToolbar() {
  return (
    <Toolbar style={{ gap: 8, padding: '8px 12px' }}>
      <QuickFilter defaultExpanded style={{ flex: 1 }}>
        <QuickFilterControl
          placeholder="Filter policies..."
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

export function PolicyTable() {
  const policies = useSettingsCatalogStore((s) => s.policies);
  const selectedPolicyId = useSettingsCatalogStore((s) => s.selectedPolicyId);
  const isLoadingList = useSettingsCatalogStore((s) => s.isLoadingList);
  const selectPolicy = useSettingsCatalogStore((s) => s.selectPolicy);

  const rowSelectionModel = useMemo(
    () => ({
      type: 'include' as const,
      ids: new Set(selectedPolicyId ? [selectedPolicyId] : []),
    }),
    [selectedPolicyId],
  );

  return (
    <div className="panel panel-list">
      <div className="panel-header">
        <strong>Policy list</strong>
        <span>{policies.length} policies</span>
      </div>
      <div style={{ width: '100%' }}>
        <DataGrid<PolicyListItem>
          rows={policies}
          columns={columns}
          loading={isLoadingList}
          getRowId={(row) => row.id}
          rowSelectionModel={rowSelectionModel}
          onRowClick={(params: GridRowParams<PolicyListItem>) =>
            void selectPolicy(params.row.id)
          }
          getRowHeight={() => 'auto'}
          showToolbar
          slots={{ toolbar: CustomToolbar }}
          initialState={{
            pagination: { paginationModel: { pageSize: 25 } },
            columns: {
              columnVisibilityModel: {
                technologies: false,
                createdDateTime: false,
                settingCount: false,
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
  );
}
