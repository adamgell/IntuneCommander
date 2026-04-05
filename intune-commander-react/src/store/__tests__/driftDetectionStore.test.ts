import { vi, describe, it, expect, beforeEach } from 'vitest';
import type { DriftReport } from '../../types/driftDetection';

const mockSendCommand = vi.fn();
vi.mock('../../bridge/bridgeClient', () => ({
  sendCommand: mockSendCommand,
  onEvent: vi.fn(() => vi.fn()),
}));

const { useDriftDetectionStore } = await import('../driftDetectionStore');

const initialState = useDriftDetectionStore.getState();

const mockReport: DriftReport = {
  tenant: 'test-tenant',
  scanTime: '2026-04-05T12:00:00Z',
  driftDetected: true,
  summary: { critical: 1, high: 2, medium: 3, low: 0 },
  changes: [
    { objectType: 'DeviceConfiguration', name: 'Test Policy', changeType: 'Modified', severity: 'High', fields: [] },
  ],
};

beforeEach(() => {
  useDriftDetectionStore.setState({ ...initialState });
  vi.clearAllMocks();
});

describe('driftDetectionStore', () => {
  it('has correct initial state', () => {
    const state = useDriftDetectionStore.getState();
    expect(state.baselinePath).toBeNull();
    expect(state.currentPath).toBeNull();
    expect(state.report).toBeNull();
    expect(state.isComparing).toBe(false);
    expect(state.error).toBeNull();
  });

  describe('pickBaselineFolder', () => {
    it('sets baseline path', async () => {
      mockSendCommand.mockResolvedValueOnce('C:\\export\\baseline');
      await useDriftDetectionStore.getState().pickBaselineFolder();
      expect(useDriftDetectionStore.getState().baselinePath).toBe('C:\\export\\baseline');
    });

    it('does not set path when empty', async () => {
      mockSendCommand.mockResolvedValueOnce('');
      await useDriftDetectionStore.getState().pickBaselineFolder();
      expect(useDriftDetectionStore.getState().baselinePath).toBeNull();
    });

    it('sets error on failure', async () => {
      mockSendCommand.mockRejectedValueOnce(new Error('Cancelled'));
      await useDriftDetectionStore.getState().pickBaselineFolder();
      expect(useDriftDetectionStore.getState().error).toBe('Cancelled');
    });
  });

  describe('pickCurrentFolder', () => {
    it('sets current path', async () => {
      mockSendCommand.mockResolvedValueOnce('C:\\export\\current');
      await useDriftDetectionStore.getState().pickCurrentFolder();
      expect(useDriftDetectionStore.getState().currentPath).toBe('C:\\export\\current');
    });
  });

  describe('runComparison', () => {
    it('does nothing without both paths', async () => {
      await useDriftDetectionStore.getState().runComparison();
      expect(mockSendCommand).not.toHaveBeenCalled();
    });

    it('compares successfully', async () => {
      useDriftDetectionStore.setState({ baselinePath: '/a', currentPath: '/b' });
      mockSendCommand.mockResolvedValueOnce(mockReport);

      await useDriftDetectionStore.getState().runComparison();

      const state = useDriftDetectionStore.getState();
      expect(state.report).toEqual(mockReport);
      expect(state.isComparing).toBe(false);
      expect(mockSendCommand).toHaveBeenCalledWith('drift.compare', {
        baselinePath: '/a',
        currentPath: '/b',
      });
    });

    it('sets error on failure', async () => {
      useDriftDetectionStore.setState({ baselinePath: '/a', currentPath: '/b' });
      mockSendCommand.mockRejectedValueOnce(new Error('Comparison failed'));

      await useDriftDetectionStore.getState().runComparison();

      expect(useDriftDetectionStore.getState().error).toBe('Comparison failed');
      expect(useDriftDetectionStore.getState().isComparing).toBe(false);
    });
  });

  describe('filters', () => {
    it('sets severity filter', () => {
      useDriftDetectionStore.getState().setSeverityFilter('High');
      expect(useDriftDetectionStore.getState().severityFilter).toBe('High');
    });

    it('sets object type filter', () => {
      useDriftDetectionStore.getState().setObjectTypeFilter('DeviceConfiguration');
      expect(useDriftDetectionStore.getState().objectTypeFilter).toBe('DeviceConfiguration');
    });
  });

  describe('clearReport', () => {
    it('resets report and filters', () => {
      useDriftDetectionStore.setState({
        report: mockReport,
        error: 'old error',
        severityFilter: 'High',
        objectTypeFilter: 'DeviceConfiguration',
      });
      useDriftDetectionStore.getState().clearReport();

      const state = useDriftDetectionStore.getState();
      expect(state.report).toBeNull();
      expect(state.error).toBeNull();
      expect(state.severityFilter).toBeNull();
      expect(state.objectTypeFilter).toBeNull();
    });
  });
});
