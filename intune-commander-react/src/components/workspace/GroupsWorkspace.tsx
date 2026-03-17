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
import { useGroupsStore } from '../../store/groupsStore';
import { GroupDetailPanel } from './GroupDetailPanel';
import type { GroupListItem } from '../../types/groups';
import '../../styles/workspace.css';

const typeChips: { label: string; value: string | null }[] = [
  { label: 'All', value: null },
  { label: 'Dynamic Device', value: 'Dynamic Device' },
  { label: 'Dynamic User', value: 'Dynamic User' },
  { label: 'Assigned', value: 'Assigned' },
];

const groupTypeColor: Record<string, string> = {
  'Dynamic Device': 'var(--success, #22c55e)',
  'Dynamic User': 'var(--brand, #3b82f6)',
  'Dynamic': 'var(--warning, #f59e0b)',
  'Assigned': 'var(--text-tertiary)',
};

const columns: GridColDef<GroupListItem>[] = [
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
    field: 'groupType',
    headerName: 'Type',
    width: 150,
    renderCell: (params) => {
      const type = params.value as string;
      const color = groupTypeColor[type] ?? 'var(--text-secondary)';
      return (
        <span style={{
          color,
          fontSize: 12,
          fontWeight: 600,
          padding: '2px 8px',
          borderRadius: 4,
          backgroundColor: `${color}15`,
        }}>
          {type}
        </span>
      );
    },
  },
  {
    field: 'membershipRule',
    headerName: 'Membership Rule',
    flex: 1,
    minWidth: 200,
    renderCell: (params) => {
      const rule = params.value as string | undefined;
      if (!rule) return <span className="muted">—</span>;
      return (
        <span style={{
          fontSize: 11,
          fontFamily: "'Cascadia Code', 'Fira Code', 'Consolas', monospace",
          color: 'var(--text-secondary)',
          overflow: 'hidden',
          textOverflow: 'ellipsis',
          whiteSpace: 'nowrap',
        }}>
          {rule}
        </span>
      );
    },
  },
  {
    field: 'mail',
    headerName: 'Mail',
    width: 200,
  },
  {
    field: 'createdDateTime',
    headerName: 'Created',
    width: 170,
    valueFormatter: (value: string) =>
      value ? new Date(value).toLocaleString() : '',
  },
];

function GroupToolbar() {
  return (
    <Toolbar style={{ gap: 8, padding: '8px 12px' }}>
      <QuickFilter defaultExpanded style={{ flex: 1 }}>
        <QuickFilterControl
          placeholder="Filter groups..."
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

export function GroupsWorkspace() {
  const groups = useGroupsStore((s) => s.groups);
  const selectedGroupId = useGroupsStore((s) => s.selectedGroupId);
  const isLoadingList = useGroupsStore((s) => s.isLoadingList);
  const hasAttemptedLoad = useGroupsStore((s) => s.hasAttemptedLoad);
  const error = useGroupsStore((s) => s.error);
  const typeFilter = useGroupsStore((s) => s.typeFilter);
  const loadGroups = useGroupsStore((s) => s.loadGroups);
  const selectGroup = useGroupsStore((s) => s.selectGroup);
  const setTypeFilter = useGroupsStore((s) => s.setTypeFilter);

  useEffect(() => {
    if (!hasAttemptedLoad && !isLoadingList) {
      void loadGroups();
    }
  }, [hasAttemptedLoad, isLoadingList, loadGroups]);

  const filteredGroups = useMemo(() => {
    if (!typeFilter) return groups;
    return groups.filter((g) => g.groupType === typeFilter);
  }, [groups, typeFilter]);

  const rowSelectionModel = useMemo(
    () => ({
      type: 'include' as const,
      ids: new Set(selectedGroupId ? [selectedGroupId] : []),
    }),
    [selectedGroupId],
  );

  // Compute type counts for chips
  const typeCounts = useMemo(() => {
    const counts: Record<string, number> = {};
    for (const g of groups) {
      counts[g.groupType] = (counts[g.groupType] || 0) + 1;
    }
    return counts;
  }, [groups]);

  return (
    <div className="workspace">
      <div className="workspace-toolbar">
        <div className="workspace-heading">
          <strong className="workspace-title">Groups</strong>
          <div className="workspace-stats">
            <span className="inline-stat">
              <strong>{filteredGroups.length}</strong> groups
              {typeFilter && ` (${typeFilter})`}
            </span>
          </div>
        </div>
      </div>

      <div className="platform-filter-bar">
        {typeChips.map((chip) => (
          <button
            key={chip.label}
            className={`platform-chip${typeFilter === chip.value ? ' active' : ''}`}
            onClick={() => setTypeFilter(chip.value)}
          >
            {chip.label}
            {chip.value && typeCounts[chip.value] !== undefined && (
              <span style={{ marginLeft: 4, opacity: 0.6, fontSize: 11 }}>
                ({typeCounts[chip.value]})
              </span>
            )}
          </button>
        ))}
      </div>

      {error && <div className="workspace-error">{error}</div>}

      <div className={`settings-columns${selectedGroupId ? ' detail-active' : ''}`}>
        <div className="panel panel-list">
          <div className="panel-header">
            <strong>Group list</strong>
            <span>{filteredGroups.length} groups</span>
          </div>
          <div style={{ width: '100%' }}>
            <DataGrid<GroupListItem>
              rows={filteredGroups}
              columns={columns}
              loading={isLoadingList}
              getRowId={(row) => row.id}
              rowSelectionModel={rowSelectionModel}
              onRowClick={(params: GridRowParams<GroupListItem>) =>
                void selectGroup(params.row.id)
              }
              getRowHeight={() => 'auto'}
              showToolbar
              slots={{ toolbar: GroupToolbar }}
              initialState={{
                pagination: { paginationModel: { pageSize: 25 } },
                columns: {
                  columnVisibilityModel: {
                    mail: false,
                    createdDateTime: false,
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
        {selectedGroupId && <GroupDetailPanel />}
      </div>

      <div className="workspace-footer">
        <span>{groups.length} total groups loaded</span>
        {isLoadingList && <span>Loading...</span>}
      </div>
    </div>
  );
}
