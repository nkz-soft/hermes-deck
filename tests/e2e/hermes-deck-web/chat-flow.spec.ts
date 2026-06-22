import { test, expect, type Page } from '@playwright/test'

// T053: End-to-end test for the US1 chat flow — Telegram launch, authentication, streamed agent
// progress, message submission, and run status. The Hermes API is mocked at the network layer so
// the test exercises the real frontend wiring without a live backend.

const SESSION = {
  sessionToken: 'tok_e2e',
  expiresAt: '2026-12-31T00:00:00Z',
  identity: { identityId: 'id_e2e', displayName: 'Ada', permissions: ['chat'] },
}

const SSE_BODY = [
  'event: chat.message.delta',
  'id: evt_1',
  'data: {"conversationId":"conv_e2e","runId":"r_e2e","messageId":"m_assistant","delta":"Hello "}',
  '',
  'event: chat.message.delta',
  'id: evt_2',
  'data: {"conversationId":"conv_e2e","runId":"r_e2e","messageId":"m_assistant","delta":"world"}',
  '',
  'event: chat.message.completed',
  'id: evt_3',
  'data: {"conversationId":"conv_e2e","runId":"r_e2e","messageId":"m_assistant"}',
  '',
  '',
].join('\n')

async function mockHermesApi(page: Page): Promise<void> {
  await page.route('**/auth/telegram', (route) =>
    route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(SESSION) }),
  )

  await page.route('**/conversations', (route) => {
    if (route.request().method() === 'POST') {
      return route.fulfill({
        status: 201,
        contentType: 'application/json',
        body: JSON.stringify({
          conversationId: 'conv_e2e',
          title: null,
          status: 'active',
          updatedAt: '2026-06-22T00:00:00Z',
        }),
      })
    }
    return route.fulfill({ status: 200, contentType: 'application/json', body: '[]' })
  })

  await page.route('**/conversations/*/messages', (route) =>
    route.fulfill({
      status: 202,
      contentType: 'application/json',
      body: JSON.stringify({ messageId: 'm_user', runId: 'r_e2e' }),
    }),
  )

  await page.route('**/events/stream**', (route) =>
    route.fulfill({ status: 200, contentType: 'text/event-stream', body: SSE_BODY }),
  )
}

test.beforeEach(async ({ page }) => {
  await mockHermesApi(page)
  // Inject a fake Telegram Mini App shell before any app code runs.
  await page.addInitScript(() => {
    ;(window as unknown as { Telegram: unknown }).Telegram = {
      WebApp: {
        initData: 'auth_date=1&hash=deadbeef',
        initDataUnsafe: {},
        ready: () => {},
        expand: () => {},
      },
    }
  })
})

test('launches, streams agent progress, sends a message, and shows run status', async ({ page }) => {
  await page.goto('/')

  // App launches into the chat view (authenticated + conversation created).
  await expect(page.getByRole('heading', { name: 'Hermes Deck' })).toBeVisible()
  const composer = page.getByLabel('Message')
  await expect(composer).toBeVisible()

  // Streamed agent deltas render as a single assistant message.
  await expect(page.getByText('Hello world')).toBeVisible()

  // Sending a message shows the user's message and starts a run (status badge appears).
  await composer.fill('hi hermes')
  await page.getByRole('button', { name: 'Send' }).click()

  await expect(page.getByText('hi hermes')).toBeVisible()
  // Starting a run surfaces the run-status badge with a concrete lifecycle state.
  await expect(page.locator('[data-status]')).toHaveText(/Waiting|Running|Completed/)
})
