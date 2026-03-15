import { BrandingPanel } from './BrandingPanel';
import { LoginForm } from './LoginForm';
import '../../styles/login.css';

export function LoginScreen() {
  return (
    <div className="login-screen">
      <BrandingPanel />
      <LoginForm />
    </div>
  );
}
