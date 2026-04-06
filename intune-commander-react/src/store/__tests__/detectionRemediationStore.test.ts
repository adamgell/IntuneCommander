import { vi, describe, it, expect, beforeEach, afterEach, afterAll } from 'vitest';

// Stub window for node environment (store uses window.setInterval/setTimeout)
vi.stubGlobal('window', {
  setInterval: globalThis.setInterval.bind(globalThis),
  setTimeout: globalThis.setTimeout.bind(globalThis),
  clearInterval: globalThis.clearInterval.bind(globalThis),
  clearTimeout: globalThis.clearTimeout.bind(globalThis),
});

afterAll(() => { vi.unstubAllGlobals(); });

const mockSendCommand = vi.fn();
vi.mock('../../bridge/bridgeClient', () => ({
  sendCommand: mockSendCommand,
  onEvent: vi.fn(() => vi.fn()),
}));

const { useDetectionRemediationStore } = await import('../detectionRemediationStore');
const initialState = useDetectionRemediationStore.getState();

beforeEach(() => {
  useDetectionRemediationStore.setState({
    ...initialState,
    selectedDeviceIds: new Set(),
    deploymentRecords: [],
    monitoringStates: [],
  });
  vi.clearAllMocks();
  vi.useFakeTimers();
});

afterEach(() => {
  useDetectionRemediationStore.getState().stopMonitoring();
  vi.useRealTimers();
});

