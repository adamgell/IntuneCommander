import { vi, describe, it, expect, beforeEach } from 'vitest';

const mockSendCommand = vi.fn();
vi.mock('../../bridge/bridgeClient', () => ({
  sendCommand: mockSendCommand,
  onEvent: vi.fn(() => vi.fn()),
}));

const { useAppProtectionPoliciesStore } = await import('../appProtectionPoliciesStore');

const initialState = useAppProtectionPoliciesStore.getState();

beforeEach(() => {
  useAppProtectionPoliciesStore.setState({ ...initialState });
  vi.clearAllMocks();
});

describe('appProtectionPoliciesStore', () => {
  it('has correct initial state', () => {
    const state = useAppProtectionPoliciesStore.getState();
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
      const mockItems = [{ id: '1', displayName: 'Policy A' }, { id: '2', displayName: 'Policy B' }];
      mockSendCommand.mockResolvedValueOnce(mockItems);

      await useAppProtectionPoliciesStore.getState().loadItems();

      const state = useAppProtectionPoliciesStore.getState();
      expect(mockSendCommand).toHaveBeenCalledWith('appProtectionPolicies.list');
      expect(state.items).toEqual(mockItems);
      expect(state.isLoadingList).toBe(false);
      expect(state.hasAttemptedLoad).toBe(true);
      expect(state.error).toBeNull();
    });

    it('handles load error', async () => {
      mockSendCommand.mockRejectedValueOnce(new Error('Network failure'));

      await useAppProtectionPoliciesStore.getState().loadItems();

      const state = useAppProtectionPoliciesStore.getState();
      expect(state.items).toEqual([]);
      expect(state.isLoadingList).toBe(false);
      expect(state.hasAttemptedLoad).toBe(true);
      expect(state.error).toBe('Network failure');
    });

    it('uses fallback error message for non-Error throws', async () => {
      mockSendCommand.mockRejectedValueOnce('raw string');

      await useAppProtectionPoliciesStore.getState().loadItems();

      expect(useAppProtectionPoliciesStore.getState().error).toBe('Failed to load app protection policies');
    });
  });

  describe('selectItem', () => {
    it('selects an item and loads detail', async () => {
      const mockDetail = { id: '1', displayName: 'Policy A', settings: {} };
      mockSendCommand.mockResolvedValueOnce(mockDetail);

      await useAppProtectionPoliciesStore.getState().selectItem('1');

      const state = useAppProtectionPoliciesStore.getState();
      expect(mockSendCommand).toHaveBeenCalledWith('appProtectionPolicies.getDetail', { id: '1' });
      expect(state.selectedId).toBe('1');
      expect(state.detail).toEqual(mockDetail);
      expect(state.isLoadingDetail).toBe(false);
      expect(state.error).toBeNull();
    });

    it('handles select error', async () => {
      mockSendCommand.mockRejectedValueOnce(new Error('Not found'));

      await useAppProtectionPoliciesStore.getState().selectItem('1');

      const state = useAppProtectionPoliciesStore.getState();
      expect(state.selectedId).toBe('1');
      expect(state.detail).toBeNull();
      expect(state.isLoadingDetail).toBe(false);
      expect(state.error).toBe('Not found');
    });

    it('discards result if selection changed (stale guard)', async () => {
      const mockDetail = { id: '1', displayName: 'Policy A' };
      mockSendCommand.mockImplementation(() => {
        useAppProtectionPoliciesStore.setState({ selectedId: '2' });
        return Promise.resolve(mockDetail);
      });

      await useAppProtectionPoliciesStore.getState().selectItem('1');

      const state = useAppProtectionPoliciesStore.getState();
      expect(state.selectedId).toBe('2');
      expect(state.detail).toBeNull();
    });

    it('discards error if selection changed (stale guard on error)', async () => {
      mockSendCommand.mockImplementation(() => {
        useAppProtectionPoliciesStore.setState({ selectedId: '2' });
        return Promise.reject(new Error('fail'));
      });

      await useAppProtectionPoliciesStore.getState().selectItem('1');

      const state = useAppProtectionPoliciesStore.getState();
      expect(state.selectedId).toBe('2');
      expect(state.error).toBeNull();
    });
  });

  describe('clearSelection', () => {
    it('resets selection state', () => {
      useAppProtectionPoliciesStore.setState({
        selectedId: '1',
        detail: { id: '1' } as any,
        isLoadingDetail: true,
      });

      useAppProtectionPoliciesStore.getState().clearSelection();

      const state = useAppProtectionPoliciesStore.getState();
      expect(state.selectedId).toBeNull();
      expect(state.detail).toBeNull();
      expect(state.isLoadingDetail).toBe(false);
    });
  });
});
