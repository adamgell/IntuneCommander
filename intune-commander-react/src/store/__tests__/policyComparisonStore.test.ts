import { vi, describe, it, expect, beforeEach } from 'vitest';
import type { PolicySummaryItem, PolicyComparisonResult } from '../../types/policyComparison';

const mockSendCommand = vi.fn();
vi.mock('../../bridge/bridgeClient', () => ({
  sendCommand: mockSendCommand,
  onEvent: vi.fn(() => vi.fn()),
}));

const { usePolicyComparisonStore } = await import('../policyComparisonStore');

const initialState = usePolicyComparisonStore.getState();

const mockPolicies: PolicySummaryItem[] = [
  { id: 'p1', displayName: 'Policy A', category: 'settingsCatalog' },
  { id: 'p2', displayName: 'Policy B', category: 'settingsCatalog' },
];

const mockResult: PolicyComparisonResult = {
  policyAName: 'Policy A',
  policyBName: 'Policy B',
  category: 'settingsCatalog',
  totalProperties: 10,
  differingProperties: 3,
  normalizedJsonA: '{}',
  normalizedJsonB: '{}',
  settingsDiff: [
    { label: 'Setting 1', category: 'General', valueA: 'true', valueB: 'false', status: 'changed' },
  ],
};

beforeEach(() => {
  usePolicyComparisonStore.setState({ ...initialState });
  vi.clearAllMocks();
});

describe('policyComparisonStore', () => {
  it('has correct initial state', () => {
    const state = usePolicyComparisonStore.getState();
    expect(state.category).toBe('settingsCatalog');
    expect(state.policies).toEqual([]);
    expect(state.policyAId).toBeNull();
    expect(state.policyBId).toBeNull();
    expect(state.comparisonResult).toBeNull();
    expect(state.isComparing).toBe(false);
  });

  describe('setCategory', () => {
    it('switches category and resets state', () => {
      usePolicyComparisonStore.setState({ policies: mockPolicies, policyAId: 'p1' });
      // Mock the loadPolicies call triggered by setCategory
      mockSendCommand.mockResolvedValueOnce([]);
      usePolicyComparisonStore.getState().setCategory('compliance');

      const state = usePolicyComparisonStore.getState();
      expect(state.category).toBe('compliance');
      expect(state.policies).toEqual([]);
      expect(state.policyAId).toBeNull();
    });
  });

  describe('loadPolicies', () => {
    it('loads policies successfully', async () => {
      mockSendCommand.mockResolvedValueOnce(mockPolicies);
      await usePolicyComparisonStore.getState().loadPolicies();

      expect(usePolicyComparisonStore.getState().policies).toEqual(mockPolicies);
      expect(usePolicyComparisonStore.getState().isLoadingPolicies).toBe(false);
    });

    it('sets error on failure', async () => {
      mockSendCommand.mockRejectedValueOnce(new Error('Load failed'));
      await usePolicyComparisonStore.getState().loadPolicies();

      expect(usePolicyComparisonStore.getState().error).toBe('Load failed');
    });
  });

  describe('compare', () => {
    it('validates both policies selected', async () => {
      await usePolicyComparisonStore.getState().compare();
      expect(usePolicyComparisonStore.getState().error).toBe('Please select two policies to compare');
      expect(mockSendCommand).not.toHaveBeenCalled();
    });

    it('validates different policies', async () => {
      usePolicyComparisonStore.setState({ policyAId: 'p1', policyBId: 'p1' });
      await usePolicyComparisonStore.getState().compare();
      expect(usePolicyComparisonStore.getState().error).toBe('Please select two different policies');
    });

    it('compares successfully', async () => {
      usePolicyComparisonStore.setState({ policyAId: 'p1', policyBId: 'p2' });
      mockSendCommand.mockResolvedValueOnce(mockResult);

      await usePolicyComparisonStore.getState().compare();

      expect(usePolicyComparisonStore.getState().comparisonResult).toEqual(mockResult);
      expect(usePolicyComparisonStore.getState().isComparing).toBe(false);
      expect(mockSendCommand).toHaveBeenCalledWith('policyComparison.compare', {
        category: 'settingsCatalog',
        idA: 'p1',
        idB: 'p2',
      });
    });

    it('sets error on failure', async () => {
      usePolicyComparisonStore.setState({ policyAId: 'p1', policyBId: 'p2' });
      mockSendCommand.mockRejectedValueOnce(new Error('Comparison failed'));

      await usePolicyComparisonStore.getState().compare();
      expect(usePolicyComparisonStore.getState().error).toBe('Comparison failed');
    });
  });

  describe('clearComparison', () => {
    it('resets selection and result', () => {
      usePolicyComparisonStore.setState({
        policyAId: 'p1',
        policyBId: 'p2',
        comparisonResult: mockResult,
        error: 'old',
      });
      usePolicyComparisonStore.getState().clearComparison();

      const state = usePolicyComparisonStore.getState();
      expect(state.policyAId).toBeNull();
      expect(state.policyBId).toBeNull();
      expect(state.comparisonResult).toBeNull();
      expect(state.error).toBeNull();
    });
  });
});
