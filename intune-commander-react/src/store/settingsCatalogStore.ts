import { create } from 'zustand';
import { sendCommand } from '../bridge/bridgeClient';
import type { PolicyListItem, PolicyDetail } from '../types/settingsCatalog';

interface SettingsCatalogState {
  policies: PolicyListItem[];
  selectedPolicyId: string | null;
  policyDetail: PolicyDetail | null;
  isLoadingList: boolean;
  isLoadingDetail: boolean;
  hasAttemptedLoad: boolean;
  error: string | null;

  loadPolicies: () => Promise<void>;
  selectPolicy: (id: string) => Promise<void>;
  clearSelection: () => void;
}

export const useSettingsCatalogStore = create<SettingsCatalogState>((set, get) => ({
  policies: [],
  selectedPolicyId: null,
  policyDetail: null,
  isLoadingList: false,
  isLoadingDetail: false,
  hasAttemptedLoad: false,
  error: null,

  loadPolicies: async () => {
    set({ isLoadingList: true, hasAttemptedLoad: true, error: null });
    try {
      const policies = await sendCommand<PolicyListItem[]>('settingsCatalog.list');
      set({ policies, isLoadingList: false });
    } catch (err) {
      set({
        isLoadingList: false,
        error: err instanceof Error ? err.message : 'Failed to load policies',
      });
    }
  },

  selectPolicy: async (id: string) => {
    set({ selectedPolicyId: id, policyDetail: null, isLoadingDetail: true, error: null });
    try {
      const detail = await sendCommand<PolicyDetail>('settingsCatalog.getDetail', { id });
      // Only update if still selected (user may have navigated away)
      if (get().selectedPolicyId === id) {
        set({ policyDetail: detail, isLoadingDetail: false });
      }
    } catch (err) {
      if (get().selectedPolicyId === id) {
        set({
          isLoadingDetail: false,
          error: err instanceof Error ? err.message : 'Failed to load policy detail',
        });
      }
    }
  },

  clearSelection: () => {
    set({ selectedPolicyId: null, policyDetail: null, isLoadingDetail: false });
  },
}));
