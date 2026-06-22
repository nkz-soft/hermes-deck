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
import { parseStartParam, type InitialTarget } from './app/routes'

type Phase = 'loading' | 'ready' | 'error' | 'outside'

const API_BASE: string =
  (import.meta.env.VITE_API_BASE_URL as string | undefined) ?? ''

/**
 * Resolves the conversation to open from a launch target. A conversation deep link opens directly;
 * a run deep link resolves to its owning conversation; anything else (or no deep link) starts a
 * fresh conversation. Backend authorization still gates every underlying call.
 */
async function resolveConversationId(
  api: HermesApiClient,
  target: InitialTarget,
): Promise<string> {
  if (target.type === 'conversation') {
    return target.id
  }
  if (target.type === 'run') {
    return (await api.getRun(target.id)).conversationId
  }
  return (await api.createConversation()).conversationId
}

function App() {
  const storeRef = useRef<ChatStore>(undefined)
  if (!storeRef.current) {
    storeRef.current = createChatStore()
  }
  const store = storeRef.current

  const apiRef = useRef<HermesApiClient>(undefined)
  const [phase, setPhase] = useState<Phase>('loading')
  const [conversationId, setConversationId] = useState<string>()
  const [streamInterrupted, setStreamInterrupted] = useState(false)
  const [sendFailed, setSendFailed] = useState(false)

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
    // Bound to this effect invocation so StrictMode's double-invoke (or an early unmount) always
    // closes the exact EventSource this pass opened, never leaking the other pass's connection.
    let stream: EventStreamClient | undefined

    void (async () => {
      try {
        const session = await api.authenticate(ctx.initData)
        const target = parseStartParam(ctx.startParam)
        const convId = await resolveConversationId(api, target)
        if (cancelled) {
          return
        }

        setConversationId(convId)
        stream = new EventStreamClient(API_BASE, session.sessionToken)
        stream.connect(convId, {
          onDelta: (e) => {
            setStreamInterrupted(false)
            store.applyDelta(e)
          },
          onCompleted: (e) => store.applyCompleted(e),
          onRunStatus: (e) => store.applyRunStatus(e),
          onError: () => setStreamInterrupted(true),
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
      stream?.disconnect()
    }
  }, [store])

  async function handleSend(content: string): Promise<void> {
    const api = apiRef.current
    if (!api || !conversationId) {
      return
    }
    setSendFailed(false)
    store.addUserMessage(`local_${Date.now()}`, content)
    try {
      const accepted = await api.sendMessage(conversationId, content)
      store.startRun(accepted.runId)
    } catch {
      setSendFailed(true)
    }
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
      {phase === 'ready' && (
        <>
          {streamInterrupted && <p role="status">Reconnecting…</p>}
          {sendFailed && (
            <p role="alert">Message could not be sent. Please try again.</p>
          )}
          <ChatView store={store} onSend={handleSend} />
        </>
      )}
    </main>
  )
}

export default App
