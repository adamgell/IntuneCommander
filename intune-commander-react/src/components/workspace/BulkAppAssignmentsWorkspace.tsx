import { useEffect, useMemo, useState } from 'react';
import {
  DataGrid,
  type GridColDef,
  type GridRowSelectionModel,
} from '@mui/x-data-grid';
import type { AppListItem } from '../../types/applications';
import type { GroupSearchResult } from '../../types/assignmentExplorer';
import type {
  BulkAssignmentFilterMode,
  BulkAssignmentIntent,
  BulkAssignmentTargetDraft,
} from '../../types/bulkAppAssignments';
import { useBulkAppAssignmentsStore } from '../../store/bulkAppAssignmentsStore';
import '../../styles/workspace.css';

const platformChips: { label: string; value: string | null }[] = [
  { label: 'All', value: null },
  { label: 'Windows', value: 'Windows' },
  { label: 'iOS', value: 'iOS' },
  { label: 'Android', value: 'Android' },
  { label: 'macOS', value: 'macOS' },
  { label: 'Web', value: 'Web' },
];

const intentOptions: { value: BulkAssignmentIntent; label: string; description: string }[] = [
  { value: 'required', label: 'Required', description: 'Install automatically for the selected targets.' },
  { value: 'available', label: 'Available', description: 'Make the app available in Company Portal.' },
  { value: 'availableWithoutEnrollment', label: 'Available w/o enrollment', description: 'Offer the app without requiring enrollment.' },
  { value: 'uninstall', label: 'Uninstall', description: 'Remove the app from the selected targets.' },
];

const filterModeOptions: { value: BulkAssignmentFilterMode; label: string }[] = [
  { value: 'none', label: 'None' },
  { value: 'include', label: 'Include' },
  { value: 'exclude', label: 'Exclude' },
];

const appColumns: GridColDef<AppListItem>[] = [
  {
    field: 'displayName',
    headerName: 'Name',
    flex: 1.8,
    minWidth: 220,
    renderCell: (params) => (
      <div style={{ display: 'flex', flexDirection: 'column', gap: 2, lineHeight: 1.35, padding: '6px 0', whiteSpace: 'normal' }}>
        <strong style={{ fontSize: 13, color: 'var(--text-primary)' }}>{params.row.displayName}</strong>
        {params.row.description && (
          <span
            title={params.row.description}
            style={{
              fontSize: 11,
              color: 'var(--text-tertiary)',
              overflow: 'hidden',
              textOverflow: 'ellipsis',
              whiteSpace: 'nowrap',
              maxWidth: '100%',
              display: 'block',
            }}
          >
            {params.row.description}
          </span>
        )}
      </div>
    ),
  },
  { field: 'appType', headerName: 'Type', width: 180 },
  { field: 'platform', headerName: 'Platform', width: 120 },
  { field: 'publisher', headerName: 'Publisher', width: 160 },
  {
    field: 'isAssigned',
    headerName: 'Assigned',
    width: 110,
    type: 'boolean',
  },
];

function GroupSearchResults({
  results,
  isSearching,
  onSelect,
}: {
  results: GroupSearchResult[];
  isSearching: boolean;
  onSelect: (group: GroupSearchResult) => void;
}) {
  if (isSearching) {
    return <div className="bulk-search-status">Searching groups…</div>;
  }

  if (results.length === 0) {
    return null;
  }

  return (
    <div className="bulk-group-results">
      {results.map((group) => (
        <button
          key={group.id}
          className="bulk-group-result"
          onClick={() => onSelect(group)}
        >
          <strong>{group.displayName}</strong>
          <span>
            {group.groupType}
            {group.membershipRule ? ` — ${group.membershipRule.slice(0, 80)}` : ''}
          </span>
        </button>
      ))}
    </div>
  );
}

