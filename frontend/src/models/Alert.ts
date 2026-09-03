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
}
