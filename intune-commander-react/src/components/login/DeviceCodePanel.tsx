import { useAppStore } from '../../store/appStore';

export function DeviceCodePanel() {
  const deviceCode = useAppStore((s) => s.deviceCode);

  if (!deviceCode) return null;

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(deviceCode.userCode);
    } catch {
      // Clipboard may not be available in some contexts
    }
  };

  return (
    <div className="device-code-panel">
      <p>Enter this code at the sign-in page:</p>
      <div className="device-code-display">
        <span className="device-code-value">{deviceCode.userCode}</span>
        <button className="btn-copy" onClick={() => void handleCopy()}>
          Copy Code
        </button>
      </div>
      <span className="device-code-url">{deviceCode.verificationUrl}</span>
    </div>
  );
}
