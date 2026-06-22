import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { HermesApiClient, HermesApiError } from './hermesApi'

function jsonResponse(status: number, body: unknown): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

describe('HermesApiClient', () => {
  let fetchMock: ReturnType<typeof vi.fn>

  beforeEach(() => {
    fetchMock = vi.fn()
    vi.stubGlobal('fetch', fetchMock)
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('authenticate posts initData and stores the session token', async () => {
    const session = {
      sessionToken: 'tok_123',
      expiresAt: '2026-06-21T00:00:00Z',
      identity: { identityId: 'id_1', displayName: 'Ada', permissions: ['chat'] },
    }
    fetchMock.mockResolvedValueOnce(jsonResponse(200, session))

    const client = new HermesApiClient('https://api.test')
    const result = await client.authenticate('initdata_xyz')

    expect(result).toEqual(session)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe('https://api.test/auth/telegram')
    expect(init.method).toBe('POST')
    expect(JSON.parse(init.body)).toEqual({ initData: 'initdata_xyz' })
    expect(client.isAuthenticated()).toBe(true)
  })

  it('throws HermesApiError without leaking the raw body on 401', async () => {
    fetchMock.mockResolvedValueOnce(
      jsonResponse(401, { error: 'secret server internals' }),
    )
    const client = new HermesApiClient('https://api.test')
    await expect(client.authenticate('bad')).rejects.toMatchObject({
      name: 'HermesApiError',
      status: 401,
    })
    try {
      await client.authenticate('bad2')
    } catch (e) {
      expect((e as HermesApiError).message).not.toContain('secret server internals')
    }
  })

  it('sends the bearer token on protected endpoints', async () => {
    fetchMock.mockResolvedValueOnce(
      jsonResponse(200, {
        sessionToken: 'tok_abc',
        expiresAt: '2026-06-21T00:00:00Z',
        identity: { identityId: 'id', displayName: 'X', permissions: [] },
      }),
    )
    fetchMock.mockResolvedValueOnce(jsonResponse(200, []))

    const client = new HermesApiClient('https://api.test')
    await client.authenticate('init')
    await client.listConversations()

    const [, init] = fetchMock.mock.calls[1]
    expect(init.headers.Authorization).toBe('Bearer tok_abc')
  })

  it('listConversations returns the conversation array', async () => {
    const convos = [{ conversationId: 'c1', status: 'open', updatedAt: '2026-06-21T00:00:00Z' }]
    fetchMock.mockResolvedValueOnce(jsonResponse(200, convos))
    const client = new HermesApiClient('https://api.test')
    client.setSessionToken('tok')
    const result = await client.listConversations()
    expect(result).toEqual(convos)
  })

  it('createConversation posts the title and returns the created conversation', async () => {
    const created = { conversationId: 'c2', title: 'New', status: 'open', updatedAt: '2026-06-21T00:00:00Z' }
    fetchMock.mockResolvedValueOnce(jsonResponse(201, created))
    const client = new HermesApiClient('https://api.test')
    client.setSessionToken('tok')
    const result = await client.createConversation('New')
    expect(result).toEqual(created)
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe('https://api.test/conversations')
    expect(JSON.parse(init.body)).toEqual({ title: 'New' })
  })

  it('sendMessage posts content and returns messageId + runId', async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse(202, { messageId: 'm1', runId: 'r1' }))
    const client = new HermesApiClient('https://api.test')
    client.setSessionToken('tok')
    const result = await client.sendMessage('c1', 'hello')
    expect(result).toEqual({ messageId: 'm1', runId: 'r1' })
    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe('https://api.test/conversations/c1/messages')
    expect(JSON.parse(init.body)).toEqual({ content: 'hello' })
  })

  it('getRun returns the run status', async () => {
    const run = { runId: 'r1', conversationId: 'c1', status: 'completed' }
    fetchMock.mockResolvedValueOnce(jsonResponse(200, run))
    const client = new HermesApiClient('https://api.test')
    client.setSessionToken('tok')
    const result = await client.getRun('r1')
    expect(result).toEqual(run)
  })
})
