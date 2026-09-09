import { Mission } from 'models/Mission'

export interface Alert {
    alertCode: string
    alertTitle: string
    alertMessage: string
    installationCode: string
    robotId?: string
}

export enum AlertType {
    MissionFail,
    RequestFail,
    DockFail,
    BlockedRobot,
    RequestDock,
    DismissDock,
    DockSuccess,
    AutoScheduleFail,
    InfoAlert,
}

export enum AlertKind {
    FailedMissions = 'failedMissions',
    AutoScheduleFail = 'autoScheduleFail',
    Dock = 'dock',
    RequestFail = 'requestFail',
    Failure = 'failure',
    Info = 'info',
}

export interface AutoScheduleFailedMissionDict {
    [key: string]: string
}

export type AlertContent =
    | { kind: AlertKind.FailedMissions; missions: Mission[] }
    | { kind: AlertKind.AutoScheduleFail; failedMissions: AutoScheduleFailedMissionDict }
    | { kind: AlertKind.Dock; dockType: AlertType }
    | { kind: AlertKind.RequestFail; message: string }
    | { kind: AlertKind.Failure; title: string; message: string }
    | { kind: AlertKind.Info; title: string; message: string }
