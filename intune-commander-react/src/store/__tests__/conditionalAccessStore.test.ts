import { vi, describe, it, expect, beforeEach } from 'vitest';

const mockSendCommand = vi.fn();
vi.mock('../../bridge/bridgeClient', () => ({
  sendCommand: mockSendCommand,
  onEvent: vi.fn(() => vi.fn()),
}));

const { useConditionalAccessStore } = await import('../conditionalAccessStore');

const initialState = useConditionalAccessStore.getState();

beforeEach(() => {
  useConditionalAccessStore.setState({ ...initialState });
  vi.clearAllMocks();
});

describe('conditionalAccessStore', () => {
  it('has correct initial state', () => {
    const state = useConditionalAccessStore.getState();
    expect(state.policies).toEqual([]);
    expect(state.selectedPolicyId).toBeNull();
    expect(state.policyDetail).toBeNull();
    expect(state.isLoadingList).toBe(false);
    expect(state.isLoadingDetail).toBe(false);
    expect(state.hasAttemptedLoad).toBe(false);
    expect(state.error).toBeNull();
    expect(state.stateFilter).toBeNull();
  });

  describe('loadPolicies', () => {
    it('loads policies successfully', async () => {
      const mockPolicies = [{ id: '1', displayName: 'CA Policy A' }, { id: '2', displayName: 'CA Policy B' }];
      mockSendCommand.mockResolvedValueOnce(mockPolicies);

      await useConditionalAccessStore.getState().loadPolicies();

      const state = useConditionalAccessStore.getState();
      expect(mockSendCommand).toHaveBeenCalledWith('conditionalAccess.list');
      expect(state.policies).toEqual(mockPolicies);
      expect(state.isLoadingList).toBe(false);
      expect(state.hasAttemptedLoad).toBe(true);
      expect(state.error).toBeNull();
    });

    it('handles load error', async () => {
      mockSendCommand.mockRejectedValueOnce(new Error('Network failure'));

      await useConditionalAccessStore.getState().loadPolicies();

      const state = useConditionalAccessStore.getState();
      expect(state.policies).toEqual([]);
      expect(state.isLoadingList).toBe(false);
      expect(state.hasAttemptedLoad).toBe(true);
      expect(state.error).toBe('Network failure');
    });

    it('uses fallback error message for non-Error throws', async () => {
      mockSendCommand.mockRejectedValueOnce('raw string');

      await useConditionalAccessStore.getState().loadPolicies();

      expect(useConditionalAccessStore.getState().error).toBe('Failed to load CA policies');
    });
  });

  describe('selectPolicy', () => {
    it('selects a policy and loads detail', async () => {
      const mockDetail = { id: '1', displayName: 'CA Policy A', conditions: {} };
      mockSendCommand.mockResolvedValueOnce(mockDetail);

      await useConditionalAccessStore.getState().selectPolicy('1');

      const state = useConditionalAccessStore.getState();
      expect(mockSendCommand).toHaveBeenCalledWith('conditionalAccess.getDetail', { id: '1' });
      expect(state.selectedPolicyId).toBe('1');
      expect(state.policyDetail).toEqual(mockDetail);
      expect(state.isLoadingDetail).toBe(false);
      expect(state.error).toBeNull();
    });

    it('handles select error', async () => {
      mockSendCommand.mockRejectedValueOnce(new Error('Not found'));

      await useConditionalAccessStore.getState().selectPolicy('1');

      const state = useConditionalAccessStore.getState();
      expect(state.selectedPolicyId).toBe('1');
      expect(state.policyDetail).toBeNull();
      expect(state.isLoadingDetail).toBe(false);
      expect(state.error).toBe('Not found');
    });

    it('discards result if selection changed (stale guard)', async () => {
      const mockDetail = { id: '1', displayName: 'CA Policy A' };
      mockSendCommand.mockImplementation(() => {
        useConditionalAccessStore.setState({ selectedPolicyId: '2' });
        return Promise.resolve(mockDetail);
      });

      await useConditionalAccessStore.getState().selectPolicy('1');

      const state = useConditionalAccessStore.getState();
      expect(state.selectedPolicyId).toBe('2');
      expect(state.policyDetail).toBeNull();
    });

    it('discards error if selection changed (stale guard on error)', async () => {
      mockSendCommand.mockImplementation(() => {
        useConditionalAccessStore.setState({ selectedPolicyId: '2' });
        return Promise.reject(new Error('fail'));
      });

      await useConditionalAccessStore.getState().selectPolicy('1');

      const state = useConditionalAccessStore.getState();
      expect(state.selectedPolicyId).toBe('2');
      expect(state.error).toBeNull();
    });
  });

  describe('clearSelection', () => {
    it('resets selection state', () => {
      useConditionalAccessStore.setState({
        selectedPolicyId: '1',
        policyDetail: { id: '1' } as any,
        isLoadingDetail: true,
      });

      useConditionalAccessStore.getState().clearSelection();

      const state = useConditionalAccessStore.getState();
      expect(state.selectedPolicyId).toBeNull();
      expect(state.policyDetail).toBeNull();
      expect(state.isLoadingDetail).toBe(false);
    });
  });

  describe('setStateFilter', () => {
    it('sets the state filter', () => {
      useConditionalAccessStore.getState().setStateFilter('enabled');
      expect(useConditionalAccessStore.getState().stateFilter).toBe('enabled');
    });

    it('clears the state filter', () => {
      useConditionalAccessStore.setState({ stateFilter: 'enabled' });
      useConditionalAccessStore.getState().setStateFilter(null);
      expect(useConditionalAccessStore.getState().stateFilter).toBeNull();
    });
  });
});
