import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { ChatView } from './ChatView'
import { createChatStore } from './chatStore'

describe('ChatView', () => {
  it('renders existing messages and the run status', () => {
    const store = createChatStore()
    store.addUserMessage('m1', 'hello there')
    store.startRun('r1')
    store.applyRunStatus({ runId: 'r1', status: 'running' })

    render(<ChatView store={store} onSend={vi.fn()} />)

    expect(screen.getByText('hello there')).toBeInTheDocument()
    expect(screen.getByRole('status')).toHaveTextContent('Running')
  })

  it('renders streamed assistant deltas as they arrive', () => {
    const store = createChatStore()
    store.startRun('r1')
    store.applyDelta({ conversationId: 'c1', runId: 'r1', messageId: 'm_a', delta: 'Hel' })
    store.applyDelta({ conversationId: 'c1', runId: 'r1', messageId: 'm_a', delta: 'lo' })

    render(<ChatView store={store} onSend={vi.fn()} />)

    expect(screen.getByText('Hello')).toBeInTheDocument()
  })

  it('sends a trimmed message and clears the composer', async () => {
    const user = userEvent.setup()
    const onSend = vi.fn()
    const store = createChatStore()

    render(<ChatView store={store} onSend={onSend} />)

    const input = screen.getByLabelText('Message') as HTMLInputElement
    await user.type(input, '  hello  ')
    await user.click(screen.getByRole('button', { name: 'Send' }))

    expect(onSend).toHaveBeenCalledWith('hello')
    expect(input.value).toBe('')
  })

  it('does not send blank messages', async () => {
    const user = userEvent.setup()
    const onSend = vi.fn()
    const store = createChatStore()

    render(<ChatView store={store} onSend={onSend} />)

    // The send button is disabled until there is non-whitespace content.
    expect(screen.getByRole('button', { name: 'Send' })).toBeDisabled()
    await user.type(screen.getByLabelText('Message'), '   ')
    expect(onSend).not.toHaveBeenCalled()
  })
})
