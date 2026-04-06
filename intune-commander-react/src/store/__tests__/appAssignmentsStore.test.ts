import { vi, describe, it, expect, beforeEach } from 'vitest';

const mockSendCommand = vi.fn();
vi.mock('../../bridge/bridgeClient', () => ({
  sendCommand: mockSendCommand,
  onEvent: vi.fn(() => vi.fn()),
}));

const { useAppAssignmentsStore } = await import('../appAssignmentsStore');

const initialState = useAppAssignmentsStore.getState();

beforeEach(() => {
  useAppAssignmentsStore.setState({ ...initialState });
  vi.clearAllMocks();
});

describe('appAssignmentsStore', () => {
  it('has correct initial state', () => {
    const state = useAppAssignmentsStore.getState();
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
      const mockItems = [{ id: '1', appName: 'App A' }, { id: '2', appName: 'App B' }];
      mockSendCommand.mockResolvedValueOnce(mockItems);

      await useAppAssignmentsStore.getState().loadItems();

      const state = useAppAssignmentsStore.getState();
      expect(mockSendCommand).toHaveBeenCalledWith('appAssignments.list');
      expect(state.items).toEqual(mockItems);
      expect(state.isLoadingList).toBe(false);
      expect(state.hasAttemptedLoad).toBe(true);
      expect(state.error).toBeNull();
    });

    it('handles load error', async () => {
      mockSendCommand.mockRejectedValueOnce(new Error('Network failure'));

      await useAppAssignmentsStore.getState().loadItems();

      const state = useAppAssignmentsStore.getState();
      expect(state.items).toEqual([]);
      expect(state.isLoadingList).toBe(false);
      expect(state.hasAttemptedLoad).toBe(true);
      expect(state.error).toBe('Network failure');
    });

    it('uses fallback error message for non-Error throws', async () => {
      mockSendCommand.mockRejectedValueOnce('raw string');

      await useAppAssignmentsStore.getState().loadItems();

      expect(useAppAssignmentsStore.getState().error).toBe('Failed to load application assignments');
    });
  });

  describe('selectItem', () => {
    it('selects an item and loads detail', async () => {
      const mockDetail = { id: '1', appName: 'App A' };
      mockSendCommand.mockResolvedValueOnce(mockDetail);

      await useAppAssignmentsStore.getState().selectItem('1');

      const state = useAppAssignmentsStore.getState();
      expect(mockSendCommand).toHaveBeenCalledWith('appAssignments.getDetail', { id: '1' });
      expect(state.selectedId).toBe('1');
      expect(state.detail).toEqual(mockDetail);
      expect(state.isLoadingDetail).toBe(false);
      expect(state.error).toBeNull();
    });

    it('handles select error', async () => {
      mockSendCommand.mockRejectedValueOnce(new Error('Not found'));

      await useAppAssignmentsStore.getState().selectItem('1');

      const state = useAppAssignmentsStore.getState();
      expect(state.selectedId).toBe('1');
      expect(state.detail).toBeNull();
      expect(state.isLoadingDetail).toBe(false);
      expect(state.error).toBe('Not found');
    });

    it('discards result if selection changed (stale guard)', async () => {
      const mockDetail = { id: '1', appName: 'App A' };
      mockSendCommand.mockImplementation(() => {
        useAppAssignmentsStore.setState({ selectedId: '2' });
        return Promise.resolve(mockDetail);
      });

      await useAppAssignmentsStore.getState().selectItem('1');

      const state = useAppAssignmentsStore.getState();
      expect(state.selectedId).toBe('2');
      expect(state.detail).toBeNull();
    });

    it('discards error if selection changed (stale guard on error)', async () => {
      mockSendCommand.mockImplementation(() => {
        useAppAssignmentsStore.setState({ selectedId: '2' });
        return Promise.reject(new Error('fail'));
      });

      await useAppAssignmentsStore.getState().selectItem('1');

      const state = useAppAssignmentsStore.getState();
      expect(state.selectedId).toBe('2');
      expect(state.error).toBeNull();
    });
  });

  describe('clearSelection', () => {
    it('resets selection state', () => {
      useAppAssignmentsStore.setState({
        selectedId: '1',
        detail: { id: '1' } as any,
        isLoadingDetail: true,
      });

      useAppAssignmentsStore.getState().clearSelection();

      const state = useAppAssignmentsStore.getState();
      expect(state.selectedId).toBeNull();
      expect(state.detail).toBeNull();
      expect(state.isLoadingDetail).toBe(false);
    });
  });
});
