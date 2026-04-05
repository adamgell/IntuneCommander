import { vi, describe, it, expect, beforeEach, afterEach } from 'vitest';

const mockSendCommand = vi.fn();
vi.mock('../../bridge/bridgeClient', () => ({
  sendCommand: mockSendCommand,
  onEvent: vi.fn(() => vi.fn()),
}));

const mockSetSidebarItem = vi.fn();
vi.mock('../appStore', () => ({
  useAppStore: {
    getState: vi.fn(() => ({
      activeSidebarItem: 'other',
      setSidebarItem: mockSetSidebarItem,
    })),
  },
}));

const { useSearchStore } = await import('../searchStore');
const initialState = useSearchStore.getState();

beforeEach(() => {
  useSearchStore.setState({ ...initialState });
  vi.clearAllMocks();
  vi.useFakeTimers();
});

afterEach(() => {
  vi.useRealTimers();
});

describe('searchStore', () => {
  it('has correct initial state', () => {
    const state = useSearchStore.getState();
    expect(state.query).toBe('');
    expect(state.results).toEqual([]);
    expect(state.isSearching).toBe(false);
  });

  describe('search', () => {
    it('clears results for short query without calling sendCommand', () => {
      useSearchStore.setState({ results: [{ id: '1', name: 'X', category: 'C', categoryKey: 'c' }] });
      useSearchStore.getState().search('a');
      expect(useSearchStore.getState().results).toEqual([]);
      expect(mockSendCommand).not.toHaveBeenCalled();
    });

    it('clears results for empty query', () => {
      useSearchStore.getState().search('');
      expect(useSearchStore.getState().results).toEqual([]);
    });

    it('sends command after 250ms debounce', async () => {
      const mockResults = [{ id: '1', name: 'Config', category: 'Policies', categoryKey: 'p' }];
      mockSendCommand.mockResolvedValueOnce(mockResults);
      useSearchStore.getState().search('device');
      expect(useSearchStore.getState().isSearching).toBe(true);
      expect(mockSendCommand).not.toHaveBeenCalled();

      await vi.advanceTimersByTimeAsync(250);

      expect(mockSendCommand).toHaveBeenCalledWith('search.query', { query: 'device' });
      expect(useSearchStore.getState().results).toEqual(mockResults);
      expect(useSearchStore.getState().isSearching).toBe(false);
    });

    it('navigates to global-search sidebar', () => {
      useSearchStore.getState().search('device');
      expect(mockSetSidebarItem).toHaveBeenCalledWith('global-search');
    });

    it('debounces rapid calls', async () => {
      mockSendCommand.mockResolvedValue([]);
      useSearchStore.getState().search('de');
      useSearchStore.getState().search('dev');
      useSearchStore.getState().search('device');
      await vi.advanceTimersByTimeAsync(250);
      expect(mockSendCommand).toHaveBeenCalledTimes(1);
      expect(mockSendCommand).toHaveBeenCalledWith('search.query', { query: 'device' });
    });

    it('ignores stale query response', async () => {
      mockSendCommand.mockResolvedValueOnce([{ id: '1', name: 'Old', category: 'C', categoryKey: 'c' }]);
      useSearchStore.getState().search('old');
      useSearchStore.setState({ query: 'new' });
      await vi.advanceTimersByTimeAsync(250);
      expect(useSearchStore.getState().results).toEqual([]);
    });
  });

  describe('clear', () => {
    it('resets state and cancels timer', () => {
      useSearchStore.getState().search('test');
      useSearchStore.getState().clear();
      expect(useSearchStore.getState().query).toBe('');
      expect(useSearchStore.getState().isSearching).toBe(false);
      vi.advanceTimersByTime(300);
      expect(mockSendCommand).not.toHaveBeenCalled();
    });
  });
});
