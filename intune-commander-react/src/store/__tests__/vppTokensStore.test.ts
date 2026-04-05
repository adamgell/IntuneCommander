import { vi, describe, it, expect, beforeEach } from 'vitest';

const mockSendCommand = vi.fn();
vi.mock('../../bridge/bridgeClient', () => ({
  sendCommand: mockSendCommand,
  onEvent: vi.fn(() => vi.fn()),
}));

const { useVppTokensStore } = await import('../vppTokensStore');

const initialState = useVppTokensStore.getState();

beforeEach(() => {
  useVppTokensStore.setState({ ...initialState });
  vi.clearAllMocks();
});

describe('vppTokensStore', () => {
  it('has correct initial state', () => {
    const state = useVppTokensStore.getState();
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
      const mockItems = [{ id: '1', organizationName: 'Org A' }, { id: '2', organizationName: 'Org B' }];
      mockSendCommand.mockResolvedValueOnce(mockItems);

      await useVppTokensStore.getState().loadItems();

      const state = useVppTokensStore.getState();
      expect(mockSendCommand).toHaveBeenCalledWith('vppTokens.list');
      expect(state.items).toEqual(mockItems);
      expect(state.isLoadingList).toBe(false);
      expect(state.hasAttemptedLoad).toBe(true);
      expect(state.error).toBeNull();
    });

    it('handles load error', async () => {
      mockSendCommand.mockRejectedValueOnce(new Error('Network failure'));

      await useVppTokensStore.getState().loadItems();

      const state = useVppTokensStore.getState();
      expect(state.items).toEqual([]);
      expect(state.isLoadingList).toBe(false);
      expect(state.hasAttemptedLoad).toBe(true);
      expect(state.error).toBe('Network failure');
    });

    it('uses fallback error message for non-Error throws', async () => {
      mockSendCommand.mockRejectedValueOnce('raw string');

      await useVppTokensStore.getState().loadItems();

      expect(useVppTokensStore.getState().error).toBe('Failed to load VPP tokens');
    });
  });

  describe('selectItem', () => {
    it('selects an item and loads detail', async () => {
      const mockDetail = { id: '1', organizationName: 'Org A', state: 'valid' };
      mockSendCommand.mockResolvedValueOnce(mockDetail);

      await useVppTokensStore.getState().selectItem('1');

      const state = useVppTokensStore.getState();
      expect(mockSendCommand).toHaveBeenCalledWith('vppTokens.getDetail', { id: '1' });
      expect(state.selectedId).toBe('1');
      expect(state.detail).toEqual(mockDetail);
      expect(state.isLoadingDetail).toBe(false);
      expect(state.error).toBeNull();
    });

    it('handles select error', async () => {
      mockSendCommand.mockRejectedValueOnce(new Error('Not found'));

      await useVppTokensStore.getState().selectItem('1');

      const state = useVppTokensStore.getState();
      expect(state.selectedId).toBe('1');
      expect(state.detail).toBeNull();
      expect(state.isLoadingDetail).toBe(false);
      expect(state.error).toBe('Not found');
    });

    it('discards result if selection changed (stale guard)', async () => {
      const mockDetail = { id: '1', organizationName: 'Org A' };
      mockSendCommand.mockImplementation(() => {
        useVppTokensStore.setState({ selectedId: '2' });
        return Promise.resolve(mockDetail);
      });

      await useVppTokensStore.getState().selectItem('1');

      const state = useVppTokensStore.getState();
      expect(state.selectedId).toBe('2');
      expect(state.detail).toBeNull();
    });

    it('discards error if selection changed (stale guard on error)', async () => {
      mockSendCommand.mockImplementation(() => {
        useVppTokensStore.setState({ selectedId: '2' });
        return Promise.reject(new Error('fail'));
      });

      await useVppTokensStore.getState().selectItem('1');

      const state = useVppTokensStore.getState();
      expect(state.selectedId).toBe('2');
      expect(state.error).toBeNull();
    });
  });

  describe('clearSelection', () => {
    it('resets selection state', () => {
      useVppTokensStore.setState({
        selectedId: '1',
        detail: { id: '1' } as any,
        isLoadingDetail: true,
      });

      useVppTokensStore.getState().clearSelection();

      const state = useVppTokensStore.getState();
      expect(state.selectedId).toBeNull();
      expect(state.detail).toBeNull();
      expect(state.isLoadingDetail).toBe(false);
    });
  });
});
