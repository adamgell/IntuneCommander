import { vi, describe, it, expect, beforeEach } from 'vitest';
import type { GroupListItem, GroupDetail } from '../../types/groups';

const mockSendCommand = vi.fn();
vi.mock('../../bridge/bridgeClient', () => ({
  sendCommand: mockSendCommand,
  onEvent: vi.fn(() => vi.fn()),
}));

const { useGroupsStore } = await import('../groupsStore');

const initialState = useGroupsStore.getState();

const mockGroups: GroupListItem[] = [
  { id: '1', displayName: 'Group A', groupType: 'Assigned', createdDateTime: '', mail: null },
  { id: '2', displayName: 'Group B', groupType: 'Dynamic Device', membershipRule: '(device.os -eq "Windows")', createdDateTime: '', mail: null },
];

const mockDetail = {
  id: '1',
  displayName: 'Group A',
  groupType: 'Assigned',
  memberCounts: { users: 5, devices: 0, nestedGroups: 0, total: 5 },
  members: [],
  assignments: [],
} as unknown as GroupDetail;

beforeEach(() => {
  useGroupsStore.setState({ ...initialState });
  vi.clearAllMocks();
});

describe('groupsStore', () => {
  it('has correct initial state', () => {
    const state = useGroupsStore.getState();
    expect(state.groups).toEqual([]);
    expect(state.selectedGroupId).toBeNull();
    expect(state.groupDetail).toBeNull();
    expect(state.isLoadingList).toBe(false);
    expect(state.isLoadingDetail).toBe(false);
    expect(state.hasAttemptedLoad).toBe(false);
    expect(state.error).toBeNull();
  });

  describe('loadGroups', () => {
    it('loads groups successfully', async () => {
      mockSendCommand.mockResolvedValueOnce(mockGroups);
      await useGroupsStore.getState().loadGroups();

      const state = useGroupsStore.getState();
      expect(state.groups).toHaveLength(2);
      expect(state.isLoadingList).toBe(false);
      expect(state.hasAttemptedLoad).toBe(true);
      expect(mockSendCommand).toHaveBeenCalledWith('groups.list');
    });

    it('sets error on failure', async () => {
      mockSendCommand.mockRejectedValueOnce(new Error('Network error'));
      await useGroupsStore.getState().loadGroups();

      const state = useGroupsStore.getState();
      expect(state.error).toBe('Network error');
      expect(state.isLoadingList).toBe(false);
      expect(state.hasAttemptedLoad).toBe(true);
    });
  });

  describe('selectGroup', () => {
    it('loads group detail successfully', async () => {
      mockSendCommand.mockResolvedValueOnce(mockDetail);
      await useGroupsStore.getState().selectGroup('1');

      const state = useGroupsStore.getState();
      expect(state.selectedGroupId).toBe('1');
      expect(state.groupDetail).toEqual(mockDetail);
      expect(state.isLoadingDetail).toBe(false);
    });

    it('ignores stale response when selection changes', async () => {
      let resolve: (v: unknown) => void;
      mockSendCommand.mockImplementationOnce(() => new Promise(r => { resolve = r; }));

      const promise = useGroupsStore.getState().selectGroup('1');
      useGroupsStore.getState().clearSelection();
      resolve!(mockDetail);
      await promise;

      expect(useGroupsStore.getState().groupDetail).toBeNull();
    });

    it('sets error on failure', async () => {
      mockSendCommand.mockRejectedValueOnce(new Error('Not found'));
      await useGroupsStore.getState().selectGroup('1');

      expect(useGroupsStore.getState().error).toBe('Not found');
      expect(useGroupsStore.getState().isLoadingDetail).toBe(false);
    });
  });

  describe('clearSelection', () => {
    it('resets selection state', () => {
      useGroupsStore.setState({ selectedGroupId: '1', groupDetail: mockDetail, isLoadingDetail: true });
      useGroupsStore.getState().clearSelection();

      const state = useGroupsStore.getState();
      expect(state.selectedGroupId).toBeNull();
      expect(state.groupDetail).toBeNull();
      expect(state.isLoadingDetail).toBe(false);
    });
  });

  describe('setTypeFilter', () => {
    it('sets filter value', () => {
      useGroupsStore.getState().setTypeFilter('Assigned');
      expect(useGroupsStore.getState().typeFilter).toBe('Assigned');
    });

    it('clears filter', () => {
      useGroupsStore.setState({ typeFilter: 'Assigned' });
      useGroupsStore.getState().setTypeFilter(null);
      expect(useGroupsStore.getState().typeFilter).toBeNull();
    });
  });

  describe('searchGroups', () => {
    it('searches successfully', async () => {
      mockSendCommand.mockResolvedValueOnce(mockGroups);
      await useGroupsStore.getState().searchGroups('test');

      expect(useGroupsStore.getState().groups).toHaveLength(2);
      expect(mockSendCommand).toHaveBeenCalledWith('groups.search', { query: 'test' });
    });

    it('ignores stale search results', async () => {
      let resolveFirst: (v: unknown) => void;
      mockSendCommand
        .mockImplementationOnce(() => new Promise(r => { resolveFirst = r; }))
        .mockResolvedValueOnce([mockGroups[1]]);

      const first = useGroupsStore.getState().searchGroups('old');
      await useGroupsStore.getState().searchGroups('new');

      resolveFirst!([mockGroups[0]]);
      await first;

      // Should have 'new' results, not 'old'
      expect(useGroupsStore.getState().searchQuery).toBe('new');
    });
  });
});
