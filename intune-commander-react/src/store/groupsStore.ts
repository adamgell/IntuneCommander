import { create } from 'zustand';
import { sendCommand } from '../bridge/bridgeClient';
import type { GroupListItem, GroupDetail } from '../types/groups';

interface GroupsState {
  groups: GroupListItem[];
  selectedGroupId: string | null;
  groupDetail: GroupDetail | null;
  isLoadingList: boolean;
  isLoadingDetail: boolean;
  hasAttemptedLoad: boolean;
  error: string | null;
  typeFilter: string | null;
  searchQuery: string;

  loadGroups: () => Promise<void>;
  selectGroup: (id: string) => Promise<void>;
  clearSelection: () => void;
  setTypeFilter: (type: string | null) => void;
  searchGroups: (query: string) => Promise<void>;
  setSearchQuery: (query: string) => void;
}

export const useGroupsStore = create<GroupsState>((set, get) => ({
  groups: [],
  selectedGroupId: null,
  groupDetail: null,
  isLoadingList: false,
  isLoadingDetail: false,
  hasAttemptedLoad: false,
  error: null,
  typeFilter: null,
  searchQuery: '',

  loadGroups: async () => {
    set({ isLoadingList: true, hasAttemptedLoad: true, error: null });
    try {
      const groups = await sendCommand<GroupListItem[]>('groups.list');
      set({ groups, isLoadingList: false });
    } catch (err) {
      set({
        isLoadingList: false,
        error: err instanceof Error ? err.message : 'Failed to load groups',
      });
    }
  },

  selectGroup: async (id: string) => {
    set({ selectedGroupId: id, groupDetail: null, isLoadingDetail: true, error: null });
    try {
      const detail = await sendCommand<GroupDetail>('groups.getDetail', { id });
      if (get().selectedGroupId === id) {
        set({ groupDetail: detail, isLoadingDetail: false });
      }
    } catch (err) {
      if (get().selectedGroupId === id) {
        set({
          isLoadingDetail: false,
          error: err instanceof Error ? err.message : 'Failed to load group detail',
        });
      }
    }
  },

  clearSelection: () => {
    set({ selectedGroupId: null, groupDetail: null, isLoadingDetail: false });
  },

  setTypeFilter: (type) => {
    set({ typeFilter: type });
  },

  searchGroups: async (query: string) => {
    set({ isLoadingList: true, error: null, searchQuery: query });
    try {
      const groups = await sendCommand<GroupListItem[]>('groups.search', { query });
      if (get().searchQuery === query) {
        set({ groups, isLoadingList: false });
      }
    } catch (err) {
      if (get().searchQuery === query) {
        set({
          isLoadingList: false,
          error: err instanceof Error ? err.message : 'Failed to search groups',
        });
      }
    }
  },

  setSearchQuery: (query) => {
    set({ searchQuery: query });
  },
}));
