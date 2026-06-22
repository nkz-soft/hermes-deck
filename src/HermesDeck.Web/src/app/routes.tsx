// T071: Deep-link route handling.
//
// Telegram delivers deep links as a flat `start_param` (slashes are not allowed), encoded as
// `task_{targetType}_{targetId}` — the underscore-flattened form of the `/task/{targetType}/{targetId}`
// deep-link contract. This module parses that parameter into an initial in-app target.
//
// Only the target *identity* is derived here; authorization happens server-side (the backend
// validates the session and authorizes the identity before returning any target metadata). An
// unknown or malformed parameter resolves safely to the home view and never reveals protected
// details.

export type DeepLinkTargetType = 'conversation' | 'run' | 'approval' | 'panel'

export type InitialTarget =
  | { type: 'home' }
  | { type: DeepLinkTargetType; id: string; route: string }

const ALLOWED_TARGET_TYPES: readonly DeepLinkTargetType[] = [
  'conversation',
  'run',
  'approval',
  'panel',
]

const HOME: InitialTarget = { type: 'home' }

/**
 * Parses a Telegram `start_param` into an initial target. Returns the home target for a missing,
 * empty, or malformed/unknown parameter.
 */
export function parseStartParam(startParam?: string): InitialTarget {
  if (!startParam) {
    return HOME
  }

  const prefix = 'task_'
  if (!startParam.startsWith(prefix)) {
    return HOME
  }

  const rest = startParam.slice(prefix.length)
  const separatorIndex = rest.indexOf('_')
  if (separatorIndex <= 0) {
    return HOME
  }

  const type = rest.slice(0, separatorIndex)
  const id = rest.slice(separatorIndex + 1)
  if (!id || !ALLOWED_TARGET_TYPES.includes(type as DeepLinkTargetType)) {
    return HOME
  }

  const targetType = type as DeepLinkTargetType
  return { type: targetType, id, route: `/task/${targetType}/${id}` }
}
