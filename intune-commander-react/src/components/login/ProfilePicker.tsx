import { useAppStore } from '../../store/appStore';

export function ProfilePicker() {
  const profiles = useAppStore((s) => s.profiles);
  const selectedProfileId = useAppStore((s) => s.selectedProfileId);
  const isBusy = useAppStore((s) => s.isBusy);
  const selectProfile = useAppStore((s) => s.selectProfile);
  const resetForm = useAppStore((s) => s.resetForm);
  const deleteProfile = useAppStore((s) => s.deleteProfile);
  const importProfiles = useAppStore((s) => s.importProfiles);

  return (
    <div className="profile-picker">
      <span className="profile-picker-label">Saved Profiles</span>
      <div className="profile-picker-row">
        <select
          value={selectedProfileId ?? ''}
          onChange={(e) => selectProfile(e.target.value || null)}
          disabled={isBusy}
        >
          <option value="">Select a profile or create new...</option>
          {profiles.map((p) => (
            <option key={p.id} value={p.id}>
              {p.name}
            </option>
          ))}
        </select>
        <button
          className="icon-btn"
          onClick={resetForm}
          disabled={isBusy}
          title="New Profile"
          aria-label="New Profile"
        >
          +
        </button>
        <button
          className="icon-btn"
          onClick={() => void importProfiles()}
          disabled={isBusy}
          title="Import Profiles"
          aria-label="Import Profiles"
        >
          &#8615;
        </button>
        <button
          className="icon-btn"
          onClick={() => void deleteProfile()}
          disabled={!selectedProfileId || isBusy}
          title="Delete Profile"
          aria-label="Delete Profile"
        >
          &#128465;
        </button>
      </div>
    </div>
  );
}
