// T065: Telegram Mini App launch bootstrap.
//
// Reads the launch context the Telegram client injects on `window.Telegram.WebApp`.
// None of this data is trusted for authorization: `initData` is forwarded verbatim to the
// backend (`POST /auth/telegram`), which validates its HMAC signature. `startParam` carries the
// optional deep-link target. In a non-Telegram environment (local dev / plain browser) this
// degrades gracefully to an empty, non-Telegram context.

/** The launch context surfaced by the Telegram Mini App shell. */
export interface LaunchContext {
  /** Raw, signed initData query string to forward to the backend for validation. */
  initData: string
  /** Optional deep-link start parameter (e.g. `task_conversation_conv_123`). */
  startParam?: string
  /** Whether the app is running inside the Telegram Mini App shell. */
  isTelegram: boolean
}

interface TelegramWebApp {
  initData?: string
  initDataUnsafe?: { start_param?: string }
  ready?: () => void
  expand?: () => void
}

function getWebApp(): TelegramWebApp | undefined {
  const telegram = (window as { Telegram?: { WebApp?: TelegramWebApp } }).Telegram
  return telegram?.WebApp
}

/**
 * Reads the current Telegram launch context. Safe to call outside Telegram, where it returns an
 * empty, non-Telegram context.
 */
export function getLaunchContext(): LaunchContext {
  const webApp = getWebApp()
  if (!webApp) {
    return { initData: '', startParam: undefined, isTelegram: false }
  }

  const startParam = webApp.initDataUnsafe?.start_param
  return {
    initData: webApp.initData ?? '',
    startParam: startParam ? startParam : undefined,
    isTelegram: true,
  }
}

/** Signals readiness to the Telegram shell and expands the viewport, when available. */
export function signalReady(): void {
  const webApp = getWebApp()
  webApp?.ready?.()
  webApp?.expand?.()
}