function TargetCard({
  target,
  filters,
  onUpdate,
  onRemove,
}: {
  target: BulkAssignmentTargetDraft;
  filters: ReturnType<typeof useBulkAppAssignmentsStore.getState>['assignmentFilters'];
  onUpdate: (updates: Partial<Pick<BulkAssignmentTargetDraft, 'isExclusion' | 'filterId' | 'filterMode'>>) => void;
  onRemove: () => void;
}) {
  const supportsExclusion = target.targetType === 'group';

  return (
    <div className="bulk-target-card">
      <div className="bulk-target-card-header">
        <div>
          <strong>{target.displayName}</strong>
          <span>
            {target.targetType === 'group'
              ? `Azure AD group${target.groupType ? ` · ${target.groupType}` : ''}`
              : target.targetType === 'allUsers'
                ? 'Built-in target · All Users'
                : 'Built-in target · All Devices'}
          </span>
        </div>
        <button className="ws-btn secondary small" onClick={onRemove}>Remove</button>
      </div>
      <div className="bulk-target-fields">
        <label className="bulk-field">
          <span>Assignment type</span>
          <select
            value={target.isExclusion ? 'exclude' : 'include'}
            onChange={(event) => onUpdate({ isExclusion: event.target.value === 'exclude' })}
            disabled={!supportsExclusion}
          >
            <option value="include">Include</option>
            <option value="exclude">Exclude</option>
          </select>
        </label>
        <label className="bulk-field">
          <span>Filter mode</span>
          <select
            value={target.filterMode}
            onChange={(event) => onUpdate({ filterMode: event.target.value as BulkAssignmentFilterMode })}
          >
            {filterModeOptions.map((option) => (
              <option key={option.value} value={option.value}>{option.label}</option>
            ))}
          </select>
        </label>
        <label className="bulk-field bulk-field-wide">
          <span>Assignment filter</span>
          <select
            value={target.filterId ?? ''}
            onChange={(event) => onUpdate({ filterId: event.target.value || null })}
            disabled={target.filterMode === 'none'}
          >
            <option value="">Select a filter</option>
            {filters.map((filter) => (
              <option key={filter.id} value={filter.id}>
                {filter.displayName} · {filter.platform}
              </option>
            ))}
          </select>
        </label>
      </div>
      {!supportsExclusion && (
        <div className="bulk-target-hint">All Users and All Devices can only be included.</div>
      )}
    </div>
  );
}

