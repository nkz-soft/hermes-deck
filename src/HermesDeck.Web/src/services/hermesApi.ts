// T066: Hermes Deck API client.
//
// Typed `fetch` wrapper that holds the session token and talks to the Hermes API. Non-2xx
// responses raise a `HermesApiError` carrying only the status code — the raw server body is never
// surfaced as user-facing content, in keeping with the no-leak rule.

export interface SessionIdentity {
  identityId: string
  displayName: string
  permissions: string[]
}

export interface SessionResponse {
  sessionToken: string
  expiresAt: string
  identity: SessionIdentity
}

export interface Conversation {
  conversationId: string
  title?: string
  status: string
  updatedAt: string
}

export interface RunAccepted {
  messageId: string
  runId: string
}

export interface AgentRun {
  runId: string
  conversationId: string
  status: string
  reviewReason?: string
}

/** Error thrown for non-2xx API responses. Carries the status but not the raw server body. */
export class HermesApiError extends Error {
  readonly status: number

  constructor(status: number) {
    super(`Hermes API request failed with status ${status}.`)
    this.name = 'HermesApiError'
    this.status = status
  }
}

export class HermesApiClient {
  private readonly baseUrl: string
  private sessionToken?: string

  constructor(baseUrl: string) {
    this.baseUrl = baseUrl.replace(/\/$/, '')
  }

  setSessionToken(token: string): void {
    this.sessionToken = token
  }

  isAuthenticated(): boolean {
    return Boolean(this.sessionToken)
  }

  async authenticate(initData: string): Promise<SessionResponse> {
    const session = await this.request<SessionResponse>('POST', '/auth/telegram', {
      body: { initData },
      authenticated: false,
    })
    this.sessionToken = session.sessionToken
    return session
  }

  listConversations(): Promise<Conversation[]> {
    return this.request<Conversation[]>('GET', '/conversations')
  }

  createConversation(title?: string): Promise<Conversation> {
    return this.request<Conversation>('POST', '/conversations', { body: { title } })
  }

  sendMessage(conversationId: string, content: string): Promise<RunAccepted> {
    return this.request<RunAccepted>(
      'POST',
      `/conversations/${encodeURIComponent(conversationId)}/messages`,
      { body: { content } },
    )
  }

  getRun(runId: string): Promise<AgentRun> {
    return this.request<AgentRun>('GET', `/runs/${encodeURIComponent(runId)}`)
  }

  private async request<T>(
    method: string,
    path: string,
    options: { body?: unknown; authenticated?: boolean } = {},
  ): Promise<T> {
    const { body, authenticated = true } = options
    const headers: Record<string, string> = {}

    if (body !== undefined) {
      headers['Content-Type'] = 'application/json'
    }
    if (authenticated && this.sessionToken) {
      headers.Authorization = `Bearer ${this.sessionToken}`
    }

    const response = await fetch(`${this.baseUrl}${path}`, {
      method,
      headers,
      body: body !== undefined ? JSON.stringify(body) : undefined,
    })

    if (!response.ok) {
      throw new HermesApiError(response.status)
    }

    if (response.status === 204) {
      return undefined as T
    }

    return (await response.json()) as T
  }
}
