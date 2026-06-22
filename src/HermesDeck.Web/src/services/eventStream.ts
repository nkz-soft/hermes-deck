// T067: Server-Sent Events client.
//
// Wraps `EventSource` to consume `GET /events/stream?conversationId=...` and dispatch the parsed
// US1 chat events to handler callbacks. Events are deduped by their SSE id so reconnect replays are
// safe (per the events contract). The session token is passed as an `access_token` query parameter
// because `EventSource` cannot set an `Authorization` header.

export interface ChatDeltaEvent {
  conversationId: string
  runId: string
  messageId: string
  delta: string
}

export interface ChatCompletedEvent {
  conversationId: string
  runId: string
  messageId: string
}

export interface RunStatusEvent {
  runId: string
  status: string
}

export interface EventStreamHandlers {
  onDelta?: (event: ChatDeltaEvent) => void
  onCompleted?: (event: ChatCompletedEvent) => void
  onRunStatus?: (event: RunStatusEvent) => void
  onError?: (event: Event) => void
}

export class EventStreamClient {
  private readonly baseUrl: string
  private readonly token: string
  private source?: EventSource
  private readonly seenEventIds = new Set<string>()

  constructor(baseUrl: string, token: string) {
    this.baseUrl = baseUrl.replace(/\/$/, '')
    this.token = token
  }

  connect(conversationId: string, handlers: EventStreamHandlers): void {
    const url =
      `${this.baseUrl}/events/stream` +
      `?conversationId=${encodeURIComponent(conversationId)}` +
      `&access_token=${encodeURIComponent(this.token)}`

    const source = new EventSource(url, { withCredentials: false })
    this.source = source

    this.listen<ChatDeltaEvent>(source, 'chat.message.delta', handlers.onDelta)
    this.listen<ChatCompletedEvent>(source, 'chat.message.completed', handlers.onCompleted)
    this.listen<RunStatusEvent>(source, 'run.status.changed', handlers.onRunStatus)

    if (handlers.onError) {
      source.onerror = handlers.onError
    }
  }

  disconnect(): void {
    this.source?.close()
    this.source = undefined
  }

  private listen<T>(
    source: EventSource,
    type: string,
    handler?: (event: T) => void,
  ): void {
    source.addEventListener(type, (ev: MessageEvent) => {
      const id = ev.lastEventId
      if (id) {
        if (this.seenEventIds.has(id)) {
          return
        }
        this.seenEventIds.add(id)
      }

      if (!handler) {
        return
      }

      handler(JSON.parse(ev.data) as T)
    })
  }
}
