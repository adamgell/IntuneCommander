export interface BridgeCommand {
  protocol: 'ic/1';
  id: string;
  command: string;
  payload?: unknown;
}

export interface BridgeResponse {
  protocol: 'ic/1';
  id: string;
  type: 'response';
  success: boolean;
  payload?: unknown;
  error?: string;
}

export interface BridgeEvent {
  protocol: 'ic/1';
  type: 'event';
  event: string;
  payload: unknown;
}

export type BridgeMessage = BridgeResponse | BridgeEvent;

// WebView2 global type declaration
declare global {
  interface Window {
    chrome?: {
      webview?: {
        postMessage(message: unknown): void;
        addEventListener(type: 'message', listener: (e: MessageEvent) => void): void;
        removeEventListener(type: 'message', listener: (e: MessageEvent) => void): void;
      };
    };
  }
}
