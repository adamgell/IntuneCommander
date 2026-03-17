export interface GroupListItem {
  id: string;
  displayName: string;
  description?: string;
  groupType: 'Dynamic Device' | 'Dynamic User' | 'Assigned' | 'Dynamic';
  membershipRule?: string;
  memberCount: number;
  mail?: string;
  createdDateTime: string;
}

export interface GroupDetail {
  id: string;
  displayName: string;
  description?: string;
  groupType: string;
  membershipRule?: string;
  membershipRuleProcessingState?: string;
  mailEnabled: boolean;
  securityEnabled: boolean;
  mail?: string;
  createdDateTime: string;
  memberCounts: GroupMemberCounts;
  members: GroupMemberInfo[];
  assignments: GroupAssignedObject[];
}

export interface GroupMemberCounts {
  users: number;
  devices: number;
  nestedGroups: number;
  total: number;
}

export interface GroupMemberInfo {
  memberType: string;
  displayName: string;
  secondaryInfo: string;
  tertiaryInfo: string;
  status: string;
  id: string;
}

export interface GroupAssignedObject {
  objectId: string;
  displayName: string;
  objectType: string;
  category: string;
  platform: string;
  assignmentIntent: string;
  isExclusion: boolean;
}
