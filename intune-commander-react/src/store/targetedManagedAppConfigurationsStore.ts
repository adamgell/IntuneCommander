import { create } from 'zustand';
import { sendCommand } from '../bridge/bridgeClient';
import type {
  TargetedManagedAppConfigurationDetail,
  TargetedManagedAppConfigurationListItem,
} from '../types/applications';

interface TargetedManagedAppConfigurationsState {
  items: TargetedManagedAppConfigurationListItem[];
  selectedId: string | null;
  detail: TargetedManagedAppConfigurationDetail | null;
  isLoadingList: boolean;
  isLoadingDetail: boolean;
  hasAttemptedLoad: boolean;
  error: string | null;

  loadItems: () => Promise<void>;
  selectItem: (id: string) => Promise<void>;
  clearSelection: () => void;
}

export const useTargetedManagedAppConfigurationsStore = create<TargetedManagedAppConfigurationsState>((set, get) => ({
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
      const items = await sendCommand<TargetedManagedAppConfigurationListItem[]>('targetedManagedAppConfigurations.list');
      set({ items, isLoadingList: false });
    } catch (err) {
      set({ isLoadingList: false, error: err instanceof Error ? err.message : 'Failed to load targeted managed app configurations' });
    }
  },

  selectItem: async (id) => {
    set({ selectedId: id, detail: null, isLoadingDetail: true, error: null });
    try {
      const detail = await sendCommand<TargetedManagedAppConfigurationDetail>('targetedManagedAppConfigurations.getDetail', { id });
      if (get().selectedId === id) {
        set({ detail, isLoadingDetail: false });
      }
    } catch (err) {
      if (get().selectedId === id) {
        set({ isLoadingDetail: false, error: err instanceof Error ? err.message : 'Failed to load configuration detail' });
      }
    }
  },

  clearSelection: () => set({ selectedId: null, detail: null, isLoadingDetail: false }),
}));
