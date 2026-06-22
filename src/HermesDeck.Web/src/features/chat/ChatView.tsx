// T069: Chat view.
//
// Presentational chat surface: subscribes to the chat store (via useSyncExternalStore), renders the
// transcript and the run-status badge, and provides the message composer. Sending is delegated to
// the `onSend` callback supplied by the container, which wires the API call, store updates, and SSE
// subscription — keeping this component free of network concerns and easy to test.

import { useState } from 'react'
import { RunStatusBadge } from '../runs/RunStatusBadge'
import type { ChatStore } from './chatStore'
import { useChatState } from './useChatState'

export interface ChatViewProps {
  store: ChatStore
  onSend: (content: string) => void | Promise<void>
  disabled?: boolean
}

export function ChatView({ store, onSend, disabled = false }: ChatViewProps) {
  const state = useChatState(store)
  const [draft, setDraft] = useState('')

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
    const content = draft.trim()
    if (!content) {
      return
    }
    setDraft('')
    await onSend(content)
  }

  return (
    <section className="chat-view" aria-label="Conversation">
      <header className="chat-view__header">
        <RunStatusBadge status={state.runStatus} />
      </header>

      <ol className="chat-view__messages">
        {state.messages.map((message) => (
          <li
            key={message.messageId}
            className={`chat-message chat-message--${message.role}`}
            data-role={message.role}
            data-streaming={message.streaming}
          >
            {message.content}
          </li>
        ))}
      </ol>

      <form className="chat-view__composer" onSubmit={handleSubmit}>
        <label className="sr-only" htmlFor="chat-input">
          Message
        </label>
        <input
          id="chat-input"
          name="content"
          value={draft}
          onChange={(e) => setDraft(e.target.value)}
          placeholder="Message Hermes"
          autoComplete="off"
          disabled={disabled}
        />
        <button type="submit" disabled={disabled || draft.trim().length === 0}>
          Send
        </button>
      </form>
    </section>
  )
}

export default ChatView
