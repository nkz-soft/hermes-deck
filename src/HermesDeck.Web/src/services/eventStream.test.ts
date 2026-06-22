import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { EventStreamClient } from './eventStream'

/** Minimal fake EventSource that lets tests emit named events. */
class FakeEventSource {
  static instances: FakeEventSource[] = []
  url: string
  withCredentials: boolean
  listeners = new Map<string, Set<(ev: MessageEvent) => void>>()
  onerror: ((ev: Event) => void) | null = null
  closed = false

  constructor(url: string, init?: { withCredentials?: boolean }) {
    this.url = url
    this.withCredentials = init?.withCredentials ?? false
    FakeEventSource.instances.push(this)
  }

  addEventListener(type: string, cb: (ev: MessageEvent) => void) {
    if (!this.listeners.has(type)) this.listeners.set(type, new Set())
    this.listeners.get(type)!.add(cb)
  }

  removeEventListener(type: string, cb: (ev: MessageEvent) => void) {
    this.listeners.get(type)?.delete(cb)
  }

  close() {
    this.closed = true
  }

  emit(type: string, data: unknown, id?: string) {
    const ev = { data: JSON.stringify(data), lastEventId: id ?? '' } as MessageEvent
    this.listeners.get(type)?.forEach((cb) => cb(ev))
  }
}

describe('EventStreamClient', () => {
  beforeEach(() => {
    FakeEventSource.instances = []
    vi.stubGlobal('EventSource', FakeEventSource as unknown as typeof EventSource)
  })
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('connects to the stream endpoint with the conversation id and token', () => {
    const client = new EventStreamClient('https://api.test', 'tok_1')
    client.connect('conv_1', {})
    const es = FakeEventSource.instances[0]
    expect(es.url).toContain('https://api.test/events/stream')
    expect(es.url).toContain('conversationId=conv_1')
    expect(es.url).toContain('access_token=tok_1')
  })

  it('dispatches parsed chat.message.delta events', () => {
    const onDelta = vi.fn()
    const client = new EventStreamClient('https://api.test', 'tok')
    client.connect('conv_1', { onDelta })
    FakeEventSource.instances[0].emit(
      'chat.message.delta',
      { conversationId: 'conv_1', runId: 'r1', messageId: 'm1', delta: 'Hel' },
      'evt_1',
    )
    expect(onDelta).toHaveBeenCalledWith(
      expect.objectContaining({ messageId: 'm1', delta: 'Hel' }),
    )
  })

  it('dispatches completed and run.status.changed events', () => {
    const onCompleted = vi.fn()
    const onRunStatus = vi.fn()
    const client = new EventStreamClient('https://api.test', 'tok')
    client.connect('conv_1', { onCompleted, onRunStatus })
    const es = FakeEventSource.instances[0]
    es.emit('chat.message.completed', { conversationId: 'conv_1', runId: 'r1', messageId: 'm1' }, 'evt_2')
    es.emit('run.status.changed', { runId: 'r1', status: 'completed' }, 'evt_3')
    expect(onCompleted).toHaveBeenCalledWith(expect.objectContaining({ messageId: 'm1' }))
    expect(onRunStatus).toHaveBeenCalledWith(expect.objectContaining({ status: 'completed' }))
  })

  it('dedupes events by id', () => {
    const onDelta = vi.fn()
    const client = new EventStreamClient('https://api.test', 'tok')
    client.connect('conv_1', { onDelta })
    const es = FakeEventSource.instances[0]
    const payload = { conversationId: 'conv_1', runId: 'r1', messageId: 'm1', delta: 'A' }
    es.emit('chat.message.delta', payload, 'evt_dup')
    es.emit('chat.message.delta', payload, 'evt_dup')
    expect(onDelta).toHaveBeenCalledTimes(1)
  })

  it('closes the underlying EventSource on disconnect', () => {
    const client = new EventStreamClient('https://api.test', 'tok')
    client.connect('conv_1', {})
    const es = FakeEventSource.instances[0]
    client.disconnect()
    expect(es.closed).toBe(true)
  })
})
