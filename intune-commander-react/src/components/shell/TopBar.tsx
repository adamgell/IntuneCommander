import type { KeyboardEvent } from 'react';
import { useAppStore } from '../../store/appStore';
import { useSearchStore } from '../../store/searchStore';
import { primaryNavTabs } from '../../types/models';
import '../../styles/topbar.css';

export function TopBar() {
  const activePrimaryTab = useAppStore((s) => s.activePrimaryTab);
  const activeSecondaryTab = useAppStore((s) => s.activeSecondaryTab);
  const activeProfile = useAppStore((s) => s.activeProfile);
  const setPrimaryTab = useAppStore((s) => s.setPrimaryTab);
  const setSecondaryTab = useAppStore((s) => s.setSecondaryTab);
  const disconnect = useAppStore((s) => s.disconnect);

  const searchQuery = useSearchStore((s) => s.query);
  const search = useSearchStore((s) => s.search);
  const clear = useSearchStore((s) => s.clear);

  const currentPrimary = primaryNavTabs.find((t) => t.id === activePrimaryTab);

  const handleKeyDown = (e: KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Escape') {
      clear();
      (e.target as HTMLInputElement).blur();
    }
  };

  return (
    <div className="topbar">
      <div className="topbar-primary">
        <div className="topbar-left">
          <div className="product-mark" />
          <div className="product-copy">
            <strong>Intune Commander</strong>
            <span>Local desktop shell</span>
          </div>
        </div>
        <div className="topbar-right">
          {activeProfile && (
            <span className="header-status">
              {activeProfile.name} / {activeProfile.cloud}
            </span>
          )}
          <input
            className="topbar-search"
            type="text"
            placeholder="Search cached policies, scripts, configs..."
            value={searchQuery}
            onChange={(e) => search(e.target.value)}
            onKeyDown={handleKeyDown}
          />
          <button className="topbar-btn secondary" onClick={disconnect}>
            Disconnect
          </button>
        </div>
      </div>
      <div className="topbar-secondary">
        <div className="primary-nav">
          {primaryNavTabs.map((tab) => (
            <button
              key={tab.id}
              className={`nav-tier-link${tab.id === activePrimaryTab ? ' active' : ''}`}
              onClick={() => setPrimaryTab(tab.id)}
            >
              {tab.label}
            </button>
          ))}
        </div>
        {currentPrimary && (
          <div className="secondary-nav">
            {currentPrimary.secondaryTabs.map((tab) => (
              <button
                key={tab.id}
                className={`nav-tier-link secondary${tab.id === activeSecondaryTab ? ' active' : ''}`}
                onClick={() => setSecondaryTab(tab.id)}
              >
                {tab.label}
              </button>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
