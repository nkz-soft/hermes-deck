// T068: Conversation state store.
//
// Framework-light observable store holding the chat transcript, the streaming-assistant buffer, and
// the active run id/status. It applies SSE events (deltas, completion, run status) and notifies
// subscribers, so it can back a React component via `useSyncExternalStore` and be unit-tested in
// isolation.

import type {
  ChatCompletedEvent,
  ChatDeltaEvent,
  RunStatusEvent,
} from '../../services/eventStream'

export type ChatRole = 'user' | 'assistant'

export interface ChatMessage {
  messageId: string
  role: ChatRole
  content: string
  streaming: boolean
}

export interface ChatState {
  messages: ChatMessage[]
  runId?: string
  runStatus?: string
}

export interface ChatStore {
  getState(): ChatState
  subscribe(listener: () => void): () => void
  addUserMessage(messageId: string, content: string): void
  startRun(runId: string): void
  applyDelta(event: ChatDeltaEvent): void
  applyCompleted(event: ChatCompletedEvent): void
  applyRunStatus(event: RunStatusEvent): void
}

export function createChatStore(): ChatStore {
  let state: ChatState = { messages: [], runId: undefined, runStatus: undefined }
  const listeners = new Set<() => void>()

  function setState(next: ChatState): void {
    state = next
    listeners.forEach((listener) => listener())
  }

  return {
    getState: () => state,

    subscribe(listener) {
      listeners.add(listener)
      return () => {
        listeners.delete(listener)
      }
    },

    addUserMessage(messageId, content) {
      const message: ChatMessage = { messageId, role: 'user', content, streaming: false }
      setState({ ...state, messages: [...state.messages, message] })
    },

    startRun(runId) {
      setState({ ...state, runId, runStatus: 'waiting' })
    },

    applyDelta(event) {
      const existing = state.messages.find((m) => m.messageId === event.messageId)
      let messages: ChatMessage[]
      if (existing) {
        messages = state.messages.map((m) =>
          m.messageId === event.messageId
            ? { ...m, content: m.content + event.delta, streaming: true }
            : m,
        )
      } else {
        messages = [
          ...state.messages,
          { messageId: event.messageId, role: 'assistant', content: event.delta, streaming: true },
        ]
      }
      setState({ ...state, messages })
    },

    applyCompleted(event) {
      const messages = state.messages.map((m) =>
        m.messageId === event.messageId ? { ...m, streaming: false } : m,
      )
      setState({ ...state, messages })
    },

    applyRunStatus(event) {
      if (event.runId !== state.runId) {
        return
      }
      setState({ ...state, runStatus: event.status })
    },
  }
}
