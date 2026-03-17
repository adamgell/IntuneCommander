import { create } from 'zustand';
import type { TenantProfile, DeviceCodeInfo, ShellState, ProfilesPayload } from '../types/models';
import { CloudEnvironment, AuthMethod } from '../types/models';
import { sendCommand, onEvent } from '../bridge/bridgeClient';

interface FormState {
  id: string;
  name: string;
  tenantId: string;
  clientId: string;
  clientSecret: string;
  cloud: CloudEnvironment;
  authMethod: AuthMethod;
}

const emptyForm: FormState = {
  id: '',
  name: '',
  tenantId: '',
  clientId: '',
  clientSecret: '',
  cloud: CloudEnvironment.Commercial,
  authMethod: AuthMethod.Interactive,
};

interface AppState {
  // .NET-owned state (updated via bridge events)
  profiles: TenantProfile[];
  activeProfileId: string | null;
  isConnected: boolean;
  isBusy: boolean;
  statusText: string;
  errorMessage: string | null;
  activeProfile: TenantProfile | null;
  deviceCode: DeviceCodeInfo | null;

  // Auto-connect state
  isAutoConnecting: boolean;

  // React-owned navigation state
  activePrimaryTab: string;
  activeSecondaryTab: string;
  activeSidebarItem: string | null;

  // React-owned form state
  form: FormState;
  selectedProfileId: string | null;

  // Actions
  setProfiles: (profiles: TenantProfile[], activeProfileId?: string | null) => void;
  setShellState: (state: Partial<ShellState>) => void;
  setDeviceCode: (info: DeviceCodeInfo | null) => void;
  selectProfile: (id: string | null) => void;
  updateForm: (updates: Partial<FormState>) => void;
  resetForm: () => void;

  // Navigation actions
  setPrimaryTab: (tabId: string) => void;
  setSecondaryTab: (tabId: string) => void;
  setSidebarItem: (itemId: string | null) => void;

  // Bridge actions
  loadProfiles: () => Promise<void>;
  saveProfile: () => Promise<void>;
  deleteProfile: () => Promise<void>;
  importProfiles: () => Promise<void>;
  connect: () => Promise<void>;
  disconnect: () => Promise<void>;
}

