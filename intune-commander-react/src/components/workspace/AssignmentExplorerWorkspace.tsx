import { useState, useMemo, useCallback } from 'react';
import {
  DataGrid,
  type GridColDef,
  Toolbar,
  QuickFilter,
  QuickFilterControl,
  QuickFilterClear,
  ColumnsPanelTrigger,
} from '@mui/x-data-grid';
import Button from '@mui/material/Button';
import ViewColumnIcon from '@mui/icons-material/ViewColumn';
import { useAssignmentExplorerStore } from '../../store/assignmentExplorerStore';
import type { AssignmentReportRow, ReportMode, GroupSearchResult } from '../../types/assignmentExplorer';
import '../../styles/workspace.css';

const reportModes: { label: string; value: ReportMode; description: string }[] = [
  { label: 'All Policies', value: 'allPolicies', description: 'All policies with assignment summaries' },
  { label: 'Group Lookup', value: 'group', description: 'Policies assigned to a specific group' },
  { label: 'All Users', value: 'allUsers', description: 'Policies targeting "All Users"' },
  { label: 'All Devices', value: 'allDevices', description: 'Policies targeting "All Devices"' },
  { label: 'Unassigned', value: 'unassigned', description: 'Policies with no assignments' },
  { label: 'Empty Groups', value: 'emptyGroups', description: 'Policies assigned to empty groups' },
];

function GroupSearch() {
  const searchGroups = useAssignmentExplorerStore((s) => s.searchGroups);
  const groupSearchResults = useAssignmentExplorerStore((s) => s.groupSearchResults);
  const isSearching = useAssignmentExplorerStore((s) => s.isSearchingGroups);
  const selectedGroup = useAssignmentExplorerStore((s) => s.selectedGroup);
  const selectGroup = useAssignmentExplorerStore((s) => s.selectGroup);
  const [query, setQuery] = useState('');

  const handleSearch = useCallback((value: string) => {
    setQuery(value);
    void searchGroups(value);
  }, [searchGroups]);

  if (selectedGroup) {
    return (
      <div style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '8px 0' }}>
        <span style={{
          display: 'inline-flex', alignItems: 'center', gap: 6,
          padding: '4px 12px', borderRadius: 16,
          backgroundColor: 'var(--brand-soft, rgba(59,130,246,0.1))',
          border: '1px solid rgba(59,130,246,0.3)',
          fontSize: 13, color: 'var(--text-primary)',
        }}>
          {selectedGroup.displayName}
          <span style={{ fontSize: 11, color: 'var(--text-tertiary)' }}>({selectedGroup.groupType})</span>
          <button
            onClick={() => selectGroup(null)}
            style={{
              background: 'none', border: 'none', cursor: 'pointer',
              color: 'var(--text-tertiary)', fontSize: 14, padding: '0 2px',
            }}
          >
            x
          </button>
        </span>
      </div>
    );
  }

  return (
    <div style={{ position: 'relative', padding: '8px 0' }}>
      <input
        type="text"
        placeholder="Search for a group by name..."
        value={query}
        onChange={(e) => handleSearch(e.target.value)}
        style={{
          width: '100%', maxWidth: 400, padding: '8px 12px',
          borderRadius: 8, border: '1px solid var(--border)',
          backgroundColor: 'var(--surface-secondary, #1f2937)',
          color: 'var(--text-primary)', fontSize: 13, outline: 'none',
        }}
      />
      {isSearching && (
        <div style={{ fontSize: 11, color: 'var(--text-tertiary)', marginTop: 4 }}>Searching...</div>
      )}
      {groupSearchResults.length > 0 && (
        <div style={{
          position: 'absolute', top: '100%', left: 0, right: 0, maxWidth: 400,
          zIndex: 100, backgroundColor: 'var(--surface-secondary, #1f2937)',
          border: '1px solid var(--border)', borderRadius: 8,
          maxHeight: 250, overflow: 'auto', marginTop: 2,
          boxShadow: '0 4px 12px rgba(0,0,0,0.3)',
        }}>
          {groupSearchResults.map((g: GroupSearchResult) => (
            <button
              key={g.id}
              onClick={() => { selectGroup(g); setQuery(''); }}
              style={{
                display: 'flex', flexDirection: 'column', gap: 2,
                width: '100%', padding: '8px 12px', border: 'none',
                background: 'none', cursor: 'pointer', textAlign: 'left',
                borderBottom: '1px solid var(--border)',
              }}
              onMouseEnter={(e) => (e.currentTarget.style.backgroundColor = 'rgba(255,255,255,0.05)')}
              onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = 'transparent')}
            >
              <span style={{ fontSize: 13, color: 'var(--text-primary)', fontWeight: 500 }}>
                {g.displayName}
              </span>
              <span style={{ fontSize: 11, color: 'var(--text-tertiary)' }}>
                {g.groupType}{g.membershipRule ? ` — ${g.membershipRule.substring(0, 60)}...` : ''}
              </span>
            </button>
          ))}
        </div>
      )}
    </div>
  );
}

