import type { BridgeCommand, BridgeMessage } from './types';

interface PendingRequest {
  resolve: (value: unknown) => void;
  reject: (reason: Error) => void;
  timer: ReturnType<typeof setTimeout>;
}

const pendingRequests = new Map<string, PendingRequest>();
const eventListeners = new Map<string, Set<(payload: unknown) => void>>();

const DEFAULT_TIMEOUT = 10_000;
const AUTH_TIMEOUT = 120_000;
const DEV_WS_URL = 'ws://localhost:5100/ws/';

function isWebView2Available(): boolean {
  return !!window.chrome?.webview;
}

// ── WebSocket dev transport ─────────────────────────────────────────
let devSocket: WebSocket | null = null;
let devSocketReady: Promise<void> | null = null;

function getDevSocket(): Promise<WebSocket> {
  if (devSocket && devSocket.readyState === WebSocket.OPEN) {
    return Promise.resolve(devSocket);
  }

  if (devSocketReady) return devSocketReady.then(() => devSocket!);

  devSocketReady = new Promise<void>((resolve, reject) => {
    const ws = new WebSocket(DEV_WS_URL);

    ws.onopen = () => {
      devSocket = ws;
      devSocketReady = null;
      console.info('[Bridge] Connected to dev WebSocket at', DEV_WS_URL);
      resolve();
    };

    ws.onmessage = (e) => {
      handleMessage({ data: e.data } as MessageEvent);
    };

    ws.onclose = () => {
      devSocket = null;
      devSocketReady = null;
    };

    ws.onerror = () => {
      devSocketReady = null;
      reject(new Error('Dev WebSocket connection failed — is the .NET host running?'));
    };
  });

  return devSocketReady.then(() => devSocket!);
}

// ── Public API ──────────────────────────────────────────────────────

export function sendCommand<T = unknown>(command: string, payload?: unknown): Promise<T> {
  const id = crypto.randomUUID();
  const timeout = command.startsWith('auth.') ? AUTH_TIMEOUT : DEFAULT_TIMEOUT;
  const msg: BridgeCommand = { protocol: 'ic/1', id, command, payload };

  return new Promise<T>((resolve, reject) => {
    const timer = setTimeout(() => {
      pendingRequests.delete(id);
      reject(new Error(`Bridge command "${command}" timed out after ${timeout}ms`));
    }, timeout);

    pendingRequests.set(id, {
      resolve: resolve as (value: unknown) => void,
      reject,
      timer,
    });

    if (isWebView2Available()) {
      window.chrome!.webview!.postMessage(msg);
    } else {
      // Dev mode: send via WebSocket
      getDevSocket()
        .then((ws) => ws.send(JSON.stringify(msg)))
        .catch((err) => {
          clearTimeout(timer);
          pendingRequests.delete(id);
          reject(err);
        });
    }
  });
}

export function onEvent(event: string, callback: (payload: unknown) => void): () => void {
  let listeners = eventListeners.get(event);
  if (!listeners) {
    listeners = new Set();
    eventListeners.set(event, listeners);
  }
  listeners.add(callback);

  return () => {
    listeners!.delete(callback);
    if (listeners!.size === 0) {
      eventListeners.delete(event);
    }
  };
}

// ── Message handler (shared by WebView2 and WebSocket) ──────────────

function handleMessage(e: MessageEvent) {
  let msg: BridgeMessage;
  try {
    msg = typeof e.data === 'string' ? JSON.parse(e.data) : e.data;
  } catch {
    return;
  }

  if (msg.protocol !== 'ic/1') return;

  if (msg.type === 'response') {
    const pending = pendingRequests.get(msg.id);
    if (pending) {
      clearTimeout(pending.timer);
      pendingRequests.delete(msg.id);

      if (msg.success) {
        pending.resolve(msg.payload);
      } else {
        pending.reject(new Error(msg.error ?? 'Unknown bridge error'));
      }
    }
  } else if (msg.type === 'event') {
    const listeners = eventListeners.get(msg.event);
    if (listeners) {
      for (const cb of listeners) {
        try {
          cb(msg.payload);
        } catch (err) {
          console.error(`[Bridge] Event listener error for "${msg.event}":`, err);
        }
      }
    }
  }
}

// ── Initialize ──────────────────────────────────────────────────────

if (isWebView2Available()) {
  window.chrome!.webview!.addEventListener('message', handleMessage);
} else {
  // Pre-connect the dev WebSocket so the first command doesn't wait
  getDevSocket().catch(() =>
    console.warn('[Bridge] Dev WebSocket not available — .NET host may not be running')
  );
}
