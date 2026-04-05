import { create } from 'zustand';
import { sendCommand } from '../bridge/bridgeClient';
import type { TenantAdminEntityType, TenantAdminListItem } from '../types/tenantAdmin';

interface TenantAdminState {
  activeEntityType: TenantAdminEntityType;
  items: TenantAdminListItem[];
  selectedId: string | null;
  detail: Record<string, unknown> | null;
  isLoadingList: boolean;
  isLoadingDetail: boolean;
  hasAttemptedLoad: boolean;
  error: string | null;

  setEntityType: (type: TenantAdminEntityType) => void;
  loadItems: () => Promise<void>;
  selectItem: (id: string) => Promise<void>;
  clearSelection: () => void;
}

export const useTenantAdminStore = create<TenantAdminState>((set, get) => ({
  activeEntityType: 'scopeTags',
  items: [],
  selectedId: null,
  detail: null,
  isLoadingList: false,
  isLoadingDetail: false,
  hasAttemptedLoad: false,
  error: null,

  setEntityType: (type) => {
    set({
      activeEntityType: type,
      items: [],
      selectedId: null,
      detail: null,
      hasAttemptedLoad: false,
      error: null,
    });
  },

  loadItems: async () => {
    const { activeEntityType } = get();
    set({ isLoadingList: true, hasAttemptedLoad: true, error: null });
    try {
      const items = await sendCommand<TenantAdminListItem[]>(
        `tenantAdmin.${activeEntityType}.list`,
      );
      if (get().activeEntityType === activeEntityType) {
        set({ items, isLoadingList: false });
      }
    } catch (err) {
      if (get().activeEntityType === activeEntityType) {
        set({
          isLoadingList: false,
          error: err instanceof Error ? err.message : 'Failed to load items',
        });
      }
    }
  },

  selectItem: async (id: string) => {
    const { activeEntityType } = get();
    set({ selectedId: id, detail: null, isLoadingDetail: true, error: null });
    try {
      const detail = await sendCommand<Record<string, unknown>>(
        `tenantAdmin.${activeEntityType}.getDetail`,
        { id },
      );
      if (get().selectedId === id && get().activeEntityType === activeEntityType) {
        set({ detail, isLoadingDetail: false });
      }
    } catch (err) {
      if (get().selectedId === id) {
        set({
          isLoadingDetail: false,
          error: err instanceof Error ? err.message : 'Failed to load detail',
        });
      }
    }
  },

  clearSelection: () => {
    set({ selectedId: null, detail: null, isLoadingDetail: false });
  },
}));
