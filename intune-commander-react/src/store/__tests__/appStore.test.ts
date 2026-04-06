import { vi, describe, it, expect, beforeEach } from 'vitest';
import { CloudEnvironment, AuthMethod } from '../../types/models';

const mockSendCommand = vi.fn();
const mockOnEvent = vi.fn(() => vi.fn());
vi.mock('../../bridge/bridgeClient', () => ({
  sendCommand: mockSendCommand,
  onEvent: mockOnEvent,
}));

const { useAppStore } = await import('../appStore');
const initialState = useAppStore.getState();

const emptyForm = {
  id: '', name: '', tenantId: '', clientId: '', clientSecret: '',
  cloud: CloudEnvironment.Commercial, authMethod: AuthMethod.Interactive,
};

beforeEach(() => {
  useAppStore.setState({ ...initialState, form: { ...emptyForm } });
  vi.clearAllMocks();
});

describe('appStore', () => {
  it('has correct initial state', () => {
    const state = useAppStore.getState();
    expect(state.profiles).toEqual([]);
    expect(state.isConnected).toBe(false);
    expect(state.isBusy).toBe(false);
    expect(state.errorMessage).toBeNull();
    expect(state.activePrimaryTab).toBe('configuration');
    expect(state.activeSidebarItem).toBe('settings-catalog');
  });

  it('subscribes to bridge events at module level', async () => {
    vi.resetModules();
    mockOnEvent.mockClear();
    await import('../appStore');
    const eventNames = mockOnEvent.mock.calls.map(c => c[0]);
    expect(eventNames).toContain('state.updated');
    expect(eventNames).toContain('deviceCode.received');
    expect(eventNames).toContain('deviceCode.cleared');
    expect(eventNames).toContain('profiles.changed');
  });

  describe('setShellState', () => {
    it('updates connection state', () => {
      useAppStore.getState().setShellState({ isConnected: true, statusText: 'Connected' });
      expect(useAppStore.getState().isConnected).toBe(true);
      expect(useAppStore.getState().statusText).toBe('Connected');
    });

    it('preserves unset fields', () => {
      useAppStore.setState({ isConnected: true, statusText: 'Connected' });
      useAppStore.getState().setShellState({ isBusy: true });
      expect(useAppStore.getState().isConnected).toBe(true);
      expect(useAppStore.getState().statusText).toBe('Connected');
    });
  });

  describe('selectProfile', () => {
    it('populates form from profile', () => {
      const profile = {
        id: 'p1', name: 'Test', tenantId: 'tid', clientId: 'cid',
        cloud: CloudEnvironment.GCCHigh, authMethod: AuthMethod.ClientSecret,
        clientSecret: 'secret',
      };
      useAppStore.setState({ profiles: [profile] });
      useAppStore.getState().selectProfile('p1');
      expect(useAppStore.getState().form.tenantId).toBe('tid');
      expect(useAppStore.getState().form.cloud).toBe(CloudEnvironment.GCCHigh);
      expect(useAppStore.getState().selectedProfileId).toBe('p1');
    });

    it('resets form on null', () => {
      useAppStore.setState({ selectedProfileId: 'p1', form: { ...emptyForm, name: 'Test' } });
      useAppStore.getState().selectProfile(null);
      expect(useAppStore.getState().selectedProfileId).toBeNull();
      expect(useAppStore.getState().form.name).toBe('');
    });
  });

  describe('updateForm', () => {
    it('merges partial updates', () => {
      useAppStore.getState().updateForm({ tenantId: 'new-tid' });
      expect(useAppStore.getState().form.tenantId).toBe('new-tid');
      expect(useAppStore.getState().form.cloud).toBe(CloudEnvironment.Commercial);
    });
  });

  describe('resetForm', () => {
    it('clears form and selection', () => {
      useAppStore.setState({ selectedProfileId: 'p1', form: { ...emptyForm, name: 'Filled' } });
      useAppStore.getState().resetForm();
      expect(useAppStore.getState().form.name).toBe('');
      expect(useAppStore.getState().selectedProfileId).toBeNull();
    });
  });

  describe('navigation', () => {
    it('setPrimaryTab sets tab and default sidebar', () => {
      useAppStore.getState().setPrimaryTab('security');
      const state = useAppStore.getState();
      expect(state.activePrimaryTab).toBe('security');
      expect(state.activeSidebarItem).toBe('security-posture');
    });

    it('setSecondaryTab sets both secondary and sidebar', () => {
      useAppStore.getState().setSecondaryTab('compliance');
      expect(useAppStore.getState().activeSecondaryTab).toBe('compliance');
      expect(useAppStore.getState().activeSidebarItem).toBe('compliance');
    });

    it('setSidebarItem sets only sidebar', () => {
      useAppStore.getState().setSidebarItem('custom');
      expect(useAppStore.getState().activeSidebarItem).toBe('custom');
    });
  });

  describe('loadProfiles', () => {
    it('loads profiles and auto-connects', async () => {
      const profiles = [{ id: 'p1', name: 'Test', tenantId: 'tid', clientId: 'cid', cloud: CloudEnvironment.Commercial, authMethod: AuthMethod.Interactive }];
      mockSendCommand
        .mockResolvedValueOnce({ profiles, activeProfileId: 'p1' })
        .mockResolvedValueOnce(undefined);

      await useAppStore.getState().loadProfiles();

      expect(useAppStore.getState().profiles).toEqual(profiles);
      expect(mockSendCommand).toHaveBeenCalledWith('auth.connect', profiles[0]);
    });

    it('skips if already loading', async () => {
      useAppStore.setState({ isAutoConnecting: true });
      await useAppStore.getState().loadProfiles();
      expect(mockSendCommand).not.toHaveBeenCalled();
    });

    it('skips if profiles already loaded', async () => {
      useAppStore.setState({ profiles: [{ id: 'p1' }] as any[] });
      await useAppStore.getState().loadProfiles();
      expect(mockSendCommand).not.toHaveBeenCalled();
    });
  });

  describe('saveProfile', () => {
    it('saves and updates profiles', async () => {
      useAppStore.setState({ form: { ...emptyForm, id: 'p1', name: 'Test', tenantId: 'tid', clientId: 'cid' } });
      const result = { profiles: [{ id: 'p1' }], activeProfileId: 'p1' };
      mockSendCommand.mockResolvedValueOnce(result);

      await useAppStore.getState().saveProfile();

      expect(mockSendCommand).toHaveBeenCalledWith('profiles.save', expect.objectContaining({ name: 'Test' }));
      expect(useAppStore.getState().profiles).toEqual(result.profiles);
    });

    it('sets error on failure', async () => {
      mockSendCommand.mockRejectedValueOnce(new Error('Save failed'));
      await useAppStore.getState().saveProfile();
      expect(useAppStore.getState().errorMessage).toBe('Save failed');
    });
  });

  describe('deleteProfile', () => {
    it('does nothing without selected profile', async () => {
      await useAppStore.getState().deleteProfile();
      expect(mockSendCommand).not.toHaveBeenCalled();
    });

    it('deletes and resets form', async () => {
      useAppStore.setState({ selectedProfileId: 'p1' });
      mockSendCommand.mockResolvedValueOnce({ profiles: [], activeProfileId: null });

      await useAppStore.getState().deleteProfile();

      expect(mockSendCommand).toHaveBeenCalledWith('profiles.delete', { profileId: 'p1' });
      expect(useAppStore.getState().selectedProfileId).toBeNull();
    });
  });

  describe('connect', () => {
    it('sends auth.connect with form data', async () => {
      useAppStore.setState({ form: { ...emptyForm, tenantId: 'tid', clientId: 'cid' } });
      mockSendCommand.mockResolvedValueOnce(undefined);

      await useAppStore.getState().connect();

      expect(mockSendCommand).toHaveBeenCalledWith('auth.connect', expect.objectContaining({ tenantId: 'tid' }));
    });
  });

  describe('disconnect', () => {
    it('sends auth.disconnect', async () => {
      mockSendCommand.mockResolvedValueOnce(undefined);
      await useAppStore.getState().disconnect();
      expect(mockSendCommand).toHaveBeenCalledWith('auth.disconnect');
    });
  });
});
