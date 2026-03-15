import { useSearchStore, type SearchResult } from '../../store/searchStore';
import { useAppStore } from '../../store/appStore';
import { useSettingsCatalogStore } from '../../store/settingsCatalogStore';
import '../../styles/workspace.css';

export function GlobalSearchResultsWorkspace() {
  const query = useSearchStore((s) => s.query);
  const results = useSearchStore((s) => s.results);
  const isSearching = useSearchStore((s) => s.isSearching);

  const setSidebarItem = useAppStore((s) => s.setSidebarItem);
  const selectPolicy = useSettingsCatalogStore((s) => s.selectPolicy);

  // Group results by category
  const grouped = results.reduce<Record<string, SearchResult[]>>((acc, r) => {
    (acc[r.category] ??= []).push(r);
    return acc;
  }, {});

  const categoryCount = Object.keys(grouped).length;

  const handleView = (result: SearchResult) => {
    if (result.categoryKey === 'SettingsCatalog') {
      setSidebarItem('settings-catalog');
      void selectPolicy(result.id);
    } else {
      setSidebarItem(result.categoryKey);
    }
  };

  return (
    <div className="workspace">
      <div className="workspace-toolbar">
        <div className="workspace-heading">
          <strong className="workspace-title">Search Results</strong>
          <div className="workspace-stats">
            {query && (
              <span className="inline-stat">
                <strong>{results.length}</strong> results across{' '}
                <strong>{categoryCount}</strong> categories for &ldquo;{query}&rdquo;
              </span>
            )}
          </div>
        </div>
      </div>

      {isSearching && (
        <div className="search-workspace-status">
          <div className="loading-spinner" />
          <span>Searching cached data...</span>
        </div>
      )}

      {!isSearching && query && results.length === 0 && (
        <div className="search-workspace-empty">
          <h3>No results found</h3>
          <p>No cached data matches &ldquo;{query}&rdquo;. Try a different term or load more data first.</p>
        </div>
      )}

      {!isSearching && !query && (
        <div className="search-workspace-empty">
          <h3>Start searching</h3>
          <p>Type in the search bar above to find policies, scripts, and configurations across all cached data.</p>
        </div>
      )}

      <div className="search-results-list">
        {Object.entries(grouped).map(([category, items]) => (
          <div key={category} className="search-category-group">
            <div className="search-category-header">
              <span className="search-category-label">{category}</span>
              <span className="search-category-count">{items.length}</span>
            </div>
            <table className="search-results-table">
              <thead>
                <tr>
                  <th>Name</th>
                  <th>Description</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {items.map((result) => (
                  <tr key={`${result.categoryKey}-${result.id}`}>
                    <td className="search-result-name">{result.name}</td>
                    <td className="search-result-desc">{result.description || '\u2014'}</td>
                    <td className="search-result-actions">
                      <button
                        className="ws-btn secondary small"
                        onClick={() => handleView(result)}
                      >
                        View
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ))}
      </div>

      <div className="workspace-footer">
        <span>
          {results.length} result{results.length !== 1 ? 's' : ''} from cached data
        </span>
        {isSearching && <span>Searching...</span>}
      </div>
    </div>
  );
}
