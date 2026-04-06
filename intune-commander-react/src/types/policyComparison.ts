// Policy Comparison / Diff Workspace types

export type PolicyCategory =
  | 'settingsCatalog'
  | 'compliance'
  | 'deviceConfiguration'
  | 'conditionalAccess'
  | 'appProtection'
  | 'endpointSecurity';

export interface PolicySummaryItem {
  id: string;
  displayName: string;
  category: PolicyCategory;
}

export interface PolicyComparisonResult {
  policyAName: string;
  policyBName: string;
  category: string;
  totalProperties: number;
  differingProperties: number;
  normalizedJsonA: string;
  normalizedJsonB: string;
  settingsDiff?: SettingDiffItem[];
}

export type DiffStatus = 'same' | 'changed' | 'onlyA' | 'onlyB';

export interface SettingDiffItem {
  label: string;
  category: string;
  valueA?: string;
  valueB?: string;
  status: DiffStatus;
}
