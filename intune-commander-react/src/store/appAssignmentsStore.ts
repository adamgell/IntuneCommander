import { create } from 'zustand';
import { sendCommand } from '../bridge/bridgeClient';
import type { ApplicationAssignmentRow } from '../types/applications';

interface AppAssignmentsState {
  items: ApplicationAssignmentRow[];
  selectedId: string | null;
  detail: ApplicationAssignmentRow | null;
  isLoadingList: boolean;
  isLoadingDetail: boolean;
  hasAttemptedLoad: boolean;
  error: string | null;

  loadItems: () => Promise<void>;
  selectItem: (id: string) => Promise<void>;
  clearSelection: () => void;
}

export const useAppAssignmentsStore = create<AppAssignmentsState>((set, get) => ({
  items: [],
  selectedId: null,
  detail: null,
  isLoadingList: false,
  isLoadingDetail: false,
  hasAttemptedLoad: false,
  error: null,

  loadItems: async () => {
    set({ isLoadingList: true, hasAttemptedLoad: true, error: null });
    try {
      const items = await sendCommand<ApplicationAssignmentRow[]>('appAssignments.list');
      set({ items, isLoadingList: false });
    } catch (err) {
      set({ isLoadingList: false, error: err instanceof Error ? err.message : 'Failed to load application assignments' });
    }
  },

  selectItem: async (id) => {
    set({ selectedId: id, detail: null, isLoadingDetail: true, error: null });
    try {
      const detail = await sendCommand<ApplicationAssignmentRow>('appAssignments.getDetail', { id });
      if (get().selectedId === id) {
        set({ detail, isLoadingDetail: false });
      }
    } catch (err) {
      if (get().selectedId === id) {
        set({ isLoadingDetail: false, error: err instanceof Error ? err.message : 'Failed to load assignment detail' });
      }
    }
  },

  clearSelection: () => set({ selectedId: null, detail: null, isLoadingDetail: false }),
}));
