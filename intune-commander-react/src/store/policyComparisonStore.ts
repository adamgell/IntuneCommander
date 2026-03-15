import { create } from 'zustand';
import { sendCommand } from '../bridge/bridgeClient';
import type { PolicyCategory, PolicySummaryItem, PolicyComparisonResult } from '../types/policyComparison';

interface PolicyComparisonState {
  category: PolicyCategory;
  policies: PolicySummaryItem[];
  isLoadingPolicies: boolean;
  policyAId: string | null;
  policyBId: string | null;
  comparisonResult: PolicyComparisonResult | null;
  isComparing: boolean;
  error: string | null;

  setCategory: (category: PolicyCategory) => void;
  loadPolicies: () => Promise<void>;
  setPolicyA: (id: string | null) => void;
  setPolicyB: (id: string | null) => void;
  compare: () => Promise<void>;
  clearComparison: () => void;
}

export const usePolicyComparisonStore = create<PolicyComparisonState>((set, get) => ({
  category: 'settingsCatalog',
  policies: [],
  isLoadingPolicies: false,
  policyAId: null,
  policyBId: null,
  comparisonResult: null,
  isComparing: false,
  error: null,

  setCategory: (category) => {
    set({ category, policies: [], policyAId: null, policyBId: null, comparisonResult: null, error: null });
    void get().loadPolicies();
  },

  loadPolicies: async () => {
    const { category } = get();
    set({ isLoadingPolicies: true, error: null });
    try {
      const policies = await sendCommand<PolicySummaryItem[]>('policyComparison.list', { category });
      set({ policies, isLoadingPolicies: false });
    } catch (err) {
      set({
        isLoadingPolicies: false,
        error: err instanceof Error ? err.message : 'Failed to load policies',
      });
    }
  },

  setPolicyA: (id) => {
    set({ policyAId: id, comparisonResult: null });
  },

  setPolicyB: (id) => {
    set({ policyBId: id, comparisonResult: null });
  },

  compare: async () => {
    const { category, policyAId, policyBId } = get();
    if (!policyAId || !policyBId) {
      set({ error: 'Please select two policies to compare' });
      return;
    }
    if (policyAId === policyBId) {
      set({ error: 'Please select two different policies' });
      return;
    }

    set({ isComparing: true, error: null, comparisonResult: null });
    try {
      const result = await sendCommand<PolicyComparisonResult>('policyComparison.compare', {
        category, idA: policyAId, idB: policyBId,
      });
      set({ comparisonResult: result, isComparing: false });
    } catch (err) {
      set({
        isComparing: false,
        error: err instanceof Error ? err.message : 'Failed to compare policies',
      });
    }
  },

  clearComparison: () => {
    set({ policyAId: null, policyBId: null, comparisonResult: null, error: null });
  },
}));
