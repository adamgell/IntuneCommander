import { vi, describe, it, expect, beforeEach } from 'vitest';

const mockSendCommand = vi.fn();
vi.mock('../../bridge/bridgeClient', () => ({
  sendCommand: mockSendCommand,
  onEvent: vi.fn(() => vi.fn()),
}));

// Mock crypto.randomUUID for addTarget
vi.stubGlobal('crypto', { randomUUID: () => 'mock-uuid' });

const { useBulkAppAssignmentsStore } = await import('../bulkAppAssignmentsStore');
const initialState = useBulkAppAssignmentsStore.getState();

beforeEach(() => {
  useBulkAppAssignmentsStore.setState({ ...initialState, targets: [], selectedAppIds: [] });
  vi.clearAllMocks();
});

describe('bulkAppAssignmentsStore', () => {
  it('has correct initial state', () => {
    const state = useBulkAppAssignmentsStore.getState();
    expect(state.apps).toEqual([]);
    expect(state.selectedAppIds).toEqual([]);
    expect(state.targets).toEqual([]);
    expect(state.intent).toBe('required');
    expect(state.isApplying).toBe(false);
    expect(state.error).toBeNull();
    expect(state.lastApplyResult).toBeNull();
  });

  describe('loadBootstrap', () => {
    it('loads apps and filters', async () => {
      const bootstrap = { apps: [{ id: 'a1' }], assignmentFilters: [{ id: 'f1' }] };
      mockSendCommand.mockResolvedValueOnce(bootstrap);
      await useBulkAppAssignmentsStore.getState().loadBootstrap();
      expect(useBulkAppAssignmentsStore.getState().apps).toEqual([{ id: 'a1' }]);
      expect(useBulkAppAssignmentsStore.getState().assignmentFilters).toEqual([{ id: 'f1' }]);
      expect(mockSendCommand).toHaveBeenCalledWith('bulkAppAssignments.bootstrap');
    });

    it('sets error on failure', async () => {
      mockSendCommand.mockRejectedValueOnce(new Error('Failed'));
      await useBulkAppAssignmentsStore.getState().loadBootstrap();
      expect(useBulkAppAssignmentsStore.getState().error).toBe('Failed');
    });
  });

  describe('setters', () => {
    it('setSearchQuery', () => {
      useBulkAppAssignmentsStore.getState().setSearchQuery('test');
      expect(useBulkAppAssignmentsStore.getState().searchQuery).toBe('test');
    });

    it('setPlatformFilter', () => {
      useBulkAppAssignmentsStore.getState().setPlatformFilter('windows');
      expect(useBulkAppAssignmentsStore.getState().platformFilter).toBe('windows');
    });

    it('setSelectedAppIds', () => {
      useBulkAppAssignmentsStore.getState().setSelectedAppIds(['a1', 'a2']);
      expect(useBulkAppAssignmentsStore.getState().selectedAppIds).toEqual(['a1', 'a2']);
    });

    it('setIntent', () => {
      useBulkAppAssignmentsStore.getState().setIntent('available');
      expect(useBulkAppAssignmentsStore.getState().intent).toBe('available');
    });
  });

  describe('searchGroups', () => {
    it('searches successfully', async () => {
      mockSendCommand.mockResolvedValueOnce([{ id: 'g1', displayName: 'Group A' }]);
      await useBulkAppAssignmentsStore.getState().searchGroups('group');
      expect(useBulkAppAssignmentsStore.getState().groupSearchResults).toHaveLength(1);
    });

    it('clears for short query', async () => {
      await useBulkAppAssignmentsStore.getState().searchGroups('a');
      expect(useBulkAppAssignmentsStore.getState().groupSearchResults).toEqual([]);
      expect(mockSendCommand).not.toHaveBeenCalled();
    });

    it('ignores stale results', async () => {
      let resolve: (v: unknown) => void;
      mockSendCommand.mockImplementationOnce(() => new Promise(r => { resolve = r; }));
      const promise = useBulkAppAssignmentsStore.getState().searchGroups('old');
      useBulkAppAssignmentsStore.setState({ groupSearchQuery: 'new' });
      resolve!([{ id: 'g1' }]);
      await promise;
      expect(useBulkAppAssignmentsStore.getState().groupSearchResults).toEqual([]);
    });
  });

  describe('target management', () => {
    it('addTarget adds with defaults', () => {
      useBulkAppAssignmentsStore.getState().addTarget('group', { targetId: 'g1', displayName: 'Group A', groupType: 'Assigned' });
      const targets = useBulkAppAssignmentsStore.getState().targets;
      expect(targets).toHaveLength(1);
      expect(targets[0].targetType).toBe('group');
      expect(targets[0].isExclusion).toBe(false);
      expect(targets[0].filterMode).toBe('none');
    });

    it('updateTarget merges updates', () => {
      useBulkAppAssignmentsStore.getState().addTarget('group', { targetId: 'g1', displayName: 'Group A', groupType: 'Assigned' });
      const id = useBulkAppAssignmentsStore.getState().targets[0].id;
      useBulkAppAssignmentsStore.getState().updateTarget(id, { isExclusion: true });
      expect(useBulkAppAssignmentsStore.getState().targets[0].isExclusion).toBe(true);
    });

    it('removeTarget removes', () => {
      useBulkAppAssignmentsStore.getState().addTarget('group', { targetId: 'g1', displayName: 'A', groupType: 'Assigned' });
      const id = useBulkAppAssignmentsStore.getState().targets[0].id;
      useBulkAppAssignmentsStore.getState().removeTarget(id);
      expect(useBulkAppAssignmentsStore.getState().targets).toHaveLength(0);
    });
  });

  describe('applyAssignments', () => {
    it('validates apps selected', async () => {
      await useBulkAppAssignmentsStore.getState().applyAssignments();
      expect(useBulkAppAssignmentsStore.getState().error).toBe('Select at least one app before applying assignments');
    });

    it('validates targets added', async () => {
      useBulkAppAssignmentsStore.setState({ selectedAppIds: ['a1'] });
      await useBulkAppAssignmentsStore.getState().applyAssignments();
      expect(useBulkAppAssignmentsStore.getState().error).toBe('Add at least one target before applying assignments');
    });

    it('applies successfully', async () => {
      useBulkAppAssignmentsStore.setState({ selectedAppIds: ['a1'] });
      useBulkAppAssignmentsStore.getState().addTarget('allUsers', { targetId: '', displayName: 'All Users', groupType: undefined });
      const result = { appResults: [{ appId: 'a1', success: true }] };
      mockSendCommand
        .mockResolvedValueOnce(result) // apply
        .mockResolvedValueOnce({ apps: [], assignmentFilters: [] }); // bootstrap reload

      await useBulkAppAssignmentsStore.getState().applyAssignments();

      expect(useBulkAppAssignmentsStore.getState().lastApplyResult).toEqual(result);
      expect(useBulkAppAssignmentsStore.getState().isApplying).toBe(false);
    });

    it('sets error on failure', async () => {
      useBulkAppAssignmentsStore.setState({ selectedAppIds: ['a1'] });
      useBulkAppAssignmentsStore.getState().addTarget('allUsers', { targetId: '', displayName: 'All Users', groupType: undefined });
      mockSendCommand.mockRejectedValueOnce(new Error('Apply failed'));

      await useBulkAppAssignmentsStore.getState().applyAssignments();

      expect(useBulkAppAssignmentsStore.getState().error).toBe('Apply failed');
    });
  });
});
