export interface PolicyListItem {
  id: string;
  name: string;
  description?: string;
  platform: string;
  profileType: string;
  technologies: string;
  createdDateTime: string;
  lastModified: string;
  scopeTag: string;
  isAssigned: boolean;
  settingCount: number;
}

export interface PolicyDetail {
  id: string;
  name: string;
  description?: string;
  platform: string;
  profileType: string;
  technologies: string;
  createdDateTime: string;
  lastModifiedDateTime: string;
  scopeTags: string[];
  settingCount: number;
  isAssigned: boolean;
  templateReference?: string;
  assignments: AssignmentData;
  settingGroups: SettingGroup[];
}

export interface AssignmentData {
  included: AssignmentEntry[];
  excluded: AssignmentEntry[];
}

export interface AssignmentEntry {
  groupName: string;
  status: string;
  filter?: string;
  filterMode?: string;
}

export interface SettingGroup {
  name: string;
  settingCount: number;
  settings: SettingEntry[];
}

export interface SettingEntry {
  label: string;
  value: string;
}