export function BulkAppAssignmentsWorkspace() {
  const apps = useBulkAppAssignmentsStore((s) => s.apps);
  const assignmentFilters = useBulkAppAssignmentsStore((s) => s.assignmentFilters);
  const selectedAppIds = useBulkAppAssignmentsStore((s) => s.selectedAppIds);
  const targets = useBulkAppAssignmentsStore((s) => s.targets);
  const intent = useBulkAppAssignmentsStore((s) => s.intent);
  const searchQuery = useBulkAppAssignmentsStore((s) => s.searchQuery);
  const platformFilter = useBulkAppAssignmentsStore((s) => s.platformFilter);
  const groupSearchQuery = useBulkAppAssignmentsStore((s) => s.groupSearchQuery);
  const groupSearchResults = useBulkAppAssignmentsStore((s) => s.groupSearchResults);
  const isLoadingBootstrap = useBulkAppAssignmentsStore((s) => s.isLoadingBootstrap);
  const isSearchingGroups = useBulkAppAssignmentsStore((s) => s.isSearchingGroups);
  const isApplying = useBulkAppAssignmentsStore((s) => s.isApplying);
  const hasAttemptedLoad = useBulkAppAssignmentsStore((s) => s.hasAttemptedLoad);
  const error = useBulkAppAssignmentsStore((s) => s.error);
  const lastApplyResult = useBulkAppAssignmentsStore((s) => s.lastApplyResult);
  const loadBootstrap = useBulkAppAssignmentsStore((s) => s.loadBootstrap);
  const setSearchQuery = useBulkAppAssignmentsStore((s) => s.setSearchQuery);
  const setPlatformFilter = useBulkAppAssignmentsStore((s) => s.setPlatformFilter);
  const setSelectedAppIds = useBulkAppAssignmentsStore((s) => s.setSelectedAppIds);
  const setIntent = useBulkAppAssignmentsStore((s) => s.setIntent);
  const searchGroups = useBulkAppAssignmentsStore((s) => s.searchGroups);
  const clearGroupSearch = useBulkAppAssignmentsStore((s) => s.clearGroupSearch);
  const addTarget = useBulkAppAssignmentsStore((s) => s.addTarget);
  const updateTarget = useBulkAppAssignmentsStore((s) => s.updateTarget);
  const removeTarget = useBulkAppAssignmentsStore((s) => s.removeTarget);
  const applyAssignments = useBulkAppAssignmentsStore((s) => s.applyAssignments);

  const [groupInput, setGroupInput] = useState('');

  useEffect(() => {
    if (!hasAttemptedLoad && !isLoadingBootstrap) {
      void loadBootstrap();
    }
  }, [hasAttemptedLoad, isLoadingBootstrap, loadBootstrap]);

  const filteredApps = useMemo(() => {
    const normalizedQuery = searchQuery.trim().toLowerCase();

    return apps.filter((app) => {
      const matchesPlatform = !platformFilter || app.platform.toLowerCase().includes(platformFilter.toLowerCase());
      const matchesQuery = !normalizedQuery
        || app.displayName.toLowerCase().includes(normalizedQuery)
        || app.appType.toLowerCase().includes(normalizedQuery)
        || (app.publisher ?? '').toLowerCase().includes(normalizedQuery);

      return matchesPlatform && matchesQuery;
    });
  }, [apps, platformFilter, searchQuery]);

  const selectedApps = useMemo(
    () => apps.filter((app) => selectedAppIds.includes(app.id)),
    [apps, selectedAppIds],
  );

  const previewRows = useMemo(() => {
    return selectedApps.flatMap((app) => targets.map((target) => ({
      id: `${app.id}-${target.id}`,
      appName: app.displayName,
      targetName: target.displayName,
      assignmentType: target.isExclusion ? 'Exclusion' : 'Include',
      intent,
      filter: target.filterMode === 'none'
        ? 'None'
        : `${target.filterMode} · ${assignmentFilters.find((filter) => filter.id === target.filterId)?.displayName ?? 'Missing filter'}`,
    })));
  }, [assignmentFilters, intent, selectedApps, targets]);

  const invalidTargets = useMemo(() => {
    return targets.filter((target) =>
      (target.targetType !== 'group' && target.isExclusion)
      || (target.filterMode !== 'none' && !target.filterId));
  }, [targets]);

  const rowSelectionModel = useMemo<GridRowSelectionModel>(
    () => ({ type: 'include', ids: new Set(selectedAppIds) }),
    [selectedAppIds],
  );

  const selectedIntent = intentOptions.find((option) => option.value === intent);

  return (
    <div className="workspace">
      <div className="workspace-toolbar">
        <div className="workspace-heading">
          <strong className="workspace-title">Bulk Assignment</strong>
          <div className="workspace-stats">
            <span className="inline-stat"><strong>{selectedAppIds.length}</strong> selected apps</span>
            <span className="inline-stat"><strong>{targets.length}</strong> targets</span>
            <span className="inline-stat"><strong>{previewRows.length}</strong> preview operations</span>
          </div>
        </div>
        <div className="workspace-actions">
          <button
            className="ws-btn primary"
            onClick={() => void applyAssignments()}
            disabled={selectedAppIds.length === 0 || targets.length === 0 || invalidTargets.length > 0 || isApplying}
          >
            {isApplying ? 'Applying…' : 'Apply to selected apps'}
          </button>
        </div>
      </div>

      {error && <div className="workspace-error">{error}</div>}

      <div className="bulk-layout-grid">
        <div className="panel">
          <div className="panel-header">
            <strong>1. Select apps</strong>
            <span>{filteredApps.length} shown</span>
          </div>
          <div className="panel-body bulk-panel-body">
            <div className="bulk-filter-row">
              <label className="bulk-field bulk-field-grow">
                <span>Search</span>
                <input
                  value={searchQuery}
                  onChange={(event) => setSearchQuery(event.target.value)}
                  placeholder="Filter by name, publisher, or app type"
                />
              </label>
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
            <div className="bulk-grid-shell">
              <DataGrid<AppListItem>
                rows={filteredApps}
                columns={appColumns}
                loading={isLoadingBootstrap}
                checkboxSelection
                disableRowSelectionOnClick
                getRowId={(row) => row.id}
                rowSelectionModel={rowSelectionModel}
                onRowSelectionModelChange={(nextModel) => {
                  setSelectedAppIds(Array.from(nextModel.ids as Set<string>));
                }}
                getRowHeight={() => 'auto'}
                initialState={{ pagination: { paginationModel: { pageSize: 10 } } }}
                pageSizeOptions={[10, 25, 50]}
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
                  '& .MuiDataGrid-cell': {
                    borderBottom: '1px solid var(--border)',
                    color: 'var(--text-secondary)',
                    alignItems: 'center',
                    '&:focus, &:focus-within': { outline: 'none' },
                  },
                  '& .MuiDataGrid-footerContainer': {
                    borderTop: '1px solid var(--border)',
                    color: 'var(--text-tertiary)',
                  },
                  '& .MuiDataGrid-overlay': { backgroundColor: 'transparent' },
                }}
              />
            </div>
          </div>
        </div>

        <div className="bulk-right-column">
          <div className="panel">
            <div className="panel-header">
              <strong>2. Configure assignment</strong>
              <span>{selectedIntent?.description}</span>
            </div>
            <div className="panel-body bulk-panel-body">
              <div className="bulk-filter-row">
                <label className="bulk-field bulk-field-grow">
                  <span>Intent</span>
                  <select
                    value={intent}
                    onChange={(event) => setIntent(event.target.value as BulkAssignmentIntent)}
                  >
                    {intentOptions.map((option) => (
                      <option key={option.value} value={option.value}>{option.label}</option>
                    ))}
                  </select>
                </label>
              </div>

              <div className="bulk-add-target-row">
                <button className="ws-btn secondary" onClick={() => addTarget('allUsers', { displayName: 'All Users' })}>
                  Add All Users
                </button>
                <button className="ws-btn secondary" onClick={() => addTarget('allDevices', { displayName: 'All Devices' })}>
                  Add All Devices
                </button>
              </div>

              <div className="bulk-filter-row">
                <label className="bulk-field bulk-field-grow">
                  <span>Search Azure AD groups</span>
                  <input
                    value={groupInput}
                    onChange={(event) => {
                      const value = event.target.value;
                      setGroupInput(value);
                      void searchGroups(value);
                    }}
                    placeholder="Type at least two characters"
                  />
                </label>
                <div className="bulk-search-actions">
                  <button className="ws-btn secondary small" onClick={() => { setGroupInput(''); clearGroupSearch(); }}>
                    Clear
                  </button>
                </div>
              </div>
              <GroupSearchResults
                results={groupSearchResults}
                isSearching={isSearchingGroups}
                onSelect={(group) => {
                  addTarget('group', {
                    displayName: group.displayName,
                    targetId: group.id,
                    groupType: group.groupType,
                  });
                  setGroupInput('');
                  clearGroupSearch();
                }}
              />
              {groupSearchQuery.length > 0 && groupSearchResults.length === 0 && !isSearchingGroups && (
                <div className="bulk-search-status">No groups matched “{groupSearchQuery}”.</div>
              )}

              <div className="bulk-target-list">
                {targets.length === 0 ? (
                  <div className="bulk-empty-state">Add one or more built-in targets or Azure AD groups to continue.</div>
                ) : (
                  targets.map((target) => (
                    <TargetCard
                      key={target.id}
                      target={target}
                      filters={assignmentFilters}
                      onUpdate={(updates) => updateTarget(target.id, updates)}
                      onRemove={() => removeTarget(target.id)}
                    />
                  ))
                )}
              </div>
            </div>
          </div>

          <div className="panel">
            <div className="panel-header">
              <strong>3. Preview</strong>
              <span>{previewRows.length} operations</span>
            </div>
            <div className="panel-body bulk-panel-body">
              {invalidTargets.length > 0 && (
                <div className="workspace-error">
                  Fix invalid targets before applying. Filters in include/exclude mode require a selected filter, and only groups can be exclusions.
                </div>
              )}
              {previewRows.length === 0 ? (
                <div className="bulk-empty-state">Select apps and targets to preview the assignment operations.</div>
              ) : (
                <div className="bulk-preview-table-shell">
                  <table className="policy-table">
                    <thead>
                      <tr>
                        <th>App</th>
                        <th>Target</th>
                        <th>Type</th>
                        <th>Intent</th>
                        <th>Filter</th>
                      </tr>
                    </thead>
                    <tbody>
                      {previewRows.map((row) => (
                        <tr key={row.id}>
                          <td>{row.appName}</td>
                          <td>{row.targetName}</td>
                          <td>{row.assignmentType}</td>
                          <td>{row.intent}</td>
                          <td>{row.filter}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          </div>

          {lastApplyResult && (
            <div className="panel">
              <div className="panel-header">
                <strong>Last apply result</strong>
                <span>
                  {lastApplyResult.succeededAppCount} succeeded · {lastApplyResult.failedAppCount} failed
                </span>
              </div>
              <div className="panel-body bulk-panel-body">
                <div className="bulk-result-list">
                  {lastApplyResult.results.map((result) => (
                    <div key={`${result.appId}-${result.success}`} className={`bulk-result-item${result.success ? ' success' : ' error'}`}>
                      <strong>{result.appName}</strong>
                      <span>
                        {result.success
                          ? `Applied successfully · ${result.finalAssignmentCount} total assignments sent`
                          : result.error ?? 'Unknown failure'}
                      </span>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
