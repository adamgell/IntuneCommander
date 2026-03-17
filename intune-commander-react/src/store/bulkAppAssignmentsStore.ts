import { create } from 'zustand';
import { sendCommand } from '../bridge/bridgeClient';
import type { AppListItem } from '../types/applications';
import type { GroupSearchResult } from '../types/assignmentExplorer';
import type {
  AssignmentFilterListItem,
  BulkAssignmentApplyResult,
  BulkAssignmentIntent,
  BulkAssignmentTargetDraft,
  BulkAssignmentTargetType,
  BulkAssignmentBootstrap,
} from '../types/bulkAppAssignments';

interface BulkAppAssignmentsState {
  apps: AppListItem[];
  assignmentFilters: AssignmentFilterListItem[];
  selectedAppIds: string[];
  targets: BulkAssignmentTargetDraft[];
  intent: BulkAssignmentIntent;
  searchQuery: string;
  platformFilter: string | null;
  groupSearchQuery: string;
  groupSearchResults: GroupSearchResult[];
  isLoadingBootstrap: boolean;
  isSearchingGroups: boolean;
  isApplying: boolean;
  hasAttemptedLoad: boolean;
  error: string | null;
  lastApplyResult: BulkAssignmentApplyResult | null;

  loadBootstrap: () => Promise<void>;
  setSearchQuery: (query: string) => void;
  setPlatformFilter: (platform: string | null) => void;
  setSelectedAppIds: (ids: string[]) => void;
  setIntent: (intent: BulkAssignmentIntent) => void;
  searchGroups: (query: string) => Promise<void>;
  clearGroupSearch: () => void;
  addTarget: (targetType: BulkAssignmentTargetType, target: Pick<BulkAssignmentTargetDraft, 'displayName' | 'targetId' | 'groupType'>) => void;
  updateTarget: (id: string, updates: Partial<Pick<BulkAssignmentTargetDraft, 'isExclusion' | 'filterId' | 'filterMode'>>) => void;
  removeTarget: (id: string) => void;
  applyAssignments: () => Promise<void>;
}

export const useBulkAppAssignmentsStore = create<BulkAppAssignmentsState>((set, get) => ({
  apps: [],
  assignmentFilters: [],
  selectedAppIds: [],
  targets: [],
  intent: 'required',
  searchQuery: '',
  platformFilter: null,
  groupSearchQuery: '',
  groupSearchResults: [],
  isLoadingBootstrap: false,
  isSearchingGroups: false,
  isApplying: false,
  hasAttemptedLoad: false,
  error: null,
  lastApplyResult: null,

  loadBootstrap: async () => {
    set({ isLoadingBootstrap: true, hasAttemptedLoad: true, error: null });
    try {
      const bootstrap = await sendCommand<BulkAssignmentBootstrap>('bulkAppAssignments.bootstrap');
      set({
        apps: bootstrap.apps,
        assignmentFilters: bootstrap.assignmentFilters,
        isLoadingBootstrap: false,
      });
    } catch (err) {
      set({
        isLoadingBootstrap: false,
        error: err instanceof Error ? err.message : 'Failed to load bulk assignment workspace',
      });
    }
  },

  setSearchQuery: (query) => set({ searchQuery: query }),
  setPlatformFilter: (platform) => set({ platformFilter: platform }),
  setSelectedAppIds: (ids) => set({ selectedAppIds: ids }),
  setIntent: (intent) => set({ intent }),

  searchGroups: async (query) => {
    set({ groupSearchQuery: query });
    if (query.trim().length < 2) {
      set({ groupSearchResults: [], isSearchingGroups: false });
      return;
    }

    set({ isSearchingGroups: true });
    try {
      const results = await sendCommand<GroupSearchResult[]>('assignments.searchGroups', { query });
      if (get().groupSearchQuery === query) {
        set({ groupSearchResults: results, isSearchingGroups: false });
      }
    } catch {
      if (get().groupSearchQuery === query) {
        set({ groupSearchResults: [], isSearchingGroups: false });
      }
    }
  },

  clearGroupSearch: () => set({ groupSearchQuery: '', groupSearchResults: [], isSearchingGroups: false }),

  addTarget: (targetType, target) => {
    set((state) => ({
      targets: [
        ...state.targets,
        {
          id: crypto.randomUUID(),
          targetType,
          targetId: target.targetId,
          displayName: target.displayName,
          groupType: target.groupType,
          isExclusion: false,
          filterId: null,
          filterMode: 'none',
        },
      ],
      groupSearchQuery: '',
      groupSearchResults: [],
      error: null,
      lastApplyResult: null,
    }));
  },

  updateTarget: (id, updates) => {
    set((state) => ({
      targets: state.targets.map((target) => {
        if (target.id !== id) {
          return target;
        }

        const next: BulkAssignmentTargetDraft = { ...target, ...updates };
        if (next.targetType !== 'group' && next.isExclusion) {
          next.isExclusion = false;
        }
        if (next.filterMode === 'none') {
          next.filterId = null;
        }
        return next;
      }),
      error: null,
      lastApplyResult: null,
    }));
  },

  removeTarget: (id) => {
    set((state) => ({
      targets: state.targets.filter((target) => target.id !== id),
      error: null,
      lastApplyResult: null,
    }));
  },

  applyAssignments: async () => {
    const { selectedAppIds, targets, intent, loadBootstrap } = get();

    if (selectedAppIds.length === 0) {
      set({ error: 'Select at least one app before applying assignments' });
      return;
    }

    if (targets.length === 0) {
      set({ error: 'Add at least one target before applying assignments' });
      return;
    }

    const invalidTarget = targets.find((target) =>
      (target.targetType !== 'group' && target.isExclusion)
      || (target.filterMode !== 'none' && !target.filterId));

    if (invalidTarget) {
      set({ error: `Target "${invalidTarget.displayName}" has an invalid exclusion or filter configuration` });
      return;
    }

    set({ isApplying: true, error: null, lastApplyResult: null });
    try {
      const result = await sendCommand<BulkAssignmentApplyResult>('bulkAppAssignments.apply', {
        appIds: selectedAppIds,
        intent,
        targets: targets.map((target) => ({
          targetType: target.targetType,
          targetId: target.targetId,
          displayName: target.displayName,
          isExclusion: target.isExclusion,
          filterId: target.filterMode === 'none' ? undefined : target.filterId ?? undefined,
          filterMode: target.filterMode,
        })),
      });

      set({ isApplying: false, lastApplyResult: result });
      await loadBootstrap();
    } catch (err) {
      set({
        isApplying: false,
        error: err instanceof Error ? err.message : 'Bulk assignment failed',
      });
    }
  },
}));
