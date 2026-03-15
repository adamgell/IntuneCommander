import { create } from 'zustand';
import { sendCommand } from '../bridge/bridgeClient';
import { useAppStore } from './appStore';

export interface SearchResult {
  category: string;
  categoryKey: string;
  id: string;
  name: string;
  description?: string;
}

interface SearchState {
  query: string;
  results: SearchResult[];
  isSearching: boolean;

  search: (query: string) => void;
  clear: () => void;
}

let debounceTimer: ReturnType<typeof setTimeout> | null = null;

export const useSearchStore = create<SearchState>((set, get) => ({
  query: '',
  results: [],
  isSearching: false,

  search: (query: string) => {
    set({ query });

    if (debounceTimer) {
      clearTimeout(debounceTimer);
      debounceTimer = null;
    }

    if (!query || query.trim().length < 2) {
      set({ results: [], isSearching: false });
      return;
    }

    // Navigate to search workspace as soon as the user starts typing
    useAppStore.getState().setSidebarItem('global-search');

    set({ isSearching: true });

    debounceTimer = setTimeout(async () => {
      debounceTimer = null;
      try {
        const results = await sendCommand<SearchResult[]>('search.query', { query: query.trim() });
        // Only update if query hasn't changed
        if (get().query === query) {
          set({ results, isSearching: false });
        }
      } catch {
        if (get().query === query) {
          set({ results: [], isSearching: false });
        }
      }
    }, 250);
  },

  clear: () => {
    if (debounceTimer) {
      clearTimeout(debounceTimer);
      debounceTimer = null;
    }
    set({ query: '', results: [], isSearching: false });
  },
}));
