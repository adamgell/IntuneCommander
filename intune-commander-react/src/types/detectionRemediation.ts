export interface HealthScriptListItem {
  id: string;
  displayName: string;
  description?: string;
  publisher: string;
  version: string;
  runAsAccount: string;
  runAs32Bit: boolean;
  enforceSignatureCheck: boolean;
  isGlobal: boolean;
  createdDateTime: string;
  lastModified: string;
  deviceHealthScriptType: number;
  hasRemediation: boolean;
  status: string;
  noIssueDetectedCount: number;
  issueDetectedCount: number;
  issueRemediatedCount: number;
  issueReoccurredCount: number;
  totalRemediatedCount: number;
}

export interface HealthScriptDetail {
  id: string;
  displayName: string;
  description?: string;
  publisher: string;
  version: string;
  runAsAccount: string;
  runAs32Bit: boolean;
  enforceSignatureCheck: boolean;
  isGlobal: boolean;
  createdDateTime: string;
  lastModifiedDateTime: string;
  roleScopeTagIds: string[];
  detectionScript: string;
  remediationScript: string;
  runSummary: RunSummary | null;
  deviceRunStates: DeviceRunState[];
  assignments: HealthScriptAssignment[];
}

export interface RunSummary {
  noIssueDetectedCount: number;
  issueDetectedCount: number;
  issueRemediatedCount: number;
  issueReoccurredCount: number;
  errorDeviceCount: number;
  lastScriptRunDateTime: string | null;
}

export interface DeviceRunState {
  deviceName: string;
  detectionState: string;
  remediationState: string;
  lastStateUpdateDateTime: string;
  userPrincipalName?: string;
  preRemediationDetectionScriptOutput?: string;
  preRemediationDetectionScriptError?: string;
  postRemediationDetectionScriptOutput?: string;
  postRemediationDetectionScriptError?: string;
  remediationScriptError?: string;
  expectedStateUpdateDateTime?: string;
  lastSyncDateTime?: string;
}

export interface HealthScriptAssignment {
  target: string;
  targetKind: string;
  schedule: string;
  runRemediation: boolean;
}

export interface DeviceSearchResult {
  id: string;
  deviceName: string;
  operatingSystem?: string;
  osVersion?: string;
  model?: string;
  manufacturer?: string;
  lastSyncDateTime?: string;
}

export interface DeploymentRecord {
  deviceId: string;
  deviceName: string;
  succeeded: boolean;
  errorMessage?: string;
  dispatchedAt: string;
}
