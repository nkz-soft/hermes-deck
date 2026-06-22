import { defineConfig } from '@playwright/test'

// US1 e2e tests live alongside this config (e.g. chat-flow.spec.ts). The web app is started
// automatically via the Vite dev server; tests mock the Hermes API at the network layer.
export default defineConfig({
  testDir: '.',
  testMatch: '**/*.spec.ts',
  use: {
    baseURL: 'http://localhost:5173',
  },
  webServer: {
    command: 'npm run dev -- --port 5173 --strictPort',
    cwd: '../../../src/HermesDeck.Web',
    url: 'http://localhost:5173',
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
  },
})
