import { vi, describe, it, expect, beforeEach } from 'vitest';

const mockSendCommand = vi.fn();
vi.mock('../../bridge/bridgeClient', () => ({
  sendCommand: mockSendCommand,
  onEvent: vi.fn(() => vi.fn()),
}));

const { useScriptsHubStore } = await import('../scriptsHubStore');
const initialState = useScriptsHubStore.getState();

beforeEach(() => {
  useScriptsHubStore.setState({ ...initialState });
  vi.clearAllMocks();
});

describe('scriptsHubStore', () => {
  it('has correct initial state', () => {
    const state = useScriptsHubStore.getState();
    expect(state.scripts).toEqual([]);
    expect(state.selectedScriptId).toBeNull();
    expect(state.selectedScriptType).toBeNull();
    expect(state.scriptDetail).toBeNull();
    expect(state.isLoadingList).toBe(false);
    expect(state.isLoadingDetail).toBe(false);
    expect(state.hasAttemptedLoad).toBe(false);
    expect(state.error).toBeNull();
    expect(state.typeFilter).toBeNull();
  });

  describe('loadScripts', () => {
    it('loads successfully', async () => {
      const mock = [{ id: 's1', displayName: 'Script A' }];
      mockSendCommand.mockResolvedValueOnce(mock);
      await useScriptsHubStore.getState().loadScripts();
      expect(useScriptsHubStore.getState().scripts).toEqual(mock);
      expect(useScriptsHubStore.getState().isLoadingList).toBe(false);
      expect(mockSendCommand).toHaveBeenCalledWith('scripts.listAll');
    });

    it('sets error on failure', async () => {
      mockSendCommand.mockRejectedValueOnce(new Error('Failed'));
      await useScriptsHubStore.getState().loadScripts();
      expect(useScriptsHubStore.getState().error).toBe('Failed');
    });
  });

  describe('selectScript', () => {
    it('loads detail with scriptType', async () => {
      const mock = { id: 's1', displayName: 'Script A' };
      mockSendCommand.mockResolvedValueOnce(mock);
      await useScriptsHubStore.getState().selectScript('s1', 'powershell' as any);
      expect(useScriptsHubStore.getState().scriptDetail).toEqual(mock);
      expect(mockSendCommand).toHaveBeenCalledWith('scripts.getDetail', { id: 's1', scriptType: 'powershell' });
    });

    it('ignores stale response', async () => {
      let resolve: (v: unknown) => void;
      mockSendCommand.mockImplementationOnce(() => new Promise(r => { resolve = r; }));
      const promise = useScriptsHubStore.getState().selectScript('s1', 'powershell' as any);
      useScriptsHubStore.setState({ selectedScriptId: 's2' });
      resolve!({ id: 's1' });
      await promise;
      expect(useScriptsHubStore.getState().scriptDetail).toBeNull();
    });

    it('sets error on failure', async () => {
      mockSendCommand.mockRejectedValueOnce(new Error('Not found'));
      await useScriptsHubStore.getState().selectScript('s1', 'powershell' as any);
      expect(useScriptsHubStore.getState().error).toBe('Not found');
    });
  });

  describe('clearSelection', () => {
    it('resets selection state', () => {
      useScriptsHubStore.setState({ selectedScriptId: 's1', selectedScriptType: 'powershell' as any, scriptDetail: {} as any });
      useScriptsHubStore.getState().clearSelection();
      expect(useScriptsHubStore.getState().selectedScriptId).toBeNull();
      expect(useScriptsHubStore.getState().scriptDetail).toBeNull();
    });
  });

  describe('setTypeFilter', () => {
    it('sets and clears filter', () => {
      useScriptsHubStore.getState().setTypeFilter('powershell' as any);
      expect(useScriptsHubStore.getState().typeFilter).toBe('powershell');
      useScriptsHubStore.getState().setTypeFilter(null);
      expect(useScriptsHubStore.getState().typeFilter).toBeNull();
    });
  });
});
