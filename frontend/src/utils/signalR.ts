/**
 * Helpers for working with SignalR event subscriptions.
 *
 * These live outside SignalRContext because that file exports a component, and
 * fast refresh only works when a module exports components alone.
 */

export type UnsubscribeFromEvent = () => void

export const noopUnsubscribe: UnsubscribeFromEvent = () => {}

/**
 * Convenience helper for effects that register several events at once. Returns a
 * single cleanup function that unsubscribes all of them.
 */
export const unsubscribeAll =
    (subscriptions: UnsubscribeFromEvent[]): UnsubscribeFromEvent =>
    () =>
        subscriptions.forEach((unsubscribe) => unsubscribe())
