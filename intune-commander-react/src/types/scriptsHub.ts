// Scripts Hub types — unified view of all script types

export type ScriptType = 'powershell' | 'shell' | 'compliance' | 'health';

export interface ScriptListItem {
  id: string;
  displayName: string;
  description?: string;
  scriptType: ScriptType;
  platform: string;
  createdDateTime: string;
  lastModifiedDateTime: string;
  // PowerShell / Shell specific
  runAsAccount?: string;
  runAs32Bit?: boolean;
  enforceSignatureCheck?: boolean;
  // Health script specific
  hasRemediation?: boolean;
  status?: string;
  noIssueDetectedCount?: number;
  issueDetectedCount?: number;
  issueRemediatedCount?: number;
}

export interface ScriptDetail {
  id: string;
  displayName: string;
  description?: string;
  scriptType: ScriptType;
  platform: string;
  createdDateTime: string;
  lastModifiedDateTime: string;
  runAsAccount?: string;
  runAs32Bit?: boolean;
  enforceSignatureCheck?: boolean;
  scriptContent: string;
  remediationScriptContent?: string;
  language: string;
  assignments: ScriptAssignment[];
}

export interface ScriptAssignment {
  target: string;
  targetKind: string;
}
