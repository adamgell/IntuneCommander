import { sendCommand } from './bridgeClient';

export interface DialogResult {
  path: string | null;
  cancelled: boolean;
}

/**
 * Opens a native folder picker dialog.
 * Returns the selected folder path, or null if cancelled.
 */
export async function pickFolder(): Promise<DialogResult> {
  return sendCommand<DialogResult>('dialog.pickFolder');
}

/**
 * Opens a native file open dialog.
 * @param filter - File filter string (e.g., "JSON files (*.json)|*.json|All files (*.*)|*.*")
 * @param title - Dialog title
 */
export async function pickFile(filter?: string, title?: string): Promise<DialogResult> {
  return sendCommand<DialogResult>('dialog.pickFile', { filter, title });
}

/**
 * Opens a native save file dialog.
 * @param filter - File filter string
 * @param title - Dialog title
 * @param defaultFileName - Default file name
 */
export async function saveFile(filter?: string, title?: string, defaultFileName?: string): Promise<DialogResult> {
  return sendCommand<DialogResult>('dialog.saveFile', { filter, title, defaultFileName });
}