const baseColumns: GridColDef<AssignmentReportRow>[] = [
  { field: 'policyName', headerName: 'Policy Name', flex: 2, minWidth: 220 },
  { field: 'policyType', headerName: 'Policy Type', width: 180 },
  { field: 'platform', headerName: 'Platform', width: 120 },
];

const modeColumns: Record<string, GridColDef<AssignmentReportRow>[]> = {
  allPolicies: [
    ...baseColumns,
    { field: 'assignmentSummary', headerName: 'Assignment Summary', flex: 2, minWidth: 250 },
  ],
  group: [
    ...baseColumns,
    { field: 'assignmentReason', headerName: 'Reason', width: 200 },
  ],
  allUsers: [
    ...baseColumns,
    { field: 'assignmentReason', headerName: 'Reason', width: 200 },
  ],
  allDevices: [
    ...baseColumns,
    { field: 'assignmentReason', headerName: 'Reason', width: 200 },
  ],
  unassigned: baseColumns,
  emptyGroups: [
    ...baseColumns,
    { field: 'groupName', headerName: 'Empty Group', width: 200 },
  ],
};

function ReportToolbar() {
  return (
    <Toolbar style={{ gap: 8, padding: '8px 12px' }}>
      <QuickFilter defaultExpanded style={{ flex: 1 }}>
        <QuickFilterControl
          placeholder="Filter results..."
          size="small"
          style={{ minWidth: 200, maxWidth: 360 }}
        />
        <QuickFilterClear size="small" />
      </QuickFilter>
      <ColumnsPanelTrigger
        render={
          <Button
            size="small" variant="text"
            startIcon={<ViewColumnIcon sx={{ fontSize: 16 }} />}
            sx={{
              color: 'var(--text-secondary)', fontSize: 12,
              textTransform: 'none', px: 1.5,
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

export function AssignmentExplorerWorkspace() {
  const rows = useAssignmentExplorerStore((s) => s.rows);
  const isLoading = useAssignmentExplorerStore((s) => s.isLoading);
  const error = useAssignmentExplorerStore((s) => s.error);
  const reportMode = useAssignmentExplorerStore((s) => s.reportMode);
  const policyTypeFilter = useAssignmentExplorerStore((s) => s.policyTypeFilter);
  const setReportMode = useAssignmentExplorerStore((s) => s.setReportMode);
  const runReport = useAssignmentExplorerStore((s) => s.runReport);
  const setPolicyTypeFilter = useAssignmentExplorerStore((s) => s.setPolicyTypeFilter);

  const columns = useMemo(() => modeColumns[reportMode] ?? baseColumns, [reportMode]);

  const policyTypes = useMemo(() => {
    const types = new Set(rows.map((r) => r.policyType).filter(Boolean));
    return ['All', ...Array.from(types).sort()];
  }, [rows]);

  const filteredRows = useMemo(() => {
    if (!policyTypeFilter || policyTypeFilter === 'All') return rows;
    return rows.filter((r) => r.policyType === policyTypeFilter);
  }, [rows, policyTypeFilter]);

  const currentModeConfig = reportModes.find((m) => m.value === reportMode);

  return (
    <div className="workspace">
      <div className="workspace-toolbar">
        <div className="workspace-heading">
          <strong className="workspace-title">Assignment Explorer</strong>
          <div className="workspace-stats">
            <span className="inline-stat" style={{ color: 'var(--text-secondary)' }}>
              {currentModeConfig?.description ?? ''}
            </span>
          </div>
        </div>
        <div className="workspace-actions">
          <button
            className="ws-btn primary"
            onClick={() => void runReport()}
            disabled={isLoading}
          >
            {isLoading ? 'Running...' : 'Run Report'}
          </button>
        </div>
      </div>

      {/* Report mode selector */}
      <div className="platform-filter-bar">
        {reportModes.map((mode) => (
          <button
            key={mode.value}
            className={`platform-chip${reportMode === mode.value ? ' active' : ''}`}
            onClick={() => setReportMode(mode.value)}
          >
            {mode.label}
          </button>
        ))}
      </div>

      {/* Group search - only for group mode */}
      {reportMode === 'group' && <GroupSearch />}

      {error && <div className="workspace-error">{error}</div>}

      {/* Policy type filter chips (when results are available) */}
      {rows.length > 0 && policyTypes.length > 2 && (
        <div className="platform-filter-bar" style={{ paddingTop: 0 }}>
          {policyTypes.map((type) => (
            <button
              key={type}
              className={`platform-chip${(policyTypeFilter === type || (type === 'All' && !policyTypeFilter)) ? ' active' : ''}`}
              onClick={() => setPolicyTypeFilter(type === 'All' ? null : type)}
              style={{ fontSize: 11 }}
            >
              {type}
            </button>
          ))}
        </div>
      )}

      {/* Results grid */}
      <div className="panel panel-list" style={{ flex: 1 }}>
        <div className="panel-header">
          <strong>Results</strong>
          <span>{filteredRows.length} {filteredRows.length === 1 ? 'row' : 'rows'}</span>
        </div>
        <div style={{ width: '100%' }}>
          <DataGrid<AssignmentReportRow>
            rows={filteredRows}
            columns={columns}
            loading={isLoading}
            getRowId={(row) => `${row.policyId}-${row.groupId || row.policyType}`}
            showToolbar
            slots={{ toolbar: ReportToolbar }}
            initialState={{
              pagination: { paginationModel: { pageSize: 25 } },
            }}
            pageSizeOptions={[10, 25, 50, 100]}
            disableRowSelectionOnClick
            sx={{
              border: 'none',
              fontSize: 12,
              '& .MuiDataGrid-columnHeaders': {
                backgroundColor: '#111827',
                fontSize: 11, fontWeight: 600,
                letterSpacing: '0.06em', textTransform: 'uppercase',
                color: 'var(--text-muted)',
                borderBottom: '1px solid var(--border)',
              },
              '& .MuiDataGrid-columnHeader': {
                '&:focus, &:focus-within': { outline: 'none' },
              },
              '& .MuiDataGrid-cell': {
                borderBottom: '1px solid var(--border)',
                color: 'var(--text-secondary)',
                display: 'flex', alignItems: 'center',
                padding: '8px 12px',
                '&:focus, &:focus-within': { outline: 'none' },
              },
              '& .MuiDataGrid-row': {
                '&:hover': { backgroundColor: 'rgba(255,255,255,0.02)' },
              },
              '& .MuiDataGrid-footerContainer': {
                borderTop: '1px solid var(--border)',
                color: 'var(--text-tertiary)', fontSize: 12,
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

      <div className="workspace-footer">
        <span>{rows.length} total results</span>
        {isLoading && <span>Running report...</span>}
      </div>
    </div>
  );
}
