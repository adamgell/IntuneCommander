// Security Posture Dashboard types

export interface SecurityPostureSummary {
  // Conditional Access
  caEnabled: number;
  caReportOnly: number;
  caDisabled: number;
  caTotal: number;

  // Compliance
  compliancePolicies: number;
  compliancePlatforms: string[];

  // Endpoint Security
  endpointSecurityIntents: number;

  // App Protection
  appProtectionPolicies: number;

  // Authentication Strength
  authStrengthPolicies: number;

  // Named Locations
  namedLocations: number;

  // Computed score (0-100)
  securityScore: number;
  scoreBreakdown: ScoreCategory[];

  // Gap analysis
  gaps: SecurityGap[];
}

export interface ScoreCategory {
  category: string;
  score: number;
  maxScore: number;
  items: string[];
}

export interface SecurityGap {
  severity: 'high' | 'medium' | 'low';
  category: string;
  description: string;
}

export interface CaPolicySummaryItem {
  id: string;
  displayName: string;
  state: string;
}

export interface CompliancePolicySummaryItem {
  id: string;
  displayName: string;
  platform: string;
}

export interface EndpointSecurityItem {
  id: string;
  displayName: string;
  category: string;
}

export interface AppProtectionItem {
  id: string;
  displayName: string;
  platform: string;
}

export interface AuthStrengthItem {
  id: string;
  displayName: string;
  allowedCombinations: string[];
}

export interface NamedLocationItem {
  id: string;
  displayName: string;
  locationType: string;
  isTrusted: boolean;
}

export interface SecurityPostureDetail {
  conditionalAccessPolicies: CaPolicySummaryItem[];
  compliancePolicies: CompliancePolicySummaryItem[];
  endpointSecurityIntents: EndpointSecurityItem[];
  appProtectionPolicies: AppProtectionItem[];
  authStrengthPolicies: AuthStrengthItem[];
  namedLocations: NamedLocationItem[];
}
