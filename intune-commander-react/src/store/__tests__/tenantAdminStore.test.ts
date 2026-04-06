import { vi, describe, it, expect, beforeEach } from 'vitest';

const mockSendCommand = vi.fn();
vi.mock('../../bridge/bridgeClient', () => ({
  sendCommand: mockSendCommand,
  onEvent: vi.fn(() => vi.fn()),
}));

const { useTenantAdminStore } = await import('../tenantAdminStore');
const initialState = useTenantAdminStore.getState();

beforeEach(() => {
  useTenantAdminStore.setState({ ...initialState });
  vi.clearAllMocks();
});

describe('tenantAdminStore', () => {
  it('has correct initial state', () => {
    const state = useTenantAdminStore.getState();
    expect(state.activeEntityType).toBe('scopeTags');
    expect(state.items).toEqual([]);
    expect(state.selectedId).toBeNull();
    expect(state.detail).toBeNull();
    expect(state.isLoadingList).toBe(false);
    expect(state.isLoadingDetail).toBe(false);
    expect(state.hasAttemptedLoad).toBe(false);
    expect(state.error).toBeNull();
  });

  describe('setEntityType', () => {
    it('resets all state', () => {
      useTenantAdminStore.setState({ items: [{}] as any[], selectedId: '1', detail: {}, hasAttemptedLoad: true, error: 'old' });
      useTenantAdminStore.getState().setEntityType('roles');
      const state = useTenantAdminStore.getState();
      expect(state.activeEntityType).toBe('roles');
      expect(state.items).toEqual([]);
      expect(state.selectedId).toBeNull();
      expect(state.hasAttemptedLoad).toBe(false);
    });
  });

  describe('loadItems', () => {
    it('loads with dynamic command name', async () => {
      mockSendCommand.mockResolvedValueOnce([{ id: '1', displayName: 'Tag A' }]);
      await useTenantAdminStore.getState().loadItems();
      expect(mockSendCommand).toHaveBeenCalledWith('tenantAdmin.scopeTags.list');
      expect(useTenantAdminStore.getState().items).toHaveLength(1);
    });

    it('uses correct command for different entity type', async () => {
      useTenantAdminStore.setState({ activeEntityType: 'roles' });
      mockSendCommand.mockResolvedValueOnce([]);
      await useTenantAdminStore.getState().loadItems();
      expect(mockSendCommand).toHaveBeenCalledWith('tenantAdmin.roles.list');
    });

    it('ignores response if entity type changed', async () => {
      let resolve: (v: unknown) => void;
      mockSendCommand.mockImplementationOnce(() => new Promise(r => { resolve = r; }));
      const promise = useTenantAdminStore.getState().loadItems();
      useTenantAdminStore.setState({ activeEntityType: 'roles' });
      resolve!([{ id: '1' }]);
      await promise;
      expect(useTenantAdminStore.getState().items).toEqual([]);
    });

    it('sets error on failure', async () => {
      mockSendCommand.mockRejectedValueOnce(new Error('Network error'));
      await useTenantAdminStore.getState().loadItems();
      expect(useTenantAdminStore.getState().error).toBe('Network error');
    });
  });

  describe('selectItem', () => {
    it('loads detail successfully', async () => {
      mockSendCommand.mockResolvedValueOnce({ id: '1', displayName: 'Tag A' });
      await useTenantAdminStore.getState().selectItem('1');
      expect(useTenantAdminStore.getState().detail).toEqual({ id: '1', displayName: 'Tag A' });
      expect(mockSendCommand).toHaveBeenCalledWith('tenantAdmin.scopeTags.getDetail', { id: '1' });
    });

    it('ignores stale response when selection changes', async () => {
      let resolve: (v: unknown) => void;
      mockSendCommand.mockImplementationOnce(() => new Promise(r => { resolve = r; }));
      const promise = useTenantAdminStore.getState().selectItem('1');
      useTenantAdminStore.setState({ selectedId: '2' });
      resolve!({ id: '1' });
      await promise;
      expect(useTenantAdminStore.getState().detail).toBeNull();
    });

    it('ignores stale response when entity type changes', async () => {
      let resolve: (v: unknown) => void;
      mockSendCommand.mockImplementationOnce(() => new Promise(r => { resolve = r; }));
      const promise = useTenantAdminStore.getState().selectItem('1');
      useTenantAdminStore.setState({ activeEntityType: 'roles' });
      resolve!({ id: '1' });
      await promise;
      expect(useTenantAdminStore.getState().detail).toBeNull();
    });

    it('sets error on failure', async () => {
      mockSendCommand.mockRejectedValueOnce(new Error('Not found'));
      await useTenantAdminStore.getState().selectItem('1');
      expect(useTenantAdminStore.getState().error).toBe('Not found');
    });
  });

  describe('clearSelection', () => {
    it('resets selection', () => {
      useTenantAdminStore.setState({ selectedId: '1', detail: {}, isLoadingDetail: true });
      useTenantAdminStore.getState().clearSelection();
      expect(useTenantAdminStore.getState().selectedId).toBeNull();
      expect(useTenantAdminStore.getState().detail).toBeNull();
    });
  });
});
