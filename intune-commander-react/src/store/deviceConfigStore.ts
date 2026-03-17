import { create } from 'zustand';
import { sendCommand } from '../bridge/bridgeClient';
import type { DeviceConfigListItem, DeviceConfigDetail } from '../types/phase4';

interface DeviceConfigState {
  items: DeviceConfigListItem[];
  selectedId: string | null;
  detail: DeviceConfigDetail | null;
  isLoadingList: boolean;
  isLoadingDetail: boolean;
  hasAttemptedLoad: boolean;
  error: string | null;

  loadItems: () => Promise<void>;
  selectItem: (id: string) => Promise<void>;
  clearSelection: () => void;
}

export const useDeviceConfigStore = create<DeviceConfigState>((set, get) => ({
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
      const items = await sendCommand<DeviceConfigListItem[]>('deviceConfig.list');
      set({ items, isLoadingList: false });
    } catch (err) {
      set({ isLoadingList: false, error: err instanceof Error ? err.message : 'Failed to load' });
    }
  },

  selectItem: async (id) => {
    set({ selectedId: id, detail: null, isLoadingDetail: true, error: null });
    try {
      const detail = await sendCommand<DeviceConfigDetail>('deviceConfig.getDetail', { id });
      if (get().selectedId === id) set({ detail, isLoadingDetail: false });
    } catch (err) {
      if (get().selectedId === id)
        set({ isLoadingDetail: false, error: err instanceof Error ? err.message : 'Failed to load detail' });
    }
  },

  clearSelection: () => set({ selectedId: null, detail: null, isLoadingDetail: false }),
}));