export const useAppStore = create<AppState>((set, get) => ({
  // Initial state
  profiles: [],
  activeProfileId: null,
  isConnected: false,
  isBusy: false,
  statusText: 'Ready',
  errorMessage: null,
  activeProfile: null,
  deviceCode: null,

  // Auto-connect
  isAutoConnecting: false,

  // Navigation — default to Configuration > Settings Catalog
  activePrimaryTab: 'configuration',
  activeSecondaryTab: 'settings-catalog',
  activeSidebarItem: 'settings-catalog',

  form: { ...emptyForm },
  selectedProfileId: null,

  // State setters
  setProfiles: (profiles, activeProfileId) =>
    set({ profiles, activeProfileId: activeProfileId ?? get().activeProfileId }),

  setShellState: (state) =>
    set({
      isConnected: state.isConnected ?? get().isConnected,
      isBusy: state.isBusy ?? get().isBusy,
      statusText: state.statusText ?? get().statusText,
      errorMessage: state.errorMessage !== undefined ? state.errorMessage : get().errorMessage,
      activeProfile: state.activeProfile !== undefined ? state.activeProfile : get().activeProfile,
    }),

  setDeviceCode: (info) => set({ deviceCode: info }),

  selectProfile: (id) => {
    if (id === null) {
      set({ selectedProfileId: null, form: { ...emptyForm } });
      return;
    }
    const profile = get().profiles.find((p) => p.id === id);
    if (profile) {
      set({
        selectedProfileId: id,
        form: {
          id: profile.id,
          name: profile.name,
          tenantId: profile.tenantId,
          clientId: profile.clientId,
          clientSecret: profile.clientSecret ?? '',
          cloud: profile.cloud,
          authMethod: profile.authMethod,
        },
      });
    }
  },

  updateForm: (updates) => set({ form: { ...get().form, ...updates } }),

  resetForm: () => set({ form: { ...emptyForm }, selectedProfileId: null }),

  // Navigation actions
  setPrimaryTab: (tabId) => {
    const tabs: Record<string, string> = {
      'configuration': 'settings-catalog',
      'applications': 'applications',
      'security': 'security-posture',
      'devices': 'detection-remediation',
      'operations': 'assignment-explorer',
      'admin': 'tenant-admin',
    };
    const defaultSecondary = tabs[tabId] ?? '';
    set({
      activePrimaryTab: tabId,
      activeSecondaryTab: defaultSecondary,
      activeSidebarItem: defaultSecondary,
    });
  },

  setSecondaryTab: (tabId) =>
    set({ activeSecondaryTab: tabId, activeSidebarItem: tabId }),

  setSidebarItem: (itemId) =>
    set({ activeSidebarItem: itemId }),

  // Bridge actions
  loadProfiles: async () => {
    // Guard against StrictMode double-mount causing concurrent auto-connects
    if (get().isAutoConnecting || get().profiles.length > 0) return;
    try {
      const result = await sendCommand<ProfilesPayload>('profiles.load');
      set({ profiles: result.profiles, activeProfileId: result.activeProfileId });

      if (result.activeProfileId) {
        get().selectProfile(result.activeProfileId);

        // Auto-connect with the active profile
        const profile = result.profiles.find((p) => p.id === result.activeProfileId);
        if (profile) {
          set({ isAutoConnecting: true });
          try {
            await sendCommand('auth.connect', profile);
          } catch {
            // Silent fallback — show login screen
            set({ isAutoConnecting: false, errorMessage: null });
          }
        }
      }
    } catch {
      // WebView2 not available — continue with empty profiles
    }
  },

  saveProfile: async () => {
    const { form } = get();
    const profile: TenantProfile = {
      id: form.id || crypto.randomUUID(),
      name: form.name,
      tenantId: form.tenantId,
      clientId: form.clientId,
      clientSecret: form.authMethod === AuthMethod.ClientSecret ? form.clientSecret : undefined,
      cloud: form.cloud,
      authMethod: form.authMethod,
    };

    try {
      const result = await sendCommand<ProfilesPayload>('profiles.save', profile);
      set({ profiles: result.profiles, activeProfileId: result.activeProfileId });
    } catch (err) {
      set({ errorMessage: err instanceof Error ? err.message : 'Failed to save profile' });
    }
  },

  deleteProfile: async () => {
    const { selectedProfileId } = get();
    if (!selectedProfileId) return;

    try {
      const result = await sendCommand<ProfilesPayload>('profiles.delete', { profileId: selectedProfileId });
      set({
        profiles: result.profiles,
        activeProfileId: result.activeProfileId,
        selectedProfileId: null,
        form: { ...emptyForm },
      });
    } catch (err) {
      set({ errorMessage: err instanceof Error ? err.message : 'Failed to delete profile' });
    }
  },

  importProfiles: async () => {
    try {
      await sendCommand('profiles.import');
    } catch (err) {
      set({ errorMessage: err instanceof Error ? err.message : 'Failed to import profiles' });
    }
  },

  connect: async () => {
    const { form } = get();
    const profile: TenantProfile = {
      id: form.id || crypto.randomUUID(),
      name: form.name || `Tenant-${form.tenantId.substring(0, 8)}`,
      tenantId: form.tenantId,
      clientId: form.clientId,
      clientSecret: form.authMethod === AuthMethod.ClientSecret ? form.clientSecret : undefined,
      cloud: form.cloud,
      authMethod: form.authMethod,
    };

    try {
      await sendCommand('auth.connect', profile);
    } catch (err) {
      console.error('[Connect]', err);
    }
  },

  disconnect: async () => {
    try {
      await sendCommand('auth.disconnect');
    } catch (err) {
      console.error('[Disconnect]', err);
    }
  },
}));

// Subscribe to bridge events
function initBridgeEvents() {
  onEvent('state.updated', (payload) => {
    const store = useAppStore.getState();
    const state = payload as ShellState;

    // If auto-connecting and we get a failure, silently fall back to login
    if (store.isAutoConnecting) {
      if (state.isConnected === true) {
        // Auto-connect succeeded
        useAppStore.setState({ isAutoConnecting: false });
      } else if (state.errorMessage) {
        // Auto-connect failed — clear error and show login
        useAppStore.setState({ isAutoConnecting: false });
        state.errorMessage = null;
      }
    }

    store.setShellState(state);
  });

  onEvent('deviceCode.received', (payload) => {
    const store = useAppStore.getState();
    // If auto-connecting and device code is needed, fall back to login screen
    if (store.isAutoConnecting) {
      useAppStore.setState({ isAutoConnecting: false });
    }
    store.setDeviceCode(payload as DeviceCodeInfo);
  });

  onEvent('deviceCode.cleared', () => {
    useAppStore.getState().setDeviceCode(null);
  });

  onEvent('profiles.changed', (payload) => {
    const data = payload as ProfilesPayload;
    useAppStore.getState().setProfiles(data.profiles, data.activeProfileId);
  });
}

initBridgeEvents();
