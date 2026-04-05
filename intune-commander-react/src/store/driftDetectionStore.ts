import { create } from 'zustand';
import { sendCommand } from '../bridge/bridgeClient';
import type { DriftReport, DriftSeverity } from '../types/driftDetection';

interface DriftDetectionState {
  baselinePath: string | null;
  currentPath: string | null;
  report: DriftReport | null;
  isComparing: boolean;
  error: string | null;
  severityFilter: DriftSeverity | null;
  objectTypeFilter: string | null;

  pickBaselineFolder: () => Promise<void>;
  pickCurrentFolder: () => Promise<void>;
  runComparison: () => Promise<void>;
  setSeverityFilter: (severity: DriftSeverity | null) => void;
  setObjectTypeFilter: (type: string | null) => void;
  clearReport: () => void;
}

export const useDriftDetectionStore = create<DriftDetectionState>((set, get) => ({
  baselinePath: null,
  currentPath: null,
  report: null,
  isComparing: false,
  error: null,
  severityFilter: null,
  objectTypeFilter: null,

  pickBaselineFolder: async () => {
    try {
      const path = await sendCommand<string>('dialog.pickFolder');
      if (path) set({ baselinePath: path });
    } catch (err) {
      set({ error: err instanceof Error ? err.message : 'Failed to pick folder' });
    }
  },

  pickCurrentFolder: async () => {
    try {
      const path = await sendCommand<string>('dialog.pickFolder');
      if (path) set({ currentPath: path });
    } catch (err) {
      set({ error: err instanceof Error ? err.message : 'Failed to pick folder' });
    }
  },

  runComparison: async () => {
    const { baselinePath, currentPath } = get();
    if (!baselinePath || !currentPath) return;

    set({ isComparing: true, error: null, report: null });
    try {
      const report = await sendCommand<DriftReport>('drift.compare', {
        baselinePath,
        currentPath,
      });
      set({ report, isComparing: false });
    } catch (err) {
      set({
        isComparing: false,
        error: err instanceof Error ? err.message : 'Failed to compare',
      });
    }
  },

  setSeverityFilter: (severity) => set({ severityFilter: severity }),
  setObjectTypeFilter: (type) => set({ objectTypeFilter: type }),
  clearReport: () => set({ report: null, error: null, severityFilter: null, objectTypeFilter: null }),
}));
