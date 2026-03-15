import { create } from 'zustand';
import { sendCommand } from '../bridge/bridgeClient';
import type { CaPolicyListItem, CaPolicyDetail } from '../types/conditionalAccess';

interface ConditionalAccessState {
  policies: CaPolicyListItem[];
  selectedPolicyId: string | null;
  policyDetail: CaPolicyDetail | null;
  isLoadingList: boolean;
  isLoadingDetail: boolean;
  hasAttemptedLoad: boolean;
  error: string | null;
  stateFilter: string | null;

  loadPolicies: () => Promise<void>;
  selectPolicy: (id: string) => Promise<void>;
  clearSelection: () => void;
  setStateFilter: (state: string | null) => void;
}

export const useConditionalAccessStore = create<ConditionalAccessState>((set, get) => ({
  policies: [],
  selectedPolicyId: null,
  policyDetail: null,
  isLoadingList: false,
  isLoadingDetail: false,
  hasAttemptedLoad: false,
  error: null,
  stateFilter: null,

  loadPolicies: async () => {
    set({ isLoadingList: true, hasAttemptedLoad: true, error: null });
    try {
      const policies = await sendCommand<CaPolicyListItem[]>('conditionalAccess.list');
      set({ policies, isLoadingList: false });
    } catch (err) {
      set({
        isLoadingList: false,
        error: err instanceof Error ? err.message : 'Failed to load CA policies',
      });
    }
  },

  selectPolicy: async (id: string) => {
    set({ selectedPolicyId: id, policyDetail: null, isLoadingDetail: true, error: null });
    try {
      const detail = await sendCommand<CaPolicyDetail>('conditionalAccess.getDetail', { id });
      if (get().selectedPolicyId === id) {
        set({ policyDetail: detail, isLoadingDetail: false });
      }
    } catch (err) {
      if (get().selectedPolicyId === id) {
        set({
          isLoadingDetail: false,
          error: err instanceof Error ? err.message : 'Failed to load CA policy detail',
        });
      }
    }
  },

  clearSelection: () => {
    set({ selectedPolicyId: null, policyDetail: null, isLoadingDetail: false });
  },

  setStateFilter: (state) => {
    set({ stateFilter: state });
  },
}));
