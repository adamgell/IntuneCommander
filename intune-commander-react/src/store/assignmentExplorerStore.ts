import { create } from 'zustand';
import { sendCommand } from '../bridge/bridgeClient';
import type {
  AssignmentReportRow,
  GroupSearchResult,
  ReportMode,
} from '../types/assignmentExplorer';

interface AssignmentExplorerState {
  // Report results
  rows: AssignmentReportRow[];
  isLoading: boolean;
  error: string | null;

  // Current report mode
  reportMode: ReportMode;

  // Group search state
  groupSearchResults: GroupSearchResult[];
  isSearchingGroups: boolean;
  selectedGroup: GroupSearchResult | null;

  // Filter
  policyTypeFilter: string | null;

  // Actions
  setReportMode: (mode: ReportMode) => void;
  searchGroups: (query: string) => Promise<void>;
  selectGroup: (group: GroupSearchResult | null) => void;
  runReport: () => Promise<void>;
  setPolicyTypeFilter: (filter: string | null) => void;
  clearResults: () => void;
}

export const useAssignmentExplorerStore = create<AssignmentExplorerState>((set, get) => ({
  rows: [],
  isLoading: false,
  error: null,
  reportMode: 'allPolicies',
  groupSearchResults: [],
  isSearchingGroups: false,
  selectedGroup: null,
  policyTypeFilter: null,

  setReportMode: (mode) => {
    set({ reportMode: mode, rows: [], error: null, selectedGroup: null });
  },

  searchGroups: async (query: string) => {
    if (query.length < 2) {
      set({ groupSearchResults: [] });
      return;
    }
    set({ isSearchingGroups: true });
    try {
      const results = await sendCommand<GroupSearchResult[]>('assignments.searchGroups', { query });
      set({ groupSearchResults: results, isSearchingGroups: false });
    } catch {
      set({ groupSearchResults: [], isSearchingGroups: false });
    }
  },

  selectGroup: (group) => {
    set({ selectedGroup: group, groupSearchResults: [] });
  },

  runReport: async () => {
    const { reportMode, selectedGroup } = get();

    if (reportMode === 'group' && !selectedGroup) {
      set({ error: 'Please select a group first' });
      return;
    }

    set({ isLoading: true, error: null, rows: [] });
    try {
      const payload: Record<string, string> = { mode: reportMode };
      if (reportMode === 'group' && selectedGroup) {
        payload.groupId = selectedGroup.id;
        payload.groupName = selectedGroup.displayName;
      }

      const rows = await sendCommand<AssignmentReportRow[]>('assignments.runReport', payload);
      set({ rows, isLoading: false });
    } catch (err) {
      set({
        isLoading: false,
        error: err instanceof Error ? err.message : 'Failed to run assignment report',
      });
    }
  },

  setPolicyTypeFilter: (filter) => {
    set({ policyTypeFilter: filter });
  },

  clearResults: () => {
    set({ rows: [], error: null });
  },
}));
