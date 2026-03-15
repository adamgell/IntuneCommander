import { create } from 'zustand';
import { sendCommand } from '../bridge/bridgeClient';
import type {
  HealthScriptListItem,
  HealthScriptDetail,
  DeviceSearchResult,
  DeploymentRecord,
  DeviceRunState,
} from '../types/detectionRemediation';

interface ScriptUpdatePayload {
  id: string;
  displayName?: string;
  description?: string;
  detectionScript?: string;
  remediationScript?: string;
}

interface DetectionRemediationState {
  scripts: HealthScriptListItem[];
  selectedScriptId: string | null;
  scriptDetail: HealthScriptDetail | null;
  isLoadingList: boolean;
  isLoadingDetail: boolean;
  hasAttemptedLoad: boolean;
  isSaving: boolean;
  error: string | null;

  // Deploy & monitor
  deviceSearchResults: DeviceSearchResult[];
  isSearchingDevices: boolean;
  selectedDeviceIds: Set<string>;
  deploymentRecords: DeploymentRecord[];
  isDeploying: boolean;
  monitoringStates: DeviceRunState[];
  isMonitoring: boolean;
  monitorIntervalId: number | null;
  lastMonitorRefresh: string | null;

  loadScripts: () => Promise<void>;
  selectScript: (id: string) => Promise<void>;
  clearSelection: () => void;
  saveScript: (payload: ScriptUpdatePayload) => Promise<boolean>;

  // Deploy & monitor actions
  searchDevices: (query: string) => Promise<void>;
  toggleDeviceSelection: (id: string) => void;
  selectAllDevices: () => void;
  clearDeviceSelection: () => void;
  deployToDevices: (scriptId: string, devices: { id: string; deviceName: string }[]) => Promise<void>;
  startMonitoring: (scriptId: string) => void;
  stopMonitoring: () => void;
  refreshMonitoring: (scriptId: string) => Promise<void>;
  resetDeployState: () => void;
}

