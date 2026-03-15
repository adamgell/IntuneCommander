import { useEffect } from 'react';
import { useAppStore } from './store/appStore';
import { LoginScreen } from './components/login/LoginScreen';
import { AppShell } from './components/shell/AppShell';
import './styles/login.css';

export function App() {
  const isConnected = useAppStore((s) => s.isConnected);
  const isAutoConnecting = useAppStore((s) => s.isAutoConnecting);
  const activeProfile = useAppStore((s) => s.activeProfile);
  const loadProfiles = useAppStore((s) => s.loadProfiles);

  useEffect(() => {
    void loadProfiles();
  }, [loadProfiles]);

  if (isAutoConnecting) {
    return (
      <div className="login-screen">
        <div className="reconnecting-screen">
          <div className="reconnecting-spinner" />
          <strong>Reconnecting...</strong>
          <span>{activeProfile?.name ?? 'Loading profile'}</span>
        </div>
      </div>
    );
  }

  return isConnected ? <AppShell /> : <LoginScreen />;
}
