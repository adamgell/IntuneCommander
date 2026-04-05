import { vi, describe, it, expect, beforeEach } from 'vitest';
import type { ExportResult, ImportPreview, ImportResult } from '../../types/exportImport';

const mockSendCommand = vi.fn();
vi.mock('../../bridge/bridgeClient', () => ({
  sendCommand: mockSendCommand,
  onEvent: vi.fn(() => vi.fn()),
}));

const { useExportImportStore, EXPORT_OBJECT_TYPES } = await import('../exportImportStore');

const initialState = useExportImportStore.getState();

const mockExportResult: ExportResult = { exportedCount: 42, outputPath: 'C:\\export' };
const mockImportPreview: ImportPreview = {
  items: [
    { objectType: 'Device Configurations', name: 'Policy A', fileName: 'Policy A.json' },
    { objectType: 'Compliance Policies', name: 'Policy B', fileName: 'Policy B.json' },
  ],
  totalCount: 2,
  objectTypes: ['Device Configurations', 'Compliance Policies'],
};
const mockImportResult: ImportResult = {
  items: [{ objectType: 'Device Configuration', name: 'Policy A', success: true }],
  successCount: 1,
  failureCount: 0,
};

beforeEach(() => {
  useExportImportStore.setState({ ...initialState });
  vi.clearAllMocks();
});

describe('exportImportStore', () => {
  it('has correct initial state', () => {
    const state = useExportImportStore.getState();
    expect(state.mode).toBe('idle');
    expect(state.exportObjectTypes).toEqual([...EXPORT_OBJECT_TYPES]);
    expect(state.exportPath).toBeNull();
    expect(state.importPath).toBeNull();
    expect(state.error).toBeNull();
  });

  describe('setMode', () => {
    it('switches mode and clears results', () => {
      useExportImportStore.setState({ error: 'old', exportResult: mockExportResult });
      useExportImportStore.getState().setMode('export');

      const state = useExportImportStore.getState();
      expect(state.mode).toBe('export');
      expect(state.error).toBeNull();
      expect(state.exportResult).toBeNull();
    });
  });

  describe('export types', () => {
    it('toggleExportType removes then re-adds', () => {
      const store = useExportImportStore.getState();
      store.toggleExportType('ScopeTags');
      expect(useExportImportStore.getState().exportObjectTypes).not.toContain('ScopeTags');

      useExportImportStore.getState().toggleExportType('ScopeTags');
      expect(useExportImportStore.getState().exportObjectTypes).toContain('ScopeTags');
    });

    it('selectAllExportTypes restores full list', () => {
      useExportImportStore.setState({ exportObjectTypes: ['ScopeTags'] });
      useExportImportStore.getState().selectAllExportTypes();
      expect(useExportImportStore.getState().exportObjectTypes).toEqual([...EXPORT_OBJECT_TYPES]);
    });
  });

  describe('runExport', () => {
    it('does nothing without export path', async () => {
      await useExportImportStore.getState().runExport();
      expect(mockSendCommand).not.toHaveBeenCalled();
    });

    it('exports successfully', async () => {
      useExportImportStore.setState({ exportPath: 'C:\\export' });
      mockSendCommand.mockResolvedValueOnce(mockExportResult);

      await useExportImportStore.getState().runExport();

      const state = useExportImportStore.getState();
      expect(state.exportResult).toEqual(mockExportResult);
      expect(state.isExporting).toBe(false);
      expect(mockSendCommand).toHaveBeenCalledWith('export.run', {
        outputPath: 'C:\\export',
        objectTypes: [...EXPORT_OBJECT_TYPES],
      });
    });

    it('sets error on failure', async () => {
      useExportImportStore.setState({ exportPath: 'C:\\export' });
      mockSendCommand.mockRejectedValueOnce(new Error('Disk full'));

      await useExportImportStore.getState().runExport();
      expect(useExportImportStore.getState().error).toBe('Disk full');
      expect(useExportImportStore.getState().isExporting).toBe(false);
    });
  });

  describe('pickImportFolder', () => {
    it('picks folder and loads preview', async () => {
      mockSendCommand
        .mockResolvedValueOnce('C:\\import')
        .mockResolvedValueOnce(mockImportPreview);

      await useExportImportStore.getState().pickImportFolder();

      const state = useExportImportStore.getState();
      expect(state.importPath).toBe('C:\\import');
      expect(state.importPreview).toEqual(mockImportPreview);
      expect(state.selectedImportTypes).toEqual(['Device Configurations', 'Compliance Policies']);
      expect(state.isPreviewLoading).toBe(false);
    });

    it('does nothing when picker returns empty', async () => {
      mockSendCommand.mockResolvedValueOnce('');
      await useExportImportStore.getState().pickImportFolder();
      expect(useExportImportStore.getState().importPath).toBeNull();
    });
  });

  describe('import types', () => {
    it('toggleImportType toggles', () => {
      useExportImportStore.setState({ selectedImportTypes: ['A', 'B'] });
      useExportImportStore.getState().toggleImportType('A');
      expect(useExportImportStore.getState().selectedImportTypes).toEqual(['B']);

      useExportImportStore.getState().toggleImportType('A');
      expect(useExportImportStore.getState().selectedImportTypes).toContain('A');
    });
  });

  describe('runImport', () => {
    it('does nothing without import path', async () => {
      await useExportImportStore.getState().runImport();
      expect(mockSendCommand).not.toHaveBeenCalled();
    });

    it('imports successfully', async () => {
      useExportImportStore.setState({ importPath: 'C:\\import', selectedImportTypes: ['Device Configurations'] });
      mockSendCommand.mockResolvedValueOnce(mockImportResult);

      await useExportImportStore.getState().runImport();

      expect(useExportImportStore.getState().importResult).toEqual(mockImportResult);
      expect(useExportImportStore.getState().isImporting).toBe(false);
    });
  });

  describe('reset', () => {
    it('returns to initial state', () => {
      useExportImportStore.setState({
        mode: 'export',
        exportPath: '/some/path',
        exportResult: mockExportResult,
        error: 'some error',
      });
      useExportImportStore.getState().reset();

      const state = useExportImportStore.getState();
      expect(state.mode).toBe('idle');
      expect(state.exportPath).toBeNull();
      expect(state.exportResult).toBeNull();
      expect(state.error).toBeNull();
    });
  });
});
