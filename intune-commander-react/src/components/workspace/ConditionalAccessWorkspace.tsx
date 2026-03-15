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
import { useConditionalAccessStore } from '../../store/conditionalAccessStore';
import { CaPolicyDetailPanel } from './CaPolicyDetailPanel';
import type { CaPolicyListItem } from '../../types/conditionalAccess';
import '../../styles/workspace.css';

const stateChips: { label: string; value: string | null }[] = [
  { label: 'All', value: null },
  { label: 'Enabled', value: 'enabled' },
  { label: 'Report-only', value: 'enabledForReportingButNotEnforced' },
  { label: 'Disabled', value: 'disabled' },
];

function StateChip({ state }: { state: string }) {
  let color = 'var(--text-tertiary)';
  let label = state;
  if (state === 'enabled') {
    color = 'var(--success, #22c55e)';
    label = 'Enabled';
  } else if (state === 'enabledForReportingButNotEnforced') {
    color = 'var(--warning, #f59e0b)';
    label = 'Report-only';
  } else if (state === 'disabled') {
    color = 'var(--text-muted, #6b7280)';
    label = 'Disabled';
  }
  return (
    <span
      style={{
        display: 'inline-block',
        padding: '2px 10px',
        borderRadius: 12,
        fontSize: 11,
        fontWeight: 600,
        backgroundColor: `${color}20`,
        color,
        border: `1px solid ${color}40`,
      }}
    >
      {label}
    </span>
  );
}

const columns: GridColDef<CaPolicyListItem>[] = [
  {
    field: 'displayName',
    headerName: 'Policy Name',
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
    field: 'state',
    headerName: 'State',
    width: 140,
    renderCell: (params) => <StateChip state={params.value as string} />,
  },
  {
    field: 'conditions.users',
    headerName: 'Users',
    width: 160,
    valueGetter: (_value: unknown, row: CaPolicyListItem) => row.conditions.users,
  },
  {
    field: 'conditions.applications',
    headerName: 'Apps',
    width: 160,
    valueGetter: (_value: unknown, row: CaPolicyListItem) => row.conditions.applications,
  },
  {
    field: 'grantControls',
    headerName: 'Grant Controls',
    flex: 1,
    minWidth: 180,
    valueFormatter: (value: string[]) =>
      Array.isArray(value) ? value.join(', ') : '',
  },
  {
    field: 'conditions.platforms',
    headerName: 'Platforms',
    width: 140,
    valueGetter: (_value: unknown, row: CaPolicyListItem) => row.conditions.platforms,
  },
  {
    field: 'createdDateTime',
    headerName: 'Created',
    width: 170,
    valueFormatter: (value: string) =>
      value ? new Date(value).toLocaleString() : '',
  },
  {
    field: 'modifiedDateTime',
    headerName: 'Modified',
    width: 170,
    valueFormatter: (value: string) =>
      value ? new Date(value).toLocaleString() : '',
  },
];

function CaToolbar() {
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

export function ConditionalAccessWorkspace() {
  const policies = useConditionalAccessStore((s) => s.policies);
  const selectedPolicyId = useConditionalAccessStore((s) => s.selectedPolicyId);
  const isLoadingList = useConditionalAccessStore((s) => s.isLoadingList);
  const hasAttemptedLoad = useConditionalAccessStore((s) => s.hasAttemptedLoad);
  const error = useConditionalAccessStore((s) => s.error);
  const stateFilter = useConditionalAccessStore((s) => s.stateFilter);
  const loadPolicies = useConditionalAccessStore((s) => s.loadPolicies);
  const selectPolicy = useConditionalAccessStore((s) => s.selectPolicy);
  const setStateFilter = useConditionalAccessStore((s) => s.setStateFilter);

  useEffect(() => {
    if (!hasAttemptedLoad && !isLoadingList) {
      void loadPolicies();
    }
  }, [hasAttemptedLoad, isLoadingList, loadPolicies]);

  const filteredPolicies = useMemo(() => {
    if (!stateFilter) return policies;
    return policies.filter((p) => p.state === stateFilter);
  }, [policies, stateFilter]);

  const enabledCount = policies.filter((p) => p.state === 'enabled').length;
  const reportOnlyCount = policies.filter((p) => p.state === 'enabledForReportingButNotEnforced').length;
  const disabledCount = policies.filter((p) => p.state === 'disabled').length;

  const rowSelectionModel = useMemo(
    () => ({
      type: 'include' as const,
      ids: new Set(selectedPolicyId ? [selectedPolicyId] : []),
    }),
    [selectedPolicyId],
  );

  return (
    <div className="workspace">
      <div className="workspace-toolbar">
        <div className="workspace-heading">
          <strong className="workspace-title">Conditional Access</strong>
          <div className="workspace-stats">
            <span className="inline-stat" style={{ color: 'var(--success, #22c55e)' }}>
              <strong>{enabledCount}</strong> enabled
            </span>
            <span className="inline-stat" style={{ color: 'var(--warning, #f59e0b)' }}>
              <strong>{reportOnlyCount}</strong> report-only
            </span>
            <span className="inline-stat" style={{ color: 'var(--text-muted)' }}>
              <strong>{disabledCount}</strong> disabled
            </span>
          </div>
        </div>
        <div className="workspace-actions">
          <button className="ws-btn primary" disabled>Export selected</button>
        </div>
      </div>

      <div className="platform-filter-bar">
        {stateChips.map((chip) => (
          <button
            key={chip.label}
            className={`platform-chip${stateFilter === chip.value ? ' active' : ''}`}
            onClick={() => setStateFilter(chip.value)}
          >
            {chip.label}
          </button>
        ))}
      </div>

      {error && <div className="workspace-error">{error}</div>}

      <div className={`settings-columns${selectedPolicyId ? ' detail-active' : ''}`}>
        <div className="panel panel-list">
          <div className="panel-header">
            <strong>Policy list</strong>
            <span>{filteredPolicies.length} policies</span>
          </div>
          <div style={{ width: '100%' }}>
            <DataGrid<CaPolicyListItem>
              rows={filteredPolicies}
              columns={columns}
              loading={isLoadingList}
              getRowId={(row) => row.id}
              rowSelectionModel={rowSelectionModel}
              onRowClick={(params: GridRowParams<CaPolicyListItem>) =>
                void selectPolicy(params.row.id)
              }
              getRowHeight={() => 'auto'}
              showToolbar
              slots={{ toolbar: CaToolbar }}
              initialState={{
                pagination: { paginationModel: { pageSize: 25 } },
                columns: {
                  columnVisibilityModel: {
                    createdDateTime: false,
                    'conditions.platforms': false,
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
        {selectedPolicyId && <CaPolicyDetailPanel />}
      </div>

      <div className="workspace-footer">
        <span>{policies.length} total policies loaded</span>
        {isLoadingList && <span>Loading...</span>}
      </div>
    </div>
  );
}
