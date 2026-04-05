import { create } from 'zustand';
import { sendCommand } from '../bridge/bridgeClient';
import type {
  ExportImportMode,
  ExportResult,
  ImportPreview,
  ImportResult,
} from '../types/exportImport';

const EXPORT_OBJECT_TYPES = [
  'DeviceConfigurations',
  'CompliancePolicies',
  'SettingsCatalog',
  'ScopeTags',
  'RoleDefinitions',
  'TermsAndConditions',
  'IntuneBrandingProfiles',
  'AzureBrandingLocalizations',
  'DeviceHealthScripts',
  'ConditionalAccessPolicies',
] as const;

interface ExportImportState {
  mode: ExportImportMode;
  // Export
  exportPath: string | null;
  exportObjectTypes: string[];
  isExporting: boolean;
  exportResult: ExportResult | null;
  // Import
  importPath: string | null;
  importPreview: ImportPreview | null;
  isPreviewLoading: boolean;
  isImporting: boolean;
  importResult: ImportResult | null;
  selectedImportTypes: string[];
  // Common
  error: string | null;

  setMode: (mode: ExportImportMode) => void;
  pickExportFolder: () => Promise<void>;
  toggleExportType: (type: string) => void;
  selectAllExportTypes: () => void;
  runExport: () => Promise<void>;
  pickImportFolder: () => Promise<void>;
  toggleImportType: (type: string) => void;
  selectAllImportTypes: () => void;
  runImport: () => Promise<void>;
  reset: () => void;
}

export { EXPORT_OBJECT_TYPES };

export const useExportImportStore = create<ExportImportState>((set, get) => ({
  mode: 'idle',
  exportPath: null,
  exportObjectTypes: [...EXPORT_OBJECT_TYPES],
  isExporting: false,
  exportResult: null,
  importPath: null,
  importPreview: null,
  isPreviewLoading: false,
  isImporting: false,
  importResult: null,
  selectedImportTypes: [],
  error: null,

  setMode: (mode) => set({ mode, error: null, exportResult: null, importResult: null }),

  pickExportFolder: async () => {
    try {
      const path = await sendCommand<string>('dialog.pickFolder');
      if (path) set({ exportPath: path });
    } catch (err) {
      set({ error: err instanceof Error ? err.message : 'Failed to pick folder' });
    }
  },

  toggleExportType: (type) => {
    const current = get().exportObjectTypes;
    set({
      exportObjectTypes: current.includes(type)
        ? current.filter((t) => t !== type)
        : [...current, type],
    });
  },

  selectAllExportTypes: () => set({ exportObjectTypes: [...EXPORT_OBJECT_TYPES] }),

  runExport: async () => {
    const { exportPath, exportObjectTypes } = get();
    if (!exportPath) return;

    set({ isExporting: true, error: null, exportResult: null });
    try {
      const result = await sendCommand<ExportResult>('export.run', {
        outputPath: exportPath,
        objectTypes: exportObjectTypes,
      });
      set({ exportResult: result, isExporting: false });
    } catch (err) {
      set({
        isExporting: false,
        error: err instanceof Error ? err.message : 'Export failed',
      });
    }
  },

  pickImportFolder: async () => {
    try {
      const path = await sendCommand<string>('dialog.pickFolder');
      if (!path) return;
      set({ importPath: path, isPreviewLoading: true, error: null, importPreview: null, importResult: null });
      const preview = await sendCommand<ImportPreview>('import.preview', { folderPath: path });
      set({ importPreview: preview, isPreviewLoading: false, selectedImportTypes: preview.objectTypes });
    } catch (err) {
      set({
        isPreviewLoading: false,
        error: err instanceof Error ? err.message : 'Failed to preview import folder',
      });
    }
  },

  toggleImportType: (type) => {
    const current = get().selectedImportTypes;
    set({
      selectedImportTypes: current.includes(type)
        ? current.filter((t) => t !== type)
        : [...current, type],
    });
  },

  selectAllImportTypes: () => {
    const preview = get().importPreview;
    set({ selectedImportTypes: preview?.objectTypes ?? [] });
  },

  runImport: async () => {
    const { importPath, selectedImportTypes } = get();
    if (!importPath) return;

    set({ isImporting: true, error: null, importResult: null });
    try {
      const result = await sendCommand<ImportResult>('import.run', {
        folderPath: importPath,
        objectTypes: selectedImportTypes,
      });
      set({ importResult: result, isImporting: false });
    } catch (err) {
      set({
        isImporting: false,
        error: err instanceof Error ? err.message : 'Import failed',
      });
    }
  },

  reset: () => set({
    mode: 'idle',
    exportPath: null,
    exportObjectTypes: [...EXPORT_OBJECT_TYPES],
    isExporting: false,
    exportResult: null,
    importPath: null,
    importPreview: null,
    isPreviewLoading: false,
    isImporting: false,
    importResult: null,
    selectedImportTypes: [],
    error: null,
  }),
}));
