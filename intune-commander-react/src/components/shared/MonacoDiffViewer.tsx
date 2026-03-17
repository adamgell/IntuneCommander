import { DiffEditor } from '@monaco-editor/react';

export interface MonacoDiffViewerProps {
  /** The original (left-side) content */
  original: string;
  /** The modified (right-side) content */
  modified: string;
  /** Language for syntax highlighting (default: 'json') */
  language?: string;
  /** Minimum height in pixels (default: 400) */
  minHeight?: number;
  /** Whether to show side-by-side or inline diff (default: true = side-by-side) */
  renderSideBySide?: boolean;
  /** Optional height override (default: '100%') */
  height?: string | number;
}

/**
 * Reusable Monaco diff editor component with consistent dark-theme styling.
 * Used by Policy Comparison, Drift Detection, and other workspaces that need diff views.
 */
export function MonacoDiffViewer({
  original,
  modified,
  language = 'json',
  minHeight = 400,
  renderSideBySide = true,
  height,
}: MonacoDiffViewerProps) {
  return (
    <div style={{
      flex: 1,
      border: '1px solid var(--border)',
      borderRadius: 8,
      overflow: 'hidden',
      minHeight,
    }}>
      <DiffEditor
        original={original}
        modified={modified}
        language={language}
        theme="vs-dark"
        height={height}
        options={{
          readOnly: true,
          minimap: { enabled: false },
          scrollBeyondLastLine: false,
          fontSize: 12,
          lineNumbers: 'on',
          renderSideBySide,
          enableSplitViewResizing: true,
          wordWrap: 'on',
          scrollbar: { verticalScrollbarSize: 8 },
          padding: { top: 8, bottom: 8 },
        }}
      />
    </div>
  );
}
