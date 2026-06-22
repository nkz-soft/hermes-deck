import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { RunStatusBadge } from './RunStatusBadge'

describe('RunStatusBadge', () => {
  it('renders nothing when there is no status', () => {
    const { container } = render(<RunStatusBadge />)
    expect(container).toBeEmptyDOMElement()
  })

  it('renders a live status region with a readable label', () => {
    render(<RunStatusBadge status="running" />)
    const badge = screen.getByRole('status')
    expect(badge).toHaveTextContent('Running')
    expect(badge).toHaveAttribute('data-status', 'running')
  })

  it('renders the review-required state with a readable label', () => {
    render(<RunStatusBadge status="review-required" />)
    expect(screen.getByRole('status')).toHaveTextContent('Review required')
  })
})
