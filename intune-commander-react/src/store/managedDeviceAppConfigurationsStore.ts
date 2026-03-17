import { create } from 'zustand';
import { sendCommand } from '../bridge/bridgeClient';
import type {
  ManagedDeviceAppConfigurationDetail,
  ManagedDeviceAppConfigurationListItem,
} from '../types/applications';

interface ManagedDeviceAppConfigurationsState {
  items: ManagedDeviceAppConfigurationListItem[];
  selectedId: string | null;
  detail: ManagedDeviceAppConfigurationDetail | null;
  isLoadingList: boolean;
  isLoadingDetail: boolean;
  hasAttemptedLoad: boolean;
  error: string | null;

  loadItems: () => Promise<void>;
  selectItem: (id: string) => Promise<void>;
  clearSelection: () => void;
}

export const useManagedDeviceAppConfigurationsStore = create<ManagedDeviceAppConfigurationsState>((set, get) => ({
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
      const items = await sendCommand<ManagedDeviceAppConfigurationListItem[]>('managedDeviceAppConfigurations.list');
      set({ items, isLoadingList: false });
    } catch (err) {
      set({ isLoadingList: false, error: err instanceof Error ? err.message : 'Failed to load managed device app configurations' });
    }
  },

  selectItem: async (id) => {
    set({ selectedId: id, detail: null, isLoadingDetail: true, error: null });
    try {
      const detail = await sendCommand<ManagedDeviceAppConfigurationDetail>('managedDeviceAppConfigurations.getDetail', { id });
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
