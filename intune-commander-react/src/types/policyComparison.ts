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
}
