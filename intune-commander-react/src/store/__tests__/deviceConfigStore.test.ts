import { vi, describe, it, expect, beforeEach } from 'vitest';

const mockSendCommand = vi.fn();
vi.mock('../../bridge/bridgeClient', () => ({
  sendCommand: mockSendCommand,
  onEvent: vi.fn(() => vi.fn()),
}));

const { useDeviceConfigStore } = await import('../deviceConfigStore');

const initialState = useDeviceConfigStore.getState();

beforeEach(() => {
  useDeviceConfigStore.setState({ ...initialState });
  vi.clearAllMocks();
});

describe('deviceConfigStore', () => {
  it('has correct initial state', () => {
    const state = useDeviceConfigStore.getState();
    expect(state.items).toEqual([]);
    expect(state.selectedId).toBeNull();
    expect(state.detail).toBeNull();
    expect(state.isLoadingList).toBe(false);
    expect(state.isLoadingDetail).toBe(false);
    expect(state.hasAttemptedLoad).toBe(false);
    expect(state.error).toBeNull();
  });

  describe('loadItems', () => {
    it('loads items successfully', async () => {
      const mockItems = [{ id: '1', displayName: 'Config A' }, { id: '2', displayName: 'Config B' }];
      mockSendCommand.mockResolvedValueOnce(mockItems);

      await useDeviceConfigStore.getState().loadItems();

      const state = useDeviceConfigStore.getState();
      expect(mockSendCommand).toHaveBeenCalledWith('deviceConfig.list');
      expect(state.items).toEqual(mockItems);
      expect(state.isLoadingList).toBe(false);
      expect(state.hasAttemptedLoad).toBe(true);
      expect(state.error).toBeNull();
    });

    it('handles load error', async () => {
      mockSendCommand.mockRejectedValueOnce(new Error('Network failure'));

      await useDeviceConfigStore.getState().loadItems();

      const state = useDeviceConfigStore.getState();
      expect(state.items).toEqual([]);
      expect(state.isLoadingList).toBe(false);
      expect(state.hasAttemptedLoad).toBe(true);
      expect(state.error).toBe('Network failure');
    });

    it('uses fallback error message for non-Error throws', async () => {
      mockSendCommand.mockRejectedValueOnce('raw string');

      await useDeviceConfigStore.getState().loadItems();

      expect(useDeviceConfigStore.getState().error).toBe('Failed to load');
    });
  });

  describe('selectItem', () => {
    it('selects an item and loads detail', async () => {
      const mockDetail = { id: '1', displayName: 'Config A', settings: {} };
      mockSendCommand.mockResolvedValueOnce(mockDetail);

      await useDeviceConfigStore.getState().selectItem('1');

      const state = useDeviceConfigStore.getState();
      expect(mockSendCommand).toHaveBeenCalledWith('deviceConfig.getDetail', { id: '1' });
      expect(state.selectedId).toBe('1');
      expect(state.detail).toEqual(mockDetail);
      expect(state.isLoadingDetail).toBe(false);
      expect(state.error).toBeNull();
    });

    it('handles select error', async () => {
      mockSendCommand.mockRejectedValueOnce(new Error('Not found'));

      await useDeviceConfigStore.getState().selectItem('1');

      const state = useDeviceConfigStore.getState();
      expect(state.selectedId).toBe('1');
      expect(state.detail).toBeNull();
      expect(state.isLoadingDetail).toBe(false);
      expect(state.error).toBe('Not found');
    });

    it('discards result if selection changed (stale guard)', async () => {
      const mockDetail = { id: '1', displayName: 'Config A' };
      mockSendCommand.mockImplementation(() => {
        useDeviceConfigStore.setState({ selectedId: '2' });
        return Promise.resolve(mockDetail);
      });

      await useDeviceConfigStore.getState().selectItem('1');

      const state = useDeviceConfigStore.getState();
      expect(state.selectedId).toBe('2');
      expect(state.detail).toBeNull();
    });

    it('discards error if selection changed (stale guard on error)', async () => {
      mockSendCommand.mockImplementation(() => {
        useDeviceConfigStore.setState({ selectedId: '2' });
        return Promise.reject(new Error('fail'));
      });

      await useDeviceConfigStore.getState().selectItem('1');

      const state = useDeviceConfigStore.getState();
      expect(state.selectedId).toBe('2');
      expect(state.error).toBeNull();
    });
  });

  describe('clearSelection', () => {
    it('resets selection state', () => {
      useDeviceConfigStore.setState({
        selectedId: '1',
        detail: { id: '1' } as any,
        isLoadingDetail: true,
      });

      useDeviceConfigStore.getState().clearSelection();

      const state = useDeviceConfigStore.getState();
      expect(state.selectedId).toBeNull();
      expect(state.detail).toBeNull();
      expect(state.isLoadingDetail).toBe(false);
    });
  });
});
