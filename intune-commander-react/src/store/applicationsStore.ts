import { create } from 'zustand';
import { sendCommand } from '../bridge/bridgeClient';
import type { AppListItem, AppDetail } from '../types/applications';

interface ApplicationsState {
  apps: AppListItem[];
  selectedAppId: string | null;
  appDetail: AppDetail | null;
  isLoadingList: boolean;
  isLoadingDetail: boolean;
  hasAttemptedLoad: boolean;
  error: string | null;
  platformFilter: string | null;

  loadApps: () => Promise<void>;
  selectApp: (id: string) => Promise<void>;
  clearSelection: () => void;
  setPlatformFilter: (platform: string | null) => void;
}

export const useApplicationsStore = create<ApplicationsState>((set, get) => ({
  apps: [],
  selectedAppId: null,
  appDetail: null,
  isLoadingList: false,
  isLoadingDetail: false,
  hasAttemptedLoad: false,
  error: null,
  platformFilter: null,

  loadApps: async () => {
    set({ isLoadingList: true, hasAttemptedLoad: true, error: null });
    try {
      const apps = await sendCommand<AppListItem[]>('apps.list');
      set({ apps, isLoadingList: false });
    } catch (err) {
      set({
        isLoadingList: false,
        error: err instanceof Error ? err.message : 'Failed to load applications',
      });
    }
  },

  selectApp: async (id: string) => {
    set({ selectedAppId: id, appDetail: null, isLoadingDetail: true, error: null });
    try {
      const detail = await sendCommand<AppDetail>('apps.getDetail', { id });
      if (get().selectedAppId === id) {
        set({ appDetail: detail, isLoadingDetail: false });
      }
    } catch (err) {
      if (get().selectedAppId === id) {
        set({
          isLoadingDetail: false,
          error: err instanceof Error ? err.message : 'Failed to load app detail',
        });
      }
    }
  },

  clearSelection: () => {
    set({ selectedAppId: null, appDetail: null, isLoadingDetail: false });
  },

  setPlatformFilter: (platform) => {
    set({ platformFilter: platform });
  },
}));
