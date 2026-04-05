import { vi, describe, it, expect, beforeEach } from 'vitest';

const mockSendCommand = vi.fn();
vi.mock('../../bridge/bridgeClient', () => ({
  sendCommand: mockSendCommand,
  onEvent: vi.fn(() => vi.fn()),
}));

const { useSecurityPostureStore } = await import('../securityPostureStore');
const initialState = useSecurityPostureStore.getState();

beforeEach(() => {
  useSecurityPostureStore.setState({ ...initialState });
  vi.clearAllMocks();
});

describe('securityPostureStore', () => {
  it('has correct initial state', () => {
    const state = useSecurityPostureStore.getState();
    expect(state.summary).toBeNull();
    expect(state.detail).toBeNull();
    expect(state.isLoadingSummary).toBe(false);
    expect(state.isLoadingDetail).toBe(false);
    expect(state.hasAttemptedLoad).toBe(false);
    expect(state.error).toBeNull();
  });

  describe('loadSummary', () => {
    it('loads summary successfully', async () => {
      const mockSummary = { score: 85 };
      mockSendCommand.mockResolvedValueOnce(mockSummary);
      await useSecurityPostureStore.getState().loadSummary();
      expect(useSecurityPostureStore.getState().summary).toEqual(mockSummary);
      expect(useSecurityPostureStore.getState().isLoadingSummary).toBe(false);
      expect(useSecurityPostureStore.getState().hasAttemptedLoad).toBe(true);
      expect(mockSendCommand).toHaveBeenCalledWith('securityPosture.summary');
    });

    it('sets error on failure', async () => {
      mockSendCommand.mockRejectedValueOnce(new Error('Forbidden'));
      await useSecurityPostureStore.getState().loadSummary();
      expect(useSecurityPostureStore.getState().error).toBe('Forbidden');
      expect(useSecurityPostureStore.getState().isLoadingSummary).toBe(false);
    });
  });

  describe('loadDetail', () => {
    it('loads detail successfully', async () => {
      const mockDetail = { categories: [] };
      mockSendCommand.mockResolvedValueOnce(mockDetail);
      await useSecurityPostureStore.getState().loadDetail();
      expect(useSecurityPostureStore.getState().detail).toEqual(mockDetail);
      expect(useSecurityPostureStore.getState().isLoadingDetail).toBe(false);
      expect(mockSendCommand).toHaveBeenCalledWith('securityPosture.detail');
    });

    it('sets error on failure', async () => {
      mockSendCommand.mockRejectedValueOnce(new Error('Server error'));
      await useSecurityPostureStore.getState().loadDetail();
      expect(useSecurityPostureStore.getState().error).toBe('Server error');
    });
  });
});
