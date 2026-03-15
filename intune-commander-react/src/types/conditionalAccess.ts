export interface CaPolicyListItem {
  id: string;
  displayName: string;
  state: string;
  createdDateTime: string;
  modifiedDateTime: string;
  description?: string;
  conditions: CaConditionsSummary;
  grantControls: string[];
  sessionControls: string[];
}

export interface CaConditionsSummary {
  users: string;
  applications: string;
  platforms: string;
  locations: string;
  clientAppTypes: string;
  signInRiskLevels: string;
  userRiskLevels: string;
}

export interface CaPolicyDetail {
  id: string;
  displayName: string;
  state: string;
  createdDateTime: string;
  modifiedDateTime: string;
  description?: string;
  conditions: CaConditionsDetail;
  grantControls: CaGrantControls;
  sessionControls: CaSessionControls;
}

export interface CaConditionsDetail {
  includeUsers: string[];
  excludeUsers: string[];
  includeGroups: string[];
  excludeGroups: string[];
  includeApplications: string[];
  excludeApplications: string[];
  includePlatforms: string[];
  excludePlatforms: string[];
  includeLocations: string[];
  excludeLocations: string[];
  clientAppTypes: string[];
  signInRiskLevels: string[];
  userRiskLevels: string[];
}

export interface CaGrantControls {
  operator: string;
  builtInControls: string[];
  customAuthenticationFactors: string[];
  authenticationStrength?: string;
}

export interface CaSessionControls {
  applicationEnforcedRestrictions: boolean;
  cloudAppSecurity: boolean;
  signInFrequency?: string;
  persistentBrowser?: string;
  disableResilienceDefaults: boolean;
}
