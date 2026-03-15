import { useAppStore } from '../../store/appStore';
import { CloudEnvironment, AuthMethod } from '../../types/models';
import { ProfilePicker } from './ProfilePicker';
import { DeviceCodePanel } from './DeviceCodePanel';

const GUID_REGEX = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

function validateGuid(value: string): string | null {
  if (!value) return null;
  return GUID_REGEX.test(value) ? null : 'Must be a valid GUID (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx)';
}

export function LoginForm() {
  const form = useAppStore((s) => s.form);
  const isBusy = useAppStore((s) => s.isBusy);
  const statusText = useAppStore((s) => s.statusText);
  const errorMessage = useAppStore((s) => s.errorMessage);
  const updateForm = useAppStore((s) => s.updateForm);
  const connect = useAppStore((s) => s.connect);
  const saveProfile = useAppStore((s) => s.saveProfile);

  const tenantIdError = validateGuid(form.tenantId);
  const clientIdError = validateGuid(form.clientId);
  const canConnect = form.tenantId && form.clientId && !tenantIdError && !clientIdError && !isBusy;
  const canSave = form.tenantId && form.clientId && !tenantIdError && !clientIdError && !isBusy;

  return (
    <div className="login-form-container">
      <div className="login-form">
        <div className="login-heading">
          <h2>Connect to Tenant</h2>
          <p>Select a saved profile or enter your credentials below.</p>
        </div>

        <div className="form-card">
          <ProfilePicker />

          <div className="form-separator" />

          {/* Profile Name + Tenant ID */}
          <div className="form-row">
            <div className="form-field">
              <label>Profile Name (optional)</label>
              <input
                type="text"
                placeholder="e.g. Contoso-Prod"
                value={form.name}
                onChange={(e) => updateForm({ name: e.target.value })}
                disabled={isBusy}
              />
            </div>
            <div className="form-field">
              <label>Tenant ID</label>
              <input
                type="text"
                placeholder="xxxx-xxxx-xxxx-xxxx"
                value={form.tenantId}
                onChange={(e) => updateForm({ tenantId: e.target.value })}
                disabled={isBusy}
              />
              {tenantIdError && <div className="field-error">{tenantIdError}</div>}
            </div>
          </div>

          {/* Cloud Environment + Auth Method */}
          <div className="form-row">
            <div className="form-field">
              <label>Cloud Environment</label>
              <select
                value={form.cloud}
                onChange={(e) => updateForm({ cloud: e.target.value as CloudEnvironment })}
                disabled={isBusy}
              >
                {Object.values(CloudEnvironment).map((cloud) => (
                  <option key={cloud} value={cloud}>{cloud}</option>
                ))}
              </select>
            </div>
            <div className="form-field">
              <label>Auth Method</label>
              <select
                value={form.authMethod}
                onChange={(e) => updateForm({ authMethod: e.target.value as AuthMethod })}
                disabled={isBusy}
              >
                {Object.values(AuthMethod).map((method) => (
                  <option key={method} value={method}>{method}</option>
                ))}
              </select>
            </div>
          </div>

          {/* Client ID + Client Secret */}
          <div className="form-row">
            <div className="form-field">
              <label>Client ID (App Reg)</label>
              <input
                type="text"
                placeholder="xxxx-xxxx-xxxx-xxxx"
                value={form.clientId}
                onChange={(e) => updateForm({ clientId: e.target.value })}
                disabled={isBusy}
              />
              {clientIdError && <div className="field-error">{clientIdError}</div>}
            </div>
            {form.authMethod === AuthMethod.ClientSecret && (
              <div className="form-field">
                <label>Client Secret</label>
                <input
                  type="password"
                  placeholder="Enter secret value"
                  value={form.clientSecret}
                  onChange={(e) => updateForm({ clientSecret: e.target.value })}
                  disabled={isBusy}
                />
              </div>
            )}
          </div>
        </div>

        {/* Action buttons */}
        <div className="action-buttons">
          <button
            className="btn-connect"
            onClick={() => void connect()}
            disabled={!canConnect}
          >
            Connect
          </button>
          <button
            className="btn-save"
            onClick={() => void saveProfile()}
            disabled={!canSave}
          >
            Save Profile
          </button>
        </div>

        {/* Device code */}
        <DeviceCodePanel />

        {/* Status */}
        {isBusy && statusText && (
          <div className="status-message">{statusText}</div>
        )}

        {/* Progress */}
        {isBusy && (
          <div className="progress-bar">
            <div className="progress-bar-fill" />
          </div>
        )}

        {/* Error */}
        {errorMessage && (
          <div className="error-banner">{errorMessage}</div>
        )}
      </div>
    </div>
  );
}
