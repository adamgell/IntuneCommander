export interface AppListItem {
  id: string;
  displayName: string;
  description?: string;
  publisher?: string;
  appType: string;
  platform: string;
  createdDateTime: string;
  lastModifiedDateTime: string;
  isAssigned: boolean;
  publishingState: string;
  isFeatured: boolean;
}

export interface AppDetail {
  id: string;
  displayName: string;
  description?: string;
  publisher?: string;
  appType: string;
  platform: string;
  createdDateTime: string;
  lastModifiedDateTime: string;
  isAssigned: boolean;
  publishingState: string;
  isFeatured: boolean;
  developer?: string;
  owner?: string;
  notes?: string;
  version?: string;
  bundleId?: string;
  minimumOsVersion?: string;
  installCommand?: string;
  uninstallCommand?: string;
  installContext?: string;
  sizeMB?: number;
  appStoreUrl?: string;
  assignments: AppAssignmentData;
}

export interface AppAssignmentData {
  required: AppAssignmentEntry[];
  available: AppAssignmentEntry[];
  uninstall: AppAssignmentEntry[];
}

export interface AppAssignmentEntry {
  groupName: string;
  intent: string;
  isExclusion: boolean;
  filter?: string;
  filterMode?: string;
}
