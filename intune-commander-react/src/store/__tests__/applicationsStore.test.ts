import { vi, describe, it, expect, beforeEach } from 'vitest';

const mockSendCommand = vi.fn();
vi.mock('../../bridge/bridgeClient', () => ({
  sendCommand: mockSendCommand,
  onEvent: vi.fn(() => vi.fn()),
}));

const { useApplicationsStore } = await import('../applicationsStore');

const initialState = useApplicationsStore.getState();

beforeEach(() => {
  useApplicationsStore.setState({ ...initialState });
  vi.clearAllMocks();
});

describe('applicationsStore', () => {
  it('has correct initial state', () => {
    const state = useApplicationsStore.getState();
    expect(state.apps).toEqual([]);
    expect(state.selectedAppId).toBeNull();
    expect(state.appDetail).toBeNull();
    expect(state.isLoadingList).toBe(false);
    expect(state.isLoadingDetail).toBe(false);
    expect(state.hasAttemptedLoad).toBe(false);
    expect(state.error).toBeNull();
    expect(state.platformFilter).toBeNull();
  });

  describe('loadApps', () => {
    it('loads apps successfully', async () => {
      const mockApps = [{ id: '1', displayName: 'App A' }, { id: '2', displayName: 'App B' }];
      mockSendCommand.mockResolvedValueOnce(mockApps);

      await useApplicationsStore.getState().loadApps();

      const state = useApplicationsStore.getState();
      expect(mockSendCommand).toHaveBeenCalledWith('apps.list');
      expect(state.apps).toEqual(mockApps);
      expect(state.isLoadingList).toBe(false);
      expect(state.hasAttemptedLoad).toBe(true);
      expect(state.error).toBeNull();
    });

    it('handles load error', async () => {
      mockSendCommand.mockRejectedValueOnce(new Error('Network failure'));

      await useApplicationsStore.getState().loadApps();

      const state = useApplicationsStore.getState();
      expect(state.apps).toEqual([]);
      expect(state.isLoadingList).toBe(false);
      expect(state.hasAttemptedLoad).toBe(true);
      expect(state.error).toBe('Network failure');
    });

    it('uses fallback error message for non-Error throws', async () => {
      mockSendCommand.mockRejectedValueOnce('raw string');

      await useApplicationsStore.getState().loadApps();

      expect(useApplicationsStore.getState().error).toBe('Failed to load applications');
    });
  });

  describe('selectApp', () => {
    it('selects an app and loads detail', async () => {
      const mockDetail = { id: '1', displayName: 'App A', settings: {} };
      mockSendCommand.mockResolvedValueOnce(mockDetail);

      await useApplicationsStore.getState().selectApp('1');

      const state = useApplicationsStore.getState();
      expect(mockSendCommand).toHaveBeenCalledWith('apps.getDetail', { id: '1' });
      expect(state.selectedAppId).toBe('1');
      expect(state.appDetail).toEqual(mockDetail);
      expect(state.isLoadingDetail).toBe(false);
      expect(state.error).toBeNull();
    });

    it('handles select error', async () => {
      mockSendCommand.mockRejectedValueOnce(new Error('Not found'));

      await useApplicationsStore.getState().selectApp('1');

      const state = useApplicationsStore.getState();
      expect(state.selectedAppId).toBe('1');
      expect(state.appDetail).toBeNull();
      expect(state.isLoadingDetail).toBe(false);
      expect(state.error).toBe('Not found');
    });

    it('discards result if selection changed (stale guard)', async () => {
      const mockDetail = { id: '1', displayName: 'App A' };
      mockSendCommand.mockImplementation(() => {
        // Simulate user selecting a different item while loading
        useApplicationsStore.setState({ selectedAppId: '2' });
        return Promise.resolve(mockDetail);
      });

      await useApplicationsStore.getState().selectApp('1');

      const state = useApplicationsStore.getState();
      expect(state.selectedAppId).toBe('2');
      expect(state.appDetail).toBeNull();
    });

    it('discards error if selection changed (stale guard on error)', async () => {
      mockSendCommand.mockImplementation(() => {
        useApplicationsStore.setState({ selectedAppId: '2' });
        return Promise.reject(new Error('fail'));
      });

      await useApplicationsStore.getState().selectApp('1');

      const state = useApplicationsStore.getState();
      expect(state.selectedAppId).toBe('2');
      expect(state.error).toBeNull();
    });
  });

  describe('clearSelection', () => {
    it('resets selection state', () => {
      useApplicationsStore.setState({
        selectedAppId: '1',
        appDetail: { id: '1' } as any,
        isLoadingDetail: true,
      });

      useApplicationsStore.getState().clearSelection();

      const state = useApplicationsStore.getState();
      expect(state.selectedAppId).toBeNull();
      expect(state.appDetail).toBeNull();
      expect(state.isLoadingDetail).toBe(false);
    });
  });

  describe('setPlatformFilter', () => {
    it('sets the platform filter', () => {
      useApplicationsStore.getState().setPlatformFilter('iOS');
      expect(useApplicationsStore.getState().platformFilter).toBe('iOS');
    });

    it('clears the platform filter', () => {
      useApplicationsStore.setState({ platformFilter: 'iOS' });
      useApplicationsStore.getState().setPlatformFilter(null);
      expect(useApplicationsStore.getState().platformFilter).toBeNull();
    });
  });
});
