import { TopBar } from './TopBar';
import { Sidebar } from './Sidebar';
import { ContentArea } from './ContentArea';
import { StatusBar } from './StatusBar';
import '../../styles/shell.css';

export function AppShell() {
  return (
    <div className="app-shell">
      <TopBar />
      <div className="main-grid">
        <Sidebar />
        <ContentArea />
      </div>
      <StatusBar />
    </div>
  );
}
