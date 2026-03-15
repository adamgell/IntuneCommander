// Assignment Explorer types

export type ReportMode =
  | 'group'
  | 'allPolicies'
  | 'allUsers'
  | 'allDevices'
  | 'unassigned'
  | 'emptyGroups';

export interface AssignmentReportRow {
  policyId: string;
  policyName: string;
  policyType: string;
  platform: string;
  assignmentSummary: string;
  assignmentReason: string;
  groupId: string;
  groupName: string;
  group1Status: string;
  group2Status: string;
  targetDevice: string;
  userPrincipalName: string;
  status: string;
  lastReported: string;
}

export interface GroupSearchResult {
  id: string;
  displayName: string;
  groupType: string;
  membershipRule?: string;
}

export interface AssignmentExplorerQuery {
  mode: ReportMode;
  groupId?: string;
  groupName?: string;
}
