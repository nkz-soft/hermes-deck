import { describe, it, expect, afterEach } from 'vitest'
import { getLaunchContext } from './telegramLaunch'

declare global {
  interface Window {
    Telegram?: unknown
  }
}

afterEach(() => {
  delete (window as { Telegram?: unknown }).Telegram
})

describe('getLaunchContext', () => {
  it('returns empty context when not running inside Telegram', () => {
    const ctx = getLaunchContext()
    expect(ctx.initData).toBe('')
    expect(ctx.startParam).toBeUndefined()
    expect(ctx.isTelegram).toBe(false)
  })

  it('reads initData and start_param from the Telegram WebApp', () => {
    ;(window as { Telegram?: unknown }).Telegram = {
      WebApp: {
        initData: 'auth_date=1&hash=abc',
        initDataUnsafe: { start_param: 'task_conversation_conv_123' },
        ready: () => {},
        expand: () => {},
      },
    }

    const ctx = getLaunchContext()
    expect(ctx.initData).toBe('auth_date=1&hash=abc')
    expect(ctx.startParam).toBe('task_conversation_conv_123')
    expect(ctx.isTelegram).toBe(true)
  })

  it('handles a Telegram WebApp without a start_param', () => {
    ;(window as { Telegram?: unknown }).Telegram = {
      WebApp: {
        initData: 'auth_date=2&hash=def',
        initDataUnsafe: {},
        ready: () => {},
        expand: () => {},
      },
    }

    const ctx = getLaunchContext()
    expect(ctx.initData).toBe('auth_date=2&hash=def')
    expect(ctx.startParam).toBeUndefined()
    expect(ctx.isTelegram).toBe(true)
  })
})
