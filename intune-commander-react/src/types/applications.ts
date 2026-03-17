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
  informationUrl?: string;
  privacyInformationUrl?: string;
  version?: string;
  bundleId?: string;
  minimumOsVersion?: string;
  installCommand?: string;
  uninstallCommand?: string;
  installContext?: string;
  sizeMB?: number;
  appStoreUrl?: string;
  categories: string[];
  supersededAppCount: number;
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

export interface ApplicationAssignmentRow {
  id: string;
  appId: string;
  appName: string;
  publisher: string;
  description: string;
  appType: string;
  version: string;
  platform: string;
  bundleId: string;
  packageId: string;
  isFeatured: string;
  createdDate: string;
  lastModified: string;
  assignmentType: string;
  targetName: string;
  targetGroupId: string;
  installIntent: string;
  assignmentSettings: string;
  isExclusion: string;
  appStoreUrl: string;
  privacyUrl: string;
  informationUrl: string;
  minimumOsVersion: string;
  minimumFreeDiskSpaceMB: string;
  minimumMemoryMB: string;
  minimumProcessors: string;
  categories: string;
  notes: string;
}

export interface AppProtectionPolicyListItem {
  id: string;
  displayName: string;
  description?: string;
  policyType: string;
  platform: string;
  version: string;
  createdDateTime: string;
  lastModifiedDateTime: string;
  assignmentCount: number;
}

export interface AppProtectionPolicyDetail {
  id: string;
  displayName: string;
  description?: string;
  policyType: string;
  odataType: string;
  platform: string;
  version: string;
  createdDateTime: string;
  lastModifiedDateTime: string;
  roleScopeTagIds: string[];
  minimumRequiredAppVersion: string;
  minimumRequiredOsVersion: string;
  assignments: ApplicationSectionAssignment[];
}

export interface ManagedDeviceAppConfigurationListItem {
  id: string;
  displayName: string;
  description?: string;
  configurationType: string;
  version: string;
  createdDateTime: string;
  lastModifiedDateTime: string;
  targetedMobileAppCount: number;
  assignmentCount: number;
}

export interface ManagedDeviceAppConfigurationDetail {
  id: string;
  displayName: string;
  description?: string;
  configurationType: string;
  odataType: string;
  version: string;
  createdDateTime: string;
  lastModifiedDateTime: string;
  roleScopeTagIds: string[];
  targetedMobileApps: string[];
  assignments: ApplicationSectionAssignment[];
}

export interface TargetedManagedAppConfigurationListItem {
  id: string;
  displayName: string;
  description?: string;
  configurationType: string;
  version: string;
  appGroupType: string;
  isAssigned: boolean;
  deployedAppCount: number;
  createdDateTime: string;
  lastModifiedDateTime: string;
  assignmentCount: number;
}

export interface TargetedManagedAppConfigurationDetail {
  id: string;
  displayName: string;
  description?: string;
  configurationType: string;
  odataType: string;
  version: string;
  appGroupType: string;
  isAssigned: boolean;
  deployedAppCount: number;
  createdDateTime: string;
  lastModifiedDateTime: string;
  roleScopeTagIds: string[];
  assignments: ApplicationSectionAssignment[];
}

export interface VppTokenListItem {
  id: string;
  displayName: string;
  organizationName: string;
  appleId: string;
  state: string;
  expirationDateTime: string;
  lastSyncDateTime: string;
}

export interface VppTokenDetail {
  id: string;
  displayName: string;
  organizationName: string;
  appleId: string;
  state: string;
  expirationDateTime: string;
  vppTokenAccountType: string;
  lastSyncDateTime: string;
  lastSyncStatus: string;
  countryOrRegion: string;
  locationName: string;
  automaticallyUpdateApps: boolean;
  roleScopeTagIds: string[];
}

export interface ApplicationSectionAssignment {
  target: string;
  targetKind: string;
}
