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
import Chip from '@mui/material/Chip';
import ViewColumnIcon from '@mui/icons-material/ViewColumn';
import CheckCircleIcon from '@mui/icons-material/CheckCircle';
import RemoveCircleOutlineIcon from '@mui/icons-material/RemoveCircleOutline';
import { useDetectionRemediationStore } from '../../store/detectionRemediationStore';
import type { HealthScriptListItem } from '../../types/detectionRemediation';

const columns: GridColDef<HealthScriptListItem>[] = [
  {
    field: 'displayName',
    headerName: 'Name',
    flex: 2,
    minWidth: 240,
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
    field: 'publisher',
    headerName: 'Author',
    width: 140,
  },
  {
    field: 'status',
    headerName: 'Status',
    width: 140,
    renderCell: (params) => {
      const isActive = params.value === 'Active';
      return (
        <Chip
          icon={isActive
            ? <CheckCircleIcon sx={{ fontSize: '14px !important' }} />
            : <RemoveCircleOutlineIcon sx={{ fontSize: '14px !important' }} />
          }
          label={params.value}
          size="small"
          variant="outlined"
          color={isActive ? 'success' : 'default'}
          sx={{ fontSize: 11, height: 24 }}
        />
      );
    },
  },
  {
    field: 'hasRemediation',
    headerName: 'Type',
    width: 170,
    renderCell: (params) => (
      <span style={{ fontSize: 11, color: 'var(--text-secondary)' }}>
        {params.value ? 'Detection & Remediation' : 'Detection only'}
      </span>
    ),
  },
  {
    field: 'noIssueDetectedCount',
    headerName: 'Without issues',
    width: 120,
    type: 'number',
    renderCell: (params) => (
      <span style={{ fontSize: 12, color: 'var(--text-secondary)' }}>{params.value}</span>
    ),
  },
  {
    field: 'issueDetectedCount',
    headerName: 'With issues',
    width: 110,
    type: 'number',
    renderCell: (params) => {
      const val = params.value as number;
      return (
        <span style={{ fontSize: 12, fontWeight: val > 0 ? 600 : 400, color: val > 0 ? '#ef5350' : 'var(--text-secondary)' }}>
          {val}
        </span>
      );
    },
  },
  {
    field: 'issueRemediatedCount',
    headerName: 'Issue fixed',
    width: 100,
    type: 'number',
    renderCell: (params) => (
      <span style={{ fontSize: 12, color: 'var(--text-secondary)' }}>{params.value}</span>
    ),
  },
  {
    field: 'issueReoccurredCount',
    headerName: 'Recurred',
    width: 100,
    type: 'number',
    renderCell: (params) => {
      const val = params.value as number;
      return (
        <span style={{ fontSize: 12, fontWeight: val > 0 ? 600 : 400, color: val > 0 ? '#ef5350' : 'var(--text-secondary)' }}>
          {val}
        </span>
      );
    },
  },
  {
    field: 'totalRemediatedCount',
    headerName: 'Total remediated',
    width: 130,
    type: 'number',
    renderCell: (params) => (
      <span style={{ fontSize: 12, color: 'var(--text-secondary)' }}>{params.value}</span>
    ),
  },
];

function CustomToolbar() {
  return (
    <Toolbar style={{ gap: 8, padding: '8px 12px' }}>
      <QuickFilter defaultExpanded style={{ flex: 1 }}>
        <QuickFilterControl
          placeholder="Filter scripts..."
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

export function ScriptListView() {
  const scripts = useDetectionRemediationStore((s) => s.scripts);
  const isLoadingList = useDetectionRemediationStore((s) => s.isLoadingList);
  const selectScript = useDetectionRemediationStore((s) => s.selectScript);

  const rowSelectionModel = useMemo(
    () => ({ type: 'include' as const, ids: new Set<string>() }),
    [],
  );

  return (
    <div className="panel panel-list">
      <div className="panel-header">
        <strong>Script inventory</strong>
        <span>{scripts.length} scripts</span>
      </div>
      <div style={{ width: '100%' }}>
        <DataGrid<HealthScriptListItem>
          rows={scripts}
          columns={columns}
          loading={isLoadingList}
          getRowId={(row) => row.id}
          rowSelectionModel={rowSelectionModel}
          onRowClick={(params: GridRowParams<HealthScriptListItem>) =>
            void selectScript(params.row.id)
          }
          getRowHeight={() => 'auto'}
          showToolbar
          slots={{ toolbar: CustomToolbar }}
          initialState={{
            pagination: { paginationModel: { pageSize: 25 } },
            sorting: { sortModel: [{ field: 'displayName', sort: 'asc' }] },
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
