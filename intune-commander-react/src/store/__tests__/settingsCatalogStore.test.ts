import { vi, describe, it, expect, beforeEach } from 'vitest';

const mockSendCommand = vi.fn();
vi.mock('../../bridge/bridgeClient', () => ({
  sendCommand: mockSendCommand,
  onEvent: vi.fn(() => vi.fn()),
}));

const { useSettingsCatalogStore } = await import('../settingsCatalogStore');

const initialState = useSettingsCatalogStore.getState();

beforeEach(() => {
  useSettingsCatalogStore.setState({ ...initialState });
  vi.clearAllMocks();
});

describe('settingsCatalogStore', () => {
  it('has correct initial state', () => {
    const state = useSettingsCatalogStore.getState();
    expect(state.policies).toEqual([]);
    expect(state.selectedPolicyId).toBeNull();
    expect(state.policyDetail).toBeNull();
    expect(state.isLoadingList).toBe(false);
    expect(state.isLoadingDetail).toBe(false);
    expect(state.hasAttemptedLoad).toBe(false);
    expect(state.error).toBeNull();
  });

  describe('loadPolicies', () => {
    it('loads policies successfully', async () => {
      const mockPolicies = [{ id: '1', name: 'Policy A' }, { id: '2', name: 'Policy B' }];
      mockSendCommand.mockResolvedValueOnce(mockPolicies);

      await useSettingsCatalogStore.getState().loadPolicies();

      const state = useSettingsCatalogStore.getState();
      expect(mockSendCommand).toHaveBeenCalledWith('settingsCatalog.list');
      expect(state.policies).toEqual(mockPolicies);
      expect(state.isLoadingList).toBe(false);
      expect(state.hasAttemptedLoad).toBe(true);
      expect(state.error).toBeNull();
    });

    it('handles load error', async () => {
      mockSendCommand.mockRejectedValueOnce(new Error('Network failure'));

      await useSettingsCatalogStore.getState().loadPolicies();

      const state = useSettingsCatalogStore.getState();
      expect(state.policies).toEqual([]);
      expect(state.isLoadingList).toBe(false);
      expect(state.hasAttemptedLoad).toBe(true);
      expect(state.error).toBe('Network failure');
    });

    it('uses fallback error message for non-Error throws', async () => {
      mockSendCommand.mockRejectedValueOnce('raw string');

      await useSettingsCatalogStore.getState().loadPolicies();

      expect(useSettingsCatalogStore.getState().error).toBe('Failed to load policies');
    });
  });

  describe('selectPolicy', () => {
    it('selects a policy and loads detail', async () => {
      const mockDetail = { id: '1', name: 'Policy A', settings: [] };
      mockSendCommand.mockResolvedValueOnce(mockDetail);

      await useSettingsCatalogStore.getState().selectPolicy('1');

      const state = useSettingsCatalogStore.getState();
      expect(mockSendCommand).toHaveBeenCalledWith('settingsCatalog.getDetail', { id: '1' });
      expect(state.selectedPolicyId).toBe('1');
      expect(state.policyDetail).toEqual(mockDetail);
      expect(state.isLoadingDetail).toBe(false);
      expect(state.error).toBeNull();
    });

    it('handles select error', async () => {
      mockSendCommand.mockRejectedValueOnce(new Error('Not found'));

      await useSettingsCatalogStore.getState().selectPolicy('1');

      const state = useSettingsCatalogStore.getState();
      expect(state.selectedPolicyId).toBe('1');
      expect(state.policyDetail).toBeNull();
      expect(state.isLoadingDetail).toBe(false);
      expect(state.error).toBe('Not found');
    });

    it('discards result if selection changed (stale guard)', async () => {
      const mockDetail = { id: '1', name: 'Policy A' };
      mockSendCommand.mockImplementation(() => {
        useSettingsCatalogStore.setState({ selectedPolicyId: '2' });
        return Promise.resolve(mockDetail);
      });

      await useSettingsCatalogStore.getState().selectPolicy('1');

      const state = useSettingsCatalogStore.getState();
      expect(state.selectedPolicyId).toBe('2');
      expect(state.policyDetail).toBeNull();
    });

    it('discards error if selection changed (stale guard on error)', async () => {
      mockSendCommand.mockImplementation(() => {
        useSettingsCatalogStore.setState({ selectedPolicyId: '2' });
        return Promise.reject(new Error('fail'));
      });

      await useSettingsCatalogStore.getState().selectPolicy('1');

      const state = useSettingsCatalogStore.getState();
      expect(state.selectedPolicyId).toBe('2');
      expect(state.error).toBeNull();
    });
  });

  describe('clearSelection', () => {
    it('resets selection state', () => {
      useSettingsCatalogStore.setState({
        selectedPolicyId: '1',
        policyDetail: { id: '1' } as any,
        isLoadingDetail: true,
      });

      useSettingsCatalogStore.getState().clearSelection();

      const state = useSettingsCatalogStore.getState();
      expect(state.selectedPolicyId).toBeNull();
      expect(state.policyDetail).toBeNull();
      expect(state.isLoadingDetail).toBe(false);
    });
  });
});
