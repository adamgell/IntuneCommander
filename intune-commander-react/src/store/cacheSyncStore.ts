import { create } from 'zustand';
import { sendCommand, onEvent } from '../bridge/bridgeClient';

interface CacheTypeStatus {
  cacheKey: string;
  label: string;
  isCached: boolean;
  cachedAt: string | null;
  itemCount: number;
}

interface SyncProgress {
  current: number;
  total: number;
  label: string;
  status: 'loading' | 'done' | 'error' | 'complete';
}

interface SyncResult {
  totalTypes: number;
  successCount: number;
  errorCount: number;
  errors: Array<{ cacheKey: string; label: string; error: string }>;
}

interface CacheSyncState {
  isSyncing: boolean;
  progress: SyncProgress | null;
  lastResult: SyncResult | null;
  cacheStatus: CacheTypeStatus[];

  syncAll: () => Promise<void>;
  loadStatus: () => Promise<void>;
  invalidateCache: () => Promise<void>;
}

export const useCacheSyncStore = create<CacheSyncState>((set) => ({
  isSyncing: false,
  progress: null,
  lastResult: null,
  cacheStatus: [],

  syncAll: async () => {
    set({ isSyncing: true, progress: null, lastResult: null });
    try {
      const result = await sendCommand<SyncResult>('cache.sync');
      set({ lastResult: result, isSyncing: false });
    } catch (err) {
      set({
        isSyncing: false,
        lastResult: {
          totalTypes: 0,
          successCount: 0,
          errorCount: 1,
          errors: [{ cacheKey: '', label: 'Sync', error: err instanceof Error ? err.message : 'Sync failed' }],
        },
      });
    }
  },

  loadStatus: async () => {
    try {
      const status = await sendCommand<CacheTypeStatus[]>('cache.status');
      set({ cacheStatus: status });
    } catch (err) {
      console.warn('[CacheSync] Failed to load status:', err instanceof Error ? err.message : err);
    }
  },

  invalidateCache: async () => {
    try {
      await sendCommand('cache.invalidate');
      set({ cacheStatus: [], lastResult: null });
    } catch (err) {
      console.warn('[CacheSync] Failed to invalidate cache:', err instanceof Error ? err.message : err);
    }
  },
}));

// Subscribe to sync progress events
onEvent('cache.syncProgress', (payload) => {
  const progress = payload as SyncProgress;
  useCacheSyncStore.setState({ progress });
});
