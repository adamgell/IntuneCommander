import { create } from 'zustand';
import { sendCommand } from '../bridge/bridgeClient';
import type { SecurityPostureSummary, SecurityPostureDetail } from '../types/securityPosture';

interface SecurityPostureState {
  summary: SecurityPostureSummary | null;
  detail: SecurityPostureDetail | null;
  isLoadingSummary: boolean;
  isLoadingDetail: boolean;
  hasAttemptedLoad: boolean;
  error: string | null;

  loadSummary: () => Promise<void>;
  loadDetail: () => Promise<void>;
}

export const useSecurityPostureStore = create<SecurityPostureState>((set) => ({
  summary: null,
  detail: null,
  isLoadingSummary: false,
  isLoadingDetail: false,
  hasAttemptedLoad: false,
  error: null,

  loadSummary: async () => {
    set({ isLoadingSummary: true, hasAttemptedLoad: true, error: null });
    try {
      const summary = await sendCommand<SecurityPostureSummary>('securityPosture.summary');
      set({ summary, isLoadingSummary: false });
    } catch (err) {
      set({
        isLoadingSummary: false,
        error: err instanceof Error ? err.message : 'Failed to load security posture',
      });
    }
  },

  loadDetail: async () => {
    set({ isLoadingDetail: true, error: null });
    try {
      const detail = await sendCommand<SecurityPostureDetail>('securityPosture.detail');
      set({ detail, isLoadingDetail: false });
    } catch (err) {
      set({
        isLoadingDetail: false,
        error: err instanceof Error ? err.message : 'Failed to load security posture detail',
      });
    }
  },
}));
