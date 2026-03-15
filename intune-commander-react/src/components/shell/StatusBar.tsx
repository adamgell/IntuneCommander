import { useAppStore } from '../../store/appStore';
import { useCacheSyncStore } from '../../store/cacheSyncStore';

export function StatusBar() {
  const isConnected = useAppStore((s) => s.isConnected);
  const isBusy = useAppStore((s) => s.isBusy);
  const statusText = useAppStore((s) => s.statusText);
  const errorMessage = useAppStore((s) => s.errorMessage);

  const isSyncing = useCacheSyncStore((s) => s.isSyncing);
  const progress = useCacheSyncStore((s) => s.progress);
  const lastResult = useCacheSyncStore((s) => s.lastResult);
  const syncAll = useCacheSyncStore((s) => s.syncAll);

  const syncLabel = isSyncing && progress
    ? `Syncing ${progress.current}/${progress.total}: ${progress.label}...`
    : lastResult
      ? `Synced: ${lastResult.successCount}/${lastResult.totalTypes}${lastResult.errorCount > 0 ? ` (${lastResult.errorCount} errors)` : ''}`
      : 'Cache: Ready';

  return (
    <div className="status-bar">
      <div className="status-indicator">
        <span className={`status-dot ${isConnected ? 'connected' : 'disconnected'}`} />
        <span>{statusText}</span>
      </div>

      <span>{syncLabel}</span>

      {isConnected && (
        <button
          className="status-bar-sync-btn"
          onClick={syncAll}
          disabled={isSyncing}
          title="Fetch all Intune data types and populate the cache"
        >
          {isSyncing ? 'Syncing...' : 'Sync All'}
        </button>
      )}

      <div className="status-bar-spacer" />

      {isBusy && (
        <span className="status-bar-busy">Working...</span>
      )}

      {errorMessage && (
        <span className="status-bar-error" title={errorMessage}>
          {errorMessage}
        </span>
      )}

    </div>
  );
}
