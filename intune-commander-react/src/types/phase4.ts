// Phase 4 types: Device Configs, Compliance, Endpoint Security, Enrollment

// -- Device Configuration --
export interface DeviceConfigListItem {
  id: string;
  displayName: string;
  description?: string;
  configurationType: string;
  createdDateTime: string;
  lastModifiedDateTime: string;
  assignmentCount: number;
}

export interface DeviceConfigDetail {
  id: string;
  displayName: string;
  description?: string;
  configurationType: string;
  createdDateTime: string;
  lastModifiedDateTime: string;
  roleScopeTagIds: string[];
  assignments: Assignment[];
  rawJson: string;
}

// -- Compliance Policy --
export interface CompliancePolicyListItem {
  id: string;
  displayName: string;
  description?: string;
  policyType: string;
  createdDateTime: string;
  lastModifiedDateTime: string;
  assignmentCount: number;
}

export interface CompliancePolicyDetail {
  id: string;
  displayName: string;
  description?: string;
  policyType: string;
  createdDateTime: string;
  lastModifiedDateTime: string;
  roleScopeTagIds: string[];
  assignments: Assignment[];
  rawJson: string;
}

// -- Endpoint Security --
export interface EndpointSecurityListItem {
  id: string;
  displayName: string;
  description?: string;
  intentType: string;
  isAssigned: boolean;
  createdDateTime: string;
  lastModifiedDateTime: string;
  assignmentCount: number;
}

export interface EndpointSecurityDetail {
  id: string;
  displayName: string;
  description?: string;
  intentType: string;
  isAssigned: boolean;
  createdDateTime: string;
  lastModifiedDateTime: string;
  roleScopeTagIds: string[];
  assignments: Assignment[];
  rawJson: string;
}

// -- Enrollment & Autopilot --
export interface EnrollmentConfigListItem {
  id: string;
  displayName: string;
  description?: string;
  configurationType: string;
  priority: number;
  createdDateTime: string;
  lastModifiedDateTime: string;
}

export interface EnrollmentConfigDetail {
  id: string;
  displayName: string;
  description?: string;
  configurationType: string;
  priority: number;
  createdDateTime: string;
  lastModifiedDateTime: string;
  roleScopeTagIds: string[];
  rawJson: string;
}

// Shared
export interface Assignment {
  target: string;
  targetKind: string;
}
