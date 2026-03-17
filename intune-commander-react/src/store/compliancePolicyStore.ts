import { create } from 'zustand';
import { sendCommand } from '../bridge/bridgeClient';
import type { CompliancePolicyListItem, CompliancePolicyDetail } from '../types/phase4';

interface CompliancePolicyState {
  items: CompliancePolicyListItem[];
  selectedId: string | null;
  detail: CompliancePolicyDetail | null;
  isLoadingList: boolean;
  isLoadingDetail: boolean;
  hasAttemptedLoad: boolean;
  error: string | null;

  loadItems: () => Promise<void>;
  selectItem: (id: string) => Promise<void>;
  clearSelection: () => void;
}

export const useCompliancePolicyStore = create<CompliancePolicyState>((set, get) => ({
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
      const items = await sendCommand<CompliancePolicyListItem[]>('compliance.list');
      set({ items, isLoadingList: false });
    } catch (err) {
      set({ isLoadingList: false, error: err instanceof Error ? err.message : 'Failed to load' });
    }
  },

  selectItem: async (id) => {
    set({ selectedId: id, detail: null, isLoadingDetail: true, error: null });
    try {
      const detail = await sendCommand<CompliancePolicyDetail>('compliance.getDetail', { id });
      if (get().selectedId === id) set({ detail, isLoadingDetail: false });
    } catch (err) {
      if (get().selectedId === id)
        set({ isLoadingDetail: false, error: err instanceof Error ? err.message : 'Failed to load detail' });
    }
  },

  clearSelection: () => set({ selectedId: null, detail: null, isLoadingDetail: false }),
}));
