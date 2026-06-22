import { describe, it, expect, afterEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import App from './App'

describe('App', () => {
  afterEach(() => {
    delete (window as { Telegram?: unknown }).Telegram
  })

  it('prompts to open from Telegram when not launched inside the Telegram shell', async () => {
    render(<App />)
    expect(await screen.findByText(/Open this app from Telegram/i)).toBeInTheDocument()
  })
})
