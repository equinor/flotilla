export interface BackendAlert {
    alertCode: string
    alertTitle: string
    alertMessage: string
    installationCode: string
    robotId?: string
}

export type AlertSeverity = 'error' | 'warning' | 'info'

export interface Alert {
    title?: string
    message: string
    severity: AlertSeverity
    missionId?: string
    // Marks the banner raised for recently failed mission runs. Dismissing such a banner
    // records a dismissal timestamp so the same failures are not raised again.
    isMissionFailure?: boolean
}
