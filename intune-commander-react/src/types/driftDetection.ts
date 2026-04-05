export interface DriftSummary {
  critical: number;
  high: number;
  medium: number;
  low: number;
}

export interface DriftFieldChange {
  path: string;
  baseline: unknown;
  current: unknown;
}

export type DriftSeverity = 'Low' | 'Medium' | 'High' | 'Critical';
export type DriftChangeType = 'Added' | 'Modified' | 'Deleted';

export interface DriftChange {
  objectType: string;
  name: string;
  changeType: DriftChangeType;
  severity: DriftSeverity;
  fields: DriftFieldChange[];
}

export interface DriftReport {
  tenant: string;
  scanTime: string;
  driftDetected: boolean;
  summary: DriftSummary;
  changes: DriftChange[];
}