export const useDetectionRemediationStore = create<DetectionRemediationState>((set, get) => ({
  scripts: [],
  selectedScriptId: null,
  scriptDetail: null,
  isLoadingList: false,
  isLoadingDetail: false,
  hasAttemptedLoad: false,
  isSaving: false,
  error: null,

  // Deploy & monitor
  deviceSearchResults: [],
  isSearchingDevices: false,
  selectedDeviceIds: new Set<string>(),
  deploymentRecords: [],
  isDeploying: false,
  monitoringStates: [],
  isMonitoring: false,
  monitorIntervalId: null,
  lastMonitorRefresh: null,

  loadScripts: async () => {
    set({ isLoadingList: true, hasAttemptedLoad: true, error: null });
    try {
      const scripts = await sendCommand<HealthScriptListItem[]>('healthScripts.list');
      set({ scripts, isLoadingList: false });
    } catch (err) {
      set({
        isLoadingList: false,
        error: err instanceof Error ? err.message : 'Failed to load scripts',
      });
    }
  },

  selectScript: async (id: string) => {
    set({ selectedScriptId: id, isLoadingDetail: true, error: null });
    try {
      const detail = await sendCommand<HealthScriptDetail>('healthScripts.getDetail', { id });
      if (get().selectedScriptId === id) {
        set({ scriptDetail: detail, isLoadingDetail: false });
      }
    } catch (err) {
      if (get().selectedScriptId === id) {
        set({
          isLoadingDetail: false,
          error: err instanceof Error ? err.message : 'Failed to load script detail',
        });
      }
    }
  },

  clearSelection: () => {
    const { monitorIntervalId } = get();
    if (monitorIntervalId) clearInterval(monitorIntervalId);
    set({
      selectedScriptId: null,
      scriptDetail: null,
      isLoadingDetail: false,
      deviceSearchResults: [],
      selectedDeviceIds: new Set(),
      deploymentRecords: [],
      monitoringStates: [],
      isMonitoring: false,
      monitorIntervalId: null,
      lastMonitorRefresh: null,
    });
  },

  saveScript: async (payload: ScriptUpdatePayload) => {
    set({ isSaving: true, error: null });
    try {
      await sendCommand('healthScripts.update', payload);
      const detail = await sendCommand<HealthScriptDetail>('healthScripts.getDetail', { id: payload.id });
      set({ scriptDetail: detail, isSaving: false });
      void get().loadScripts();
      return true;
    } catch (err) {
      set({
        isSaving: false,
        error: err instanceof Error ? err.message : 'Failed to save script',
      });
      return false;
    }
  },

  searchDevices: async (query: string) => {
    set({ isSearchingDevices: true, error: null });
    try {
      const results = await sendCommand<DeviceSearchResult[]>('devices.search', { query });
      set({ deviceSearchResults: results, isSearchingDevices: false });
    } catch (err) {
      set({
        isSearchingDevices: false,
        error: err instanceof Error ? err.message : 'Failed to search devices',
      });
    }
  },

  toggleDeviceSelection: (id: string) => {
    const current = new Set(get().selectedDeviceIds);
    if (current.has(id)) current.delete(id);
    else current.add(id);
    set({ selectedDeviceIds: current });
  },

  selectAllDevices: () => {
    const ids = new Set(get().deviceSearchResults.map((d) => d.id));
    set({ selectedDeviceIds: ids });
  },

  clearDeviceSelection: () => {
    set({ selectedDeviceIds: new Set() });
  },

  deployToDevices: async (scriptId, devices) => {
    set({ isDeploying: true, error: null });
    try {
      const records = await sendCommand<DeploymentRecord[]>('healthScripts.deploy', {
        scriptId,
        devices,
      });
      set({ deploymentRecords: records, isDeploying: false });
      // Auto-start monitoring after deploy
      get().startMonitoring(scriptId);
    } catch (err) {
      set({
        isDeploying: false,
        error: err instanceof Error ? err.message : 'Failed to deploy script',
      });
    }
  },

  startMonitoring: (scriptId: string) => {
    const existing = get().monitorIntervalId;
    if (existing) clearInterval(existing);

    // Immediately refresh once
    get().refreshMonitoring(scriptId);

    const intervalId = window.setInterval(() => {
      if (!get().isMonitoring) {
        clearInterval(intervalId);
        return;
      }
      get().refreshMonitoring(scriptId);
    }, 10_000);

    set({ isMonitoring: true, monitorIntervalId: intervalId });

    // Auto-stop after 5 minutes
    const autoStopTimer = window.setTimeout(() => {
      // Only stop if this is still the active monitoring interval
      if (get().monitorIntervalId === intervalId && get().isMonitoring) {
        get().stopMonitoring();
      }
    }, 5 * 60_000);

    // Store the auto-stop timer so stopMonitoring can clear it
    (window as unknown as Record<string, number>).__monitorAutoStop = autoStopTimer;
  },

  stopMonitoring: () => {
    const { monitorIntervalId } = get();
    if (monitorIntervalId) clearInterval(monitorIntervalId);
    // Clear the auto-stop timeout to prevent it firing after manual stop
    const autoStop = (window as unknown as Record<string, number>).__monitorAutoStop;
    if (autoStop) {
      clearTimeout(autoStop);
      delete (window as unknown as Record<string, number>).__monitorAutoStop;
    }
    set({ isMonitoring: false, monitorIntervalId: null });
  },

  refreshMonitoring: async (scriptId: string) => {
    try {
      const states = await sendCommand<DeviceRunState[]>('healthScripts.refreshRunStates', {
        id: scriptId,
      });
      set({
        monitoringStates: states,
        lastMonitorRefresh: new Date().toISOString(),
      });
    } catch (err) {
      console.warn('[Monitor] Refresh failed:', err instanceof Error ? err.message : err);
    }
  },

  resetDeployState: () => {
    const { monitorIntervalId } = get();
    if (monitorIntervalId) clearInterval(monitorIntervalId);
    set({
      deviceSearchResults: [],
      selectedDeviceIds: new Set(),
      deploymentRecords: [],
      monitoringStates: [],
      isDeploying: false,
      isMonitoring: false,
      monitorIntervalId: null,
      lastMonitorRefresh: null,
    });
  },
}));
