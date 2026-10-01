export const MEDIA_STREAM_RECOVERY_POLICY = {
    // Bound each connection/recovery wait; repeated interruption events do not extend its deadline.
    recoveryTimeoutMs: 30_000,
    // Reset the attempt counter after this long in a healthy state, as reported by the transport.
    // An interruption cancels the stability window so short-lived recoveries cannot replenish retries.
    stabilityResetMs: 30_000,
    // Initial connection counts as attempt 1. After stability resets the counter to zero,
    // a later outage gets three fresh attempts. Manual retry also resets the counter.
    maxAttempts: 3,
    // Base for exponential backoff: startup retries wait 2s, then 4s.
    retryBaseDelayMs: 2_000,
} as const

/** Count attempts started since the last reset; zero gives a base delay before the first fresh attempt. */
export const calculateMediaStreamRetryDelayMs = (attempts: number): number =>
    MEDIA_STREAM_RECOVERY_POLICY.retryBaseDelayMs * 2 ** Math.max(0, attempts - 1)
