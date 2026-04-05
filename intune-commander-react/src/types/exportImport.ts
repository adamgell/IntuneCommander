export interface ExportResult {
  exportedCount: number;
  outputPath: string;
}

export interface ImportPreviewItem {
  objectType: string;
  name: string;
  fileName: string;
}

export interface ImportPreview {
  items: ImportPreviewItem[];
  totalCount: number;
  objectTypes: string[];
}

export interface ImportResultItem {
  objectType: string;
  name: string;
  success: boolean;
  error?: string;
}

export interface ImportResult {
  items: ImportResultItem[];
  successCount: number;
  failureCount: number;
}

export type ExportImportMode = 'idle' | 'export' | 'import';
