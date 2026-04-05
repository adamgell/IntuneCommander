import { useExportImportStore, EXPORT_OBJECT_TYPES } from '../../store/exportImportStore';
import '../../styles/workspace.css';

export function ExportImportWorkspace() {
  const store = useExportImportStore();

  return (
    <div className="workspace">
      <div className="workspace-toolbar">
        <div className="workspace-heading">
          <strong className="workspace-title">Export / Import</strong>
          <div className="workspace-stats">
            <span className="inline-stat" style={{ color: 'var(--text-secondary)' }}>
              {store.mode === 'export' ? 'Export tenant configurations to disk'
                : store.mode === 'import' ? 'Import configurations from disk'
                : 'Choose export or import to get started'}
            </span>
          </div>
        </div>
        <div style={{ display: 'flex', gap: 8 }}>
          <button
            className={`ws-btn${store.mode === 'export' ? ' primary' : ''}`}
            onClick={() => store.setMode('export')}
          >Export</button>
          <button
            className={`ws-btn${store.mode === 'import' ? ' primary' : ''}`}
            onClick={() => store.setMode('import')}
          >Import</button>
        </div>
      </div>

      <div style={{ flex: 1, overflow: 'auto', padding: '0 16px 16px' }}>
        {store.error && (
          <div style={{ color: 'var(--danger, #ef4444)', fontSize: 13, marginBottom: 12, padding: '8px 12px', background: 'var(--danger-bg, rgba(239,68,68,0.1))', borderRadius: 6 }}>
            {store.error}
          </div>
        )}

        {store.mode === 'idle' && (
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100%', color: 'var(--text-tertiary)', fontSize: 14 }}>
            Select Export or Import above to begin.
          </div>
        )}

        {store.mode === 'export' && <ExportPanel />}
        {store.mode === 'import' && <ImportPanel />}
      </div>

      <div className="workspace-footer" />
    </div>
  );
}

function ExportPanel() {
  const { exportPath, exportObjectTypes, isExporting, exportResult, pickExportFolder, toggleExportType, selectAllExportTypes, runExport } = useExportImportStore();

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      {/* Folder Selection */}
      <div>
        <label style={{ fontSize: 11, color: 'var(--text-secondary)', marginBottom: 4, display: 'block' }}>Output Folder</label>
        <div style={{ display: 'flex', gap: 8 }}>
          <button className="ws-btn" onClick={pickExportFolder}>Browse...</button>
          <span style={{ fontSize: 12, color: 'var(--text-tertiary)', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', flex: 1, lineHeight: '32px' }}>
            {exportPath ?? 'No folder selected'}
          </span>
        </div>
      </div>

      {/* Object Type Selection */}
      <div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 8 }}>
          <label style={{ fontSize: 11, color: 'var(--text-secondary)' }}>Object Types to Export</label>
          <button className="ws-btn" style={{ fontSize: 11, padding: '2px 8px' }} onClick={selectAllExportTypes}>Select All</button>
        </div>
        <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6 }}>
          {EXPORT_OBJECT_TYPES.map(type => (
            <label key={type} style={{ display: 'flex', alignItems: 'center', gap: 4, fontSize: 12, cursor: 'pointer', padding: '4px 8px', borderRadius: 4, border: '1px solid var(--border)', background: exportObjectTypes.includes(type) ? 'var(--surface-secondary)' : 'transparent' }}>
              <input
                type="checkbox"
                checked={exportObjectTypes.includes(type)}
                onChange={() => toggleExportType(type)}
                style={{ accentColor: 'var(--brand)' }}
              />
              {type}
            </label>
          ))}
        </div>
      </div>

      {/* Export Button */}
      <div>
        <button
          className="ws-btn primary"
          disabled={!exportPath || exportObjectTypes.length === 0 || isExporting}
          onClick={runExport}
        >
          {isExporting ? 'Exporting...' : 'Run Export'}
        </button>
      </div>

      {/* Export Result */}
      {exportResult && (
        <div style={{ padding: '12px 16px', borderRadius: 6, border: '1px solid var(--success, #22c55e)', background: 'rgba(34,197,94,0.08)' }}>
          <strong style={{ color: 'var(--success, #22c55e)', fontSize: 13 }}>Export Complete</strong>
          <div style={{ fontSize: 12, color: 'var(--text-secondary)', marginTop: 4 }}>
            {exportResult.exportedCount} objects exported to {exportResult.outputPath}
          </div>
        </div>
      )}
    </div>
  );
}

