import { vi, describe, it, expect, beforeEach } from 'vitest';

const mockSendCommand = vi.fn();
vi.mock('../../bridge/bridgeClient', () => ({
  sendCommand: mockSendCommand,
  onEvent: vi.fn(() => vi.fn()),
}));

const { useTargetedManagedAppConfigurationsStore } = await import('../targetedManagedAppConfigurationsStore');

const initialState = useTargetedManagedAppConfigurationsStore.getState();

beforeEach(() => {
  useTargetedManagedAppConfigurationsStore.setState({ ...initialState });
  vi.clearAllMocks();
});

describe('targetedManagedAppConfigurationsStore', () => {
  it('has correct initial state', () => {
    const state = useTargetedManagedAppConfigurationsStore.getState();
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

      await useTargetedManagedAppConfigurationsStore.getState().loadItems();

      const state = useTargetedManagedAppConfigurationsStore.getState();
      expect(mockSendCommand).toHaveBeenCalledWith('targetedManagedAppConfigurations.list');
      expect(state.items).toEqual(mockItems);
      expect(state.isLoadingList).toBe(false);
      expect(state.hasAttemptedLoad).toBe(true);
      expect(state.error).toBeNull();
    });

    it('handles load error', async () => {
      mockSendCommand.mockRejectedValueOnce(new Error('Network failure'));

      await useTargetedManagedAppConfigurationsStore.getState().loadItems();

      const state = useTargetedManagedAppConfigurationsStore.getState();
      expect(state.items).toEqual([]);
      expect(state.isLoadingList).toBe(false);
      expect(state.hasAttemptedLoad).toBe(true);
      expect(state.error).toBe('Network failure');
    });

    it('uses fallback error message for non-Error throws', async () => {
      mockSendCommand.mockRejectedValueOnce('raw string');

      await useTargetedManagedAppConfigurationsStore.getState().loadItems();

      expect(useTargetedManagedAppConfigurationsStore.getState().error).toBe('Failed to load targeted managed app configurations');
    });
  });

  describe('selectItem', () => {
    it('selects an item and loads detail', async () => {
      const mockDetail = { id: '1', displayName: 'Config A', settings: {} };
      mockSendCommand.mockResolvedValueOnce(mockDetail);

      await useTargetedManagedAppConfigurationsStore.getState().selectItem('1');

      const state = useTargetedManagedAppConfigurationsStore.getState();
      expect(mockSendCommand).toHaveBeenCalledWith('targetedManagedAppConfigurations.getDetail', { id: '1' });
      expect(state.selectedId).toBe('1');
      expect(state.detail).toEqual(mockDetail);
      expect(state.isLoadingDetail).toBe(false);
      expect(state.error).toBeNull();
    });

    it('handles select error', async () => {
      mockSendCommand.mockRejectedValueOnce(new Error('Not found'));

      await useTargetedManagedAppConfigurationsStore.getState().selectItem('1');

      const state = useTargetedManagedAppConfigurationsStore.getState();
      expect(state.selectedId).toBe('1');
      expect(state.detail).toBeNull();
      expect(state.isLoadingDetail).toBe(false);
      expect(state.error).toBe('Not found');
    });

    it('discards result if selection changed (stale guard)', async () => {
      const mockDetail = { id: '1', displayName: 'Config A' };
      mockSendCommand.mockImplementation(() => {
        useTargetedManagedAppConfigurationsStore.setState({ selectedId: '2' });
        return Promise.resolve(mockDetail);
      });

      await useTargetedManagedAppConfigurationsStore.getState().selectItem('1');

      const state = useTargetedManagedAppConfigurationsStore.getState();
      expect(state.selectedId).toBe('2');
      expect(state.detail).toBeNull();
    });

    it('discards error if selection changed (stale guard on error)', async () => {
      mockSendCommand.mockImplementation(() => {
        useTargetedManagedAppConfigurationsStore.setState({ selectedId: '2' });
        return Promise.reject(new Error('fail'));
      });

      await useTargetedManagedAppConfigurationsStore.getState().selectItem('1');

      const state = useTargetedManagedAppConfigurationsStore.getState();
      expect(state.selectedId).toBe('2');
      expect(state.error).toBeNull();
    });
  });

  describe('clearSelection', () => {
    it('resets selection state', () => {
      useTargetedManagedAppConfigurationsStore.setState({
        selectedId: '1',
        detail: { id: '1' } as any,
        isLoadingDetail: true,
      });

      useTargetedManagedAppConfigurationsStore.getState().clearSelection();

      const state = useTargetedManagedAppConfigurationsStore.getState();
      expect(state.selectedId).toBeNull();
      expect(state.detail).toBeNull();
      expect(state.isLoadingDetail).toBe(false);
    });
  });
});
