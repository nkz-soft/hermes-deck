// Application container: bootstraps the Telegram launch, authenticates against the Hermes API,
// resolves the initial conversation (from a deep link or a fresh conversation), wires the SSE
// stream into the chat store, and renders the chat view. Network concerns live here; ChatView stays
// presentational.

import { useEffect, useRef, useState } from 'react'
import { ChatView } from './features/chat/ChatView'
import { createChatStore, type ChatStore } from './features/chat/chatStore'
import { HermesApiClient } from './services/hermesApi'
import { EventStreamClient } from './services/eventStream'
import { getLaunchContext, signalReady } from './services/telegramLaunch'
import { parseStartParam } from './app/routes'

type Phase = 'loading' | 'ready' | 'error' | 'outside'

const API_BASE: string =
  (import.meta.env.VITE_API_BASE_URL as string | undefined) ?? ''

function App() {
  const storeRef = useRef<ChatStore>(undefined)
  if (!storeRef.current) {
    storeRef.current = createChatStore()
  }
  const store = storeRef.current

  const apiRef = useRef<HermesApiClient>(undefined)
  const streamRef = useRef<EventStreamClient>(undefined)
  const [phase, setPhase] = useState<Phase>('loading')
  const [conversationId, setConversationId] = useState<string>()

  useEffect(() => {
    const ctx = getLaunchContext()
    if (!ctx.isTelegram) {
      setPhase('outside')
      return
    }
    signalReady()

    const api = new HermesApiClient(API_BASE)
    apiRef.current = api
    let cancelled = false

    void (async () => {
      try {
        const session = await api.authenticate(ctx.initData)
        const target = parseStartParam(ctx.startParam)
        const convId =
          target.type === 'conversation'
            ? target.id
            : (await api.createConversation()).conversationId
        if (cancelled) {
          return
        }

        setConversationId(convId)
        const stream = new EventStreamClient(API_BASE, session.sessionToken)
        streamRef.current = stream
        stream.connect(convId, {
          onDelta: (e) => store.applyDelta(e),
          onCompleted: (e) => store.applyCompleted(e),
          onRunStatus: (e) => store.applyRunStatus(e),
        })
        setPhase('ready')
      } catch {
        if (!cancelled) {
          setPhase('error')
        }
      }
    })()

    return () => {
      cancelled = true
      streamRef.current?.disconnect()
    }
  }, [store])

  async function handleSend(content: string): Promise<void> {
    const api = apiRef.current
    if (!api || !conversationId) {
      return
    }
    store.addUserMessage(`local_${Date.now()}`, content)
    const accepted = await api.sendMessage(conversationId, content)
    store.startRun(accepted.runId)
  }

  return (
    <main className="app">
      <h1>Hermes Deck</h1>
      {phase === 'loading' && <p role="status">Connecting…</p>}
      {phase === 'outside' && (
        <p>Open this app from Telegram to control Hermes.</p>
      )}
      {phase === 'error' && (
        <p role="alert">Could not start a Hermes session. Please reopen from Telegram.</p>
      )}
      {phase === 'ready' && <ChatView store={store} onSend={handleSend} />}
    </main>
  )
}

export default App