function ImportPanel() {
  const { importPath, importPreview, isPreviewLoading, selectedImportTypes, isImporting, importResult, pickImportFolder, toggleImportType, selectAllImportTypes, runImport } = useExportImportStore();

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      {/* Folder Selection */}
      <div>
        <label style={{ fontSize: 11, color: 'var(--text-secondary)', marginBottom: 4, display: 'block' }}>Import Folder</label>
        <div style={{ display: 'flex', gap: 8 }}>
          <button className="ws-btn" onClick={pickImportFolder}>
            {isPreviewLoading ? 'Scanning...' : 'Browse...'}
          </button>
          <span style={{ fontSize: 12, color: 'var(--text-tertiary)', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap', flex: 1, lineHeight: '32px' }}>
            {importPath ?? 'No folder selected'}
          </span>
        </div>
      </div>

      {/* Preview */}
      {importPreview && (
        <>
          <div>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 8 }}>
              <label style={{ fontSize: 11, color: 'var(--text-secondary)' }}>
                {importPreview.totalCount} objects found in {importPreview.objectTypes.length} categories
              </label>
              <button className="ws-btn" style={{ fontSize: 11, padding: '2px 8px' }} onClick={selectAllImportTypes}>Select All</button>
            </div>
            <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6 }}>
              {importPreview.objectTypes.map(type => {
                const count = importPreview.items.filter(i => i.objectType === type).length;
                return (
                  <label key={type} style={{ display: 'flex', alignItems: 'center', gap: 4, fontSize: 12, cursor: 'pointer', padding: '4px 8px', borderRadius: 4, border: '1px solid var(--border)', background: selectedImportTypes.includes(type) ? 'var(--surface-secondary)' : 'transparent' }}>
                    <input
                      type="checkbox"
                      checked={selectedImportTypes.includes(type)}
                      onChange={() => toggleImportType(type)}
                      style={{ accentColor: 'var(--brand)' }}
                    />
                    {type} ({count})
                  </label>
                );
              })}
            </div>
          </div>

          {/* Preview Table */}
          <div style={{ maxHeight: 300, overflow: 'auto', border: '1px solid var(--border)', borderRadius: 6 }}>
            <table style={{ width: '100%', fontSize: 12, borderCollapse: 'collapse' }}>
              <thead>
                <tr style={{ background: 'var(--surface-secondary)', position: 'sticky', top: 0 }}>
                  <th style={{ textAlign: 'left', padding: '6px 12px', borderBottom: '1px solid var(--border)' }}>Type</th>
                  <th style={{ textAlign: 'left', padding: '6px 12px', borderBottom: '1px solid var(--border)' }}>Name</th>
                  <th style={{ textAlign: 'left', padding: '6px 12px', borderBottom: '1px solid var(--border)' }}>File</th>
                </tr>
              </thead>
              <tbody>
                {importPreview.items
                  .filter(i => selectedImportTypes.includes(i.objectType))
                  .map((item, i) => (
                  <tr key={i} style={{ borderBottom: '1px solid var(--border)' }}>
                    <td style={{ padding: '4px 12px', color: 'var(--text-secondary)' }}>{item.objectType}</td>
                    <td style={{ padding: '4px 12px' }}>{item.name}</td>
                    <td style={{ padding: '4px 12px', color: 'var(--text-tertiary)', fontFamily: 'monospace', fontSize: 11 }}>{item.fileName}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Import Button */}
          <div>
            <button
              className="ws-btn primary"
              disabled={selectedImportTypes.length === 0 || isImporting}
              onClick={runImport}
              style={{ background: 'var(--warning, #f59e0b)' }}
            >
              {isImporting ? 'Importing...' : `Import ${importPreview.items.filter(i => selectedImportTypes.includes(i.objectType)).length} Objects`}
            </button>
          </div>
        </>
      )}

      {/* Import Result */}
      {importResult && (
        <div style={{ border: '1px solid var(--border)', borderRadius: 6, overflow: 'hidden' }}>
          <div style={{ padding: '8px 12px', background: 'var(--surface-secondary)', display: 'flex', gap: 16 }}>
            <span style={{ color: 'var(--success, #22c55e)', fontSize: 13, fontWeight: 600 }}>
              {importResult.successCount} succeeded
            </span>
            {importResult.failureCount > 0 && (
              <span style={{ color: 'var(--danger, #ef4444)', fontSize: 13, fontWeight: 600 }}>
                {importResult.failureCount} failed
              </span>
            )}
          </div>
          <div style={{ maxHeight: 200, overflow: 'auto' }}>
            {importResult.items.map((item, i) => (
              <div key={i} style={{ display: 'flex', gap: 8, padding: '4px 12px', borderTop: '1px solid var(--border)', fontSize: 12 }}>
                <span style={{ color: item.success ? 'var(--success)' : 'var(--danger)', width: 16 }}>
                  {item.success ? '\u2713' : '\u2717'}
                </span>
                <span style={{ flex: 1 }}>{item.name}</span>
                <span style={{ color: 'var(--text-tertiary)' }}>{item.objectType}</span>
                {item.error && <span style={{ color: 'var(--danger)', fontSize: 11 }}>{item.error}</span>}
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
