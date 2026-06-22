// T070: Run status badge.
//
// Renders the current agent-run status (the five lifecycle states) accessibly as a live status
// region so assistive tech announces transitions.

const STATUS_LABELS: Record<string, string> = {
  waiting: 'Waiting',
  running: 'Running',
  'review-required': 'Review required',
  completed: 'Completed',
  failed: 'Failed',
}

export interface RunStatusBadgeProps {
  status?: string
}

export function RunStatusBadge({ status }: RunStatusBadgeProps) {
  if (!status) {
    return null
  }

  const label = STATUS_LABELS[status] ?? status

  return (
    <span role="status" data-status={status} className={`run-status run-status--${status}`}>
      {label}
    </span>
  )
}

export default RunStatusBadge
