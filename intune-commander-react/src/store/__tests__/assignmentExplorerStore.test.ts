import { vi, describe, it, expect, beforeEach } from 'vitest';

const mockSendCommand = vi.fn();
vi.mock('../../bridge/bridgeClient', () => ({
  sendCommand: mockSendCommand,
  onEvent: vi.fn(() => vi.fn()),
}));

const { useAssignmentExplorerStore } = await import('../assignmentExplorerStore');
const initialState = useAssignmentExplorerStore.getState();

beforeEach(() => {
  useAssignmentExplorerStore.setState({ ...initialState });
  vi.clearAllMocks();
});

describe('assignmentExplorerStore', () => {
  it('has correct initial state', () => {
    const state = useAssignmentExplorerStore.getState();
    expect(state.rows).toEqual([]);
    expect(state.isLoading).toBe(false);
    expect(state.error).toBeNull();
    expect(state.reportMode).toBe('allPolicies');
    expect(state.groupSearchResults).toEqual([]);
    expect(state.isSearchingGroups).toBe(false);
    expect(state.selectedGroup).toBeNull();
    expect(state.policyTypeFilter).toBeNull();
  });

  describe('searchGroups', () => {
    it('searches groups successfully', async () => {
      const mockResults = [{ id: 'g1', displayName: 'Sales Team' }];
      mockSendCommand.mockResolvedValueOnce(mockResults);
      await useAssignmentExplorerStore.getState().searchGroups('sal');
      expect(useAssignmentExplorerStore.getState().groupSearchResults).toEqual(mockResults);
      expect(mockSendCommand).toHaveBeenCalledWith('assignments.searchGroups', { query: 'sal' });
    });

    it('clears results for short query', async () => {
      useAssignmentExplorerStore.setState({ groupSearchResults: [{ id: 'g1', displayName: 'X' }] as any[] });
      await useAssignmentExplorerStore.getState().searchGroups('a');
      expect(useAssignmentExplorerStore.getState().groupSearchResults).toEqual([]);
      expect(mockSendCommand).not.toHaveBeenCalled();
    });

    it('clears results on error', async () => {
      mockSendCommand.mockRejectedValueOnce(new Error('Network error'));
      await useAssignmentExplorerStore.getState().searchGroups('test');
      expect(useAssignmentExplorerStore.getState().groupSearchResults).toEqual([]);
    });
  });

  describe('selectGroup', () => {
    it('sets selected group and clears search results', () => {
      const group = { id: 'g1', displayName: 'Sales Team' };
      useAssignmentExplorerStore.setState({ groupSearchResults: [group] as any[] });
      useAssignmentExplorerStore.getState().selectGroup(group);
      expect(useAssignmentExplorerStore.getState().selectedGroup).toEqual(group);
      expect(useAssignmentExplorerStore.getState().groupSearchResults).toEqual([]);
    });

    it('clears selection when passed null', () => {
      useAssignmentExplorerStore.setState({ selectedGroup: { id: 'g1', displayName: 'Sales' } as any });
      useAssignmentExplorerStore.getState().selectGroup(null);
      expect(useAssignmentExplorerStore.getState().selectedGroup).toBeNull();
    });
  });

  describe('setReportMode', () => {
    it('sets mode and clears report data', () => {
      useAssignmentExplorerStore.setState({ rows: [{}] as any[], error: 'old', selectedGroup: { id: 'g1', displayName: 'G' } as any });
      useAssignmentExplorerStore.getState().setReportMode('group');
      const state = useAssignmentExplorerStore.getState();
      expect(state.reportMode).toBe('group');
      expect(state.rows).toEqual([]);
      expect(state.error).toBeNull();
      expect(state.selectedGroup).toBeNull();
    });
  });

  describe('runReport', () => {
    it('runs allPolicies report successfully', async () => {
      const mockRows = [{ policyName: 'Policy A' }];
      mockSendCommand.mockResolvedValueOnce(mockRows);
      await useAssignmentExplorerStore.getState().runReport();
      expect(useAssignmentExplorerStore.getState().rows).toEqual(mockRows);
      expect(mockSendCommand).toHaveBeenCalledWith('assignments.runReport', { mode: 'allPolicies' });
    });

    it('runs group report with groupId', async () => {
      mockSendCommand.mockResolvedValueOnce([]);
      useAssignmentExplorerStore.setState({ reportMode: 'group', selectedGroup: { id: 'g1', displayName: 'Sales' } as any });
      await useAssignmentExplorerStore.getState().runReport();
      expect(mockSendCommand).toHaveBeenCalledWith('assignments.runReport', expect.objectContaining({ mode: 'group', groupId: 'g1' }));
    });

    it('sets error when group mode but no group selected', async () => {
      useAssignmentExplorerStore.setState({ reportMode: 'group', selectedGroup: null });
      await useAssignmentExplorerStore.getState().runReport();
      expect(useAssignmentExplorerStore.getState().error).toBe('Please select a group first');
      expect(mockSendCommand).not.toHaveBeenCalled();
    });

    it('sets error on failure', async () => {
      mockSendCommand.mockRejectedValueOnce(new Error('Report failed'));
      await useAssignmentExplorerStore.getState().runReport();
      expect(useAssignmentExplorerStore.getState().error).toBe('Report failed');
    });
  });

  describe('setPolicyTypeFilter', () => {
    it('sets and clears filter', () => {
      useAssignmentExplorerStore.getState().setPolicyTypeFilter('compliance');
      expect(useAssignmentExplorerStore.getState().policyTypeFilter).toBe('compliance');
      useAssignmentExplorerStore.getState().setPolicyTypeFilter(null);
      expect(useAssignmentExplorerStore.getState().policyTypeFilter).toBeNull();
    });
  });

  describe('clearResults', () => {
    it('resets rows and error', () => {
      useAssignmentExplorerStore.setState({ rows: [{}] as any[], error: 'err' });
      useAssignmentExplorerStore.getState().clearResults();
      expect(useAssignmentExplorerStore.getState().rows).toEqual([]);
      expect(useAssignmentExplorerStore.getState().error).toBeNull();
    });
  });
});
