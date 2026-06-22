import { describe, it, expect } from 'vitest'
import { createChatStore } from './chatStore'

describe('chatStore', () => {
  it('starts empty', () => {
    const store = createChatStore()
    const s = store.getState()
    expect(s.messages).toEqual([])
    expect(s.runId).toBeUndefined()
    expect(s.runStatus).toBeUndefined()
  })

  it('adds a user message', () => {
    const store = createChatStore()
    store.addUserMessage('m_user', 'hello there')
    const s = store.getState()
    expect(s.messages).toHaveLength(1)
    expect(s.messages[0]).toMatchObject({ role: 'user', content: 'hello there', messageId: 'm_user' })
  })

  it('tracks the active run', () => {
    const store = createChatStore()
    store.startRun('r1')
    expect(store.getState().runId).toBe('r1')
    expect(store.getState().runStatus).toBe('waiting')
  })

  it('appends streaming deltas into an assistant message', () => {
    const store = createChatStore()
    store.startRun('r1')
    store.applyDelta({ conversationId: 'c1', runId: 'r1', messageId: 'm_a', delta: 'Hel' })
    store.applyDelta({ conversationId: 'c1', runId: 'r1', messageId: 'm_a', delta: 'lo' })
    const s = store.getState()
    const assistant = s.messages.find((m) => m.messageId === 'm_a')
    expect(assistant).toMatchObject({ role: 'assistant', content: 'Hello', streaming: true })
  })

  it('finalizes the assistant message on completed', () => {
    const store = createChatStore()
    store.startRun('r1')
    store.applyDelta({ conversationId: 'c1', runId: 'r1', messageId: 'm_a', delta: 'Done' })
    store.applyCompleted({ conversationId: 'c1', runId: 'r1', messageId: 'm_a' })
    const assistant = store.getState().messages.find((m) => m.messageId === 'm_a')
    expect(assistant).toMatchObject({ content: 'Done', streaming: false })
  })

  it('updates run status on run.status.changed', () => {
    const store = createChatStore()
    store.startRun('r1')
    store.applyRunStatus({ runId: 'r1', status: 'running' })
    expect(store.getState().runStatus).toBe('running')
    store.applyRunStatus({ runId: 'r1', status: 'completed' })
    expect(store.getState().runStatus).toBe('completed')
  })

  it('ignores run status for a different run', () => {
    const store = createChatStore()
    store.startRun('r1')
    store.applyRunStatus({ runId: 'other', status: 'failed' })
    expect(store.getState().runStatus).toBe('waiting')
  })

  it('notifies subscribers on change', () => {
    const store = createChatStore()
    let calls = 0
    const unsub = store.subscribe(() => {
      calls += 1
    })
    store.addUserMessage('m1', 'hi')
    expect(calls).toBe(1)
    unsub()
    store.addUserMessage('m2', 'bye')
    expect(calls).toBe(1)
  })
})
