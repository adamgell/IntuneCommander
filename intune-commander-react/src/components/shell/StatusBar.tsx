import { useAppStore } from '../../store/appStore';

export function StatusBar() {
  const isConnected = useAppStore((s) => s.isConnected);
  const isBusy = useAppStore((s) => s.isBusy);
  const statusText = useAppStore((s) => s.statusText);
  const errorMessage = useAppStore((s) => s.errorMessage);

  return (
    <div className="status-bar">
      <div className="status-indicator">
        <span className={`status-dot ${isConnected ? 'connected' : 'disconnected'}`} />
        <span>{statusText}</span>
      </div>

      <span>Cache: Ready</span>

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
