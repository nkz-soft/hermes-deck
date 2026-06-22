import { describe, it, expect } from 'vitest'
import { parseStartParam } from './routes'

describe('parseStartParam', () => {
  it('returns home when there is no start param', () => {
    expect(parseStartParam(undefined)).toEqual({ type: 'home' })
    expect(parseStartParam('')).toEqual({ type: 'home' })
  })

  it('parses a conversation deep link', () => {
    expect(parseStartParam('task_conversation_conv_123')).toEqual({
      type: 'conversation',
      id: 'conv_123',
      route: '/task/conversation/conv_123',
    })
  })

  it('parses a run deep link', () => {
    expect(parseStartParam('task_run_run_123')).toEqual({
      type: 'run',
      id: 'run_123',
      route: '/task/run/run_123',
    })
  })

  it('parses a panel deep link whose id contains underscores', () => {
    expect(parseStartParam('task_panel_panel_service_health')).toEqual({
      type: 'panel',
      id: 'panel_service_health',
      route: '/task/panel/panel_service_health',
    })
  })

  it('falls back to home for an unknown target type', () => {
    expect(parseStartParam('task_widget_w1')).toEqual({ type: 'home' })
  })

  it('falls back to home for a malformed param', () => {
    expect(parseStartParam('garbage')).toEqual({ type: 'home' })
    expect(parseStartParam('task_conversation')).toEqual({ type: 'home' })
  })
})