describe('detectionRemediationStore', () => {
  it('has correct initial state', () => {
    const state = useDetectionRemediationStore.getState();
    expect(state.scripts).toEqual([]);
    expect(state.selectedScriptId).toBeNull();
    expect(state.scriptDetail).toBeNull();
    expect(state.isLoadingList).toBe(false);
    expect(state.isSaving).toBe(false);
    expect(state.deviceSearchResults).toEqual([]);
    expect(state.isMonitoring).toBe(false);
  });

  describe('loadScripts', () => {
    it('loads successfully', async () => {
      mockSendCommand.mockResolvedValueOnce([{ id: 's1' }]);
      await useDetectionRemediationStore.getState().loadScripts();
      expect(useDetectionRemediationStore.getState().scripts).toHaveLength(1);
      expect(mockSendCommand).toHaveBeenCalledWith('healthScripts.list');
    });

    it('sets error on failure', async () => {
      mockSendCommand.mockRejectedValueOnce(new Error('Failed'));
      await useDetectionRemediationStore.getState().loadScripts();
      expect(useDetectionRemediationStore.getState().error).toBe('Failed');
    });
  });

  describe('selectScript', () => {
    it('loads detail', async () => {
      mockSendCommand.mockResolvedValueOnce({ id: 's1', displayName: 'Script A' });
      await useDetectionRemediationStore.getState().selectScript('s1');
      expect(useDetectionRemediationStore.getState().scriptDetail).toEqual({ id: 's1', displayName: 'Script A' });
      expect(mockSendCommand).toHaveBeenCalledWith('healthScripts.getDetail', { id: 's1' });
    });

    it('ignores stale response', async () => {
      let resolve: (v: unknown) => void;
      mockSendCommand.mockImplementationOnce(() => new Promise(r => { resolve = r; }));
      const promise = useDetectionRemediationStore.getState().selectScript('s1');
      useDetectionRemediationStore.setState({ selectedScriptId: 's2' });
      resolve!({ id: 's1' });
      await promise;
      expect(useDetectionRemediationStore.getState().scriptDetail).toBeNull();
    });
  });

  describe('clearSelection', () => {
    it('resets script and deploy state', () => {
      useDetectionRemediationStore.setState({
        selectedScriptId: 's1',
        scriptDetail: {} as any,
        deploymentRecords: [{}] as any[],
        monitoringStates: [{}] as any[],
        isMonitoring: true,
      });
      useDetectionRemediationStore.getState().clearSelection();
      const state = useDetectionRemediationStore.getState();
      expect(state.selectedScriptId).toBeNull();
      expect(state.deploymentRecords).toEqual([]);
      expect(state.isMonitoring).toBe(false);
    });
  });

  describe('saveScript', () => {
    it('saves and reloads detail', async () => {
      mockSendCommand
        .mockResolvedValueOnce(undefined) // update
        .mockResolvedValueOnce({ id: 's1', displayName: 'Updated' }) // getDetail
        .mockResolvedValueOnce([]); // loadScripts (fire-and-forget)

      const result = await useDetectionRemediationStore.getState().saveScript({ id: 's1', displayName: 'Updated' });
      expect(result).toBe(true);
      expect(useDetectionRemediationStore.getState().scriptDetail).toEqual({ id: 's1', displayName: 'Updated' });
      expect(useDetectionRemediationStore.getState().isSaving).toBe(false);
    });

    it('returns false on failure', async () => {
      mockSendCommand.mockRejectedValueOnce(new Error('Save failed'));
      const result = await useDetectionRemediationStore.getState().saveScript({ id: 's1' });
      expect(result).toBe(false);
      expect(useDetectionRemediationStore.getState().error).toBe('Save failed');
    });
  });

  describe('searchDevices', () => {
    it('searches successfully', async () => {
      mockSendCommand.mockResolvedValueOnce([{ id: 'd1', deviceName: 'PC1' }]);
      await useDetectionRemediationStore.getState().searchDevices('PC');
      expect(useDetectionRemediationStore.getState().deviceSearchResults).toHaveLength(1);
      expect(mockSendCommand).toHaveBeenCalledWith('devices.search', { query: 'PC' });
    });
  });

  describe('device selection', () => {
    it('toggleDeviceSelection adds and removes', () => {
      useDetectionRemediationStore.getState().toggleDeviceSelection('d1');
      expect(useDetectionRemediationStore.getState().selectedDeviceIds.has('d1')).toBe(true);
      useDetectionRemediationStore.getState().toggleDeviceSelection('d1');
      expect(useDetectionRemediationStore.getState().selectedDeviceIds.has('d1')).toBe(false);
    });

    it('selectAllDevices selects all search results', () => {
      useDetectionRemediationStore.setState({ deviceSearchResults: [{ id: 'd1' }, { id: 'd2' }] as any[] });
      useDetectionRemediationStore.getState().selectAllDevices();
      expect(useDetectionRemediationStore.getState().selectedDeviceIds.size).toBe(2);
    });

    it('clearDeviceSelection clears all', () => {
      useDetectionRemediationStore.setState({ selectedDeviceIds: new Set(['d1', 'd2']) });
      useDetectionRemediationStore.getState().clearDeviceSelection();
      expect(useDetectionRemediationStore.getState().selectedDeviceIds.size).toBe(0);
    });
  });

  describe('deployToDevices', () => {
    it('deploys and starts monitoring', async () => {
      mockSendCommand
        .mockResolvedValueOnce([{ deviceId: 'd1', status: 'success' }]) // deploy
        .mockResolvedValueOnce([]); // refreshMonitoring

      await useDetectionRemediationStore.getState().deployToDevices('s1', [{ id: 'd1', deviceName: 'PC1' }]);

      expect(useDetectionRemediationStore.getState().deploymentRecords).toHaveLength(1);
      expect(useDetectionRemediationStore.getState().isMonitoring).toBe(true);
      expect(mockSendCommand).toHaveBeenCalledWith('healthScripts.deploy', { scriptId: 's1', devices: [{ id: 'd1', deviceName: 'PC1' }] });
    });

    it('sets error on failure', async () => {
      mockSendCommand.mockRejectedValueOnce(new Error('Deploy failed'));
      await useDetectionRemediationStore.getState().deployToDevices('s1', []);
      expect(useDetectionRemediationStore.getState().error).toBe('Deploy failed');
    });
  });

  describe('monitoring', () => {
    it('startMonitoring sets isMonitoring and refreshes', () => {
      mockSendCommand.mockResolvedValue([]);
      useDetectionRemediationStore.getState().startMonitoring('s1');
      expect(useDetectionRemediationStore.getState().isMonitoring).toBe(true);
      expect(mockSendCommand).toHaveBeenCalledWith('healthScripts.refreshRunStates', { id: 's1' });
    });

    it('stopMonitoring clears state', () => {
      mockSendCommand.mockResolvedValue([]);
      useDetectionRemediationStore.getState().startMonitoring('s1');
      useDetectionRemediationStore.getState().stopMonitoring();
      expect(useDetectionRemediationStore.getState().isMonitoring).toBe(false);
      expect(useDetectionRemediationStore.getState().monitorIntervalId).toBeNull();
    });
  });

  describe('resetDeployState', () => {
    it('resets all deploy/monitor state', () => {
      useDetectionRemediationStore.setState({
        deviceSearchResults: [{}] as any[],
        selectedDeviceIds: new Set(['d1']),
        deploymentRecords: [{}] as any[],
        isMonitoring: true,
      });
      useDetectionRemediationStore.getState().resetDeployState();
      const state = useDetectionRemediationStore.getState();
      expect(state.deviceSearchResults).toEqual([]);
      expect(state.selectedDeviceIds.size).toBe(0);
      expect(state.deploymentRecords).toEqual([]);
      expect(state.isMonitoring).toBe(false);
    });
  });
});
