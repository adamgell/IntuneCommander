import { create } from 'zustand';
import { sendCommand } from '../bridge/bridgeClient';
import type { ScriptListItem, ScriptDetail, ScriptType } from '../types/scriptsHub';

interface ScriptsHubState {
  scripts: ScriptListItem[];
  selectedScriptId: string | null;
  selectedScriptType: ScriptType | null;
  scriptDetail: ScriptDetail | null;
  isLoadingList: boolean;
  isLoadingDetail: boolean;
  hasAttemptedLoad: boolean;
  error: string | null;
  typeFilter: ScriptType | null;

  loadScripts: () => Promise<void>;
  selectScript: (id: string, scriptType: ScriptType) => Promise<void>;
  clearSelection: () => void;
  setTypeFilter: (filter: ScriptType | null) => void;
}

export const useScriptsHubStore = create<ScriptsHubState>((set, get) => ({
  scripts: [],
  selectedScriptId: null,
  selectedScriptType: null,
  scriptDetail: null,
  isLoadingList: false,
  isLoadingDetail: false,
  hasAttemptedLoad: false,
  error: null,
  typeFilter: null,

  loadScripts: async () => {
    set({ isLoadingList: true, hasAttemptedLoad: true, error: null });
    try {
      const scripts = await sendCommand<ScriptListItem[]>('scripts.listAll');
      set({ scripts, isLoadingList: false });
    } catch (err) {
      set({
        isLoadingList: false,
        error: err instanceof Error ? err.message : 'Failed to load scripts',
      });
    }
  },

  selectScript: async (id: string, scriptType: ScriptType) => {
    set({ selectedScriptId: id, selectedScriptType: scriptType, scriptDetail: null, isLoadingDetail: true, error: null });
    try {
      const detail = await sendCommand<ScriptDetail>('scripts.getDetail', { id, scriptType });
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
    set({ selectedScriptId: null, selectedScriptType: null, scriptDetail: null, isLoadingDetail: false });
  },

  setTypeFilter: (filter) => {
    set({ typeFilter: filter });
  },
}));
