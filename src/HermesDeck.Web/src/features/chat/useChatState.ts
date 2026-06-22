// React binding for the chat store: subscribes a component to store changes via the standard
// useSyncExternalStore contract.

import { useSyncExternalStore } from 'react'
import type { ChatState, ChatStore } from './chatStore'

export function useChatState(store: ChatStore): ChatState {
  return useSyncExternalStore(store.subscribe, store.getState, store.getState)
}
