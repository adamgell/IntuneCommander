import type { AppListItem } from './applications';

export type BulkAssignmentIntent =
  | 'required'
  | 'available'
  | 'availableWithoutEnrollment'
  | 'uninstall';

export type BulkAssignmentTargetType = 'allUsers' | 'allDevices' | 'group';
export type BulkAssignmentFilterMode = 'none' | 'include' | 'exclude';

export interface AssignmentFilterListItem {
  id: string;
  displayName: string;
  description?: string;
  platform: string;
  assignmentFilterManagementType: string;
  rule: string;
}

export interface BulkAssignmentBootstrap {
  apps: AppListItem[];
  assignmentFilters: AssignmentFilterListItem[];
}

export interface BulkAssignmentTargetDraft {
  id: string;
  targetType: BulkAssignmentTargetType;
  targetId?: string;
  displayName: string;
  groupType?: string;
  isExclusion: boolean;
  filterId: string | null;
  filterMode: BulkAssignmentFilterMode;
}

export interface BulkAssignmentApplyTarget {
  targetType: BulkAssignmentTargetType;
  targetId?: string;
  displayName: string;
  isExclusion: boolean;
  filterId?: string;
  filterMode: BulkAssignmentFilterMode;
}

export interface BulkAssignmentAppResult {
  appId: string;
  appName: string;
  success: boolean;
  finalAssignmentCount: number;
  error?: string;
}

export interface BulkAssignmentApplyResult {
  requestedAppCount: number;
  succeededAppCount: number;
  failedAppCount: number;
  results: BulkAssignmentAppResult[];
}
