import { createContext, FC, useContext, useEffect, useState } from 'react'
import { addMinutes, max } from 'date-fns'
import { Mission, MissionStatus } from 'models/Mission'
import { SignalREventLabels, useSignalRContext } from './SignalRContext'
import { Alert } from 'models/Alert'
import { useAssetContext } from './AssetContext'
import { RobotStatus } from 'models/Robot'
import { convertUTCDateToLocalDate } from 'utils/StringFormatting'
import { useLanguageContext } from './LanguageContext'
import { useBackendApi } from 'api/UseBackendApi'
import { AuthContext } from './AuthContext'
import { InstallationContext } from './InstallationContext'
import type { AlertContent } from 'components/Alerts/AlertContent'

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

const alertTypeEnumMap: { [key: string]: AlertType } = {
    DockFailure: AlertType.DockFail,
    generalFailure: AlertType.RequestFail,
    AutoScheduleFail: AlertType.AutoScheduleFail,
    skipAutoMission: AlertType.InfoAlert,
}

interface ActiveAlert {
    content: AlertContent
    dismiss: () => void
}

export type AlertMap = Partial<Record<AlertType, ActiveAlert>>

interface IAlertContext {
    alerts: AlertMap
    raiseAlert: (source: AlertType, content: AlertContent) => void
    clearAlert: (source: AlertType) => void
    clearAlerts: () => void
}

interface Props {
    children: React.ReactNode
}

const defaultAlertInterface: IAlertContext = {
    alerts: {},
    raiseAlert: () => {},
    clearAlert: () => {},
    clearAlerts: () => {},
}

export interface AutoScheduleFailedMissionDict {
    [key: string]: string
}

const autoScheduleStorageKey = 'autoScheduleFailedMissionDict'

const AlertContext = createContext<IAlertContext>(defaultAlertInterface)

export const AlertProvider: FC<Props> = ({ children }) => {
    const [alerts, setAlerts] = useState<AlertMap>({})
    const [recentFailedMissions, setRecentFailedMissions] = useState<Mission[]>([])
    const { registerEvent, connectionReady } = useSignalRContext()
    const { TranslateText } = useLanguageContext()
    const { enabledRobots } = useAssetContext()
    const { installation } = useContext(InstallationContext)
    const [autoScheduleFailedMissionDict, setAutoScheduleFailedMissionDict] = useState<AutoScheduleFailedMissionDict>(
        JSON.parse(window.localStorage.getItem(autoScheduleStorageKey) || '{}')
    )
    const backendApi = useBackendApi()
    const { isAuthenticated } = useContext(AuthContext)

    const pageSize: number = 100
    // The default amount of minutes in the past for failed missions to generate an alert
    const defaultTimeInterval: number = 10
    // The maximum amount of minutes in the past for failed missions to generate an alert
    const maxTimeInterval: number = 60
    const dismissMissionFailTimeKey: string = 'lastMissionFailDismissalTime'

    const applyDismissSideEffects = (source: AlertType) => {
        if (source === AlertType.MissionFail) {
            sessionStorage.setItem(dismissMissionFailTimeKey, JSON.stringify(Date.now()))
            setRecentFailedMissions([])
        }
        if (source === AlertType.AutoScheduleFail) {
            setAutoScheduleFailedMissionDict({})
            window.localStorage.setItem(autoScheduleStorageKey, JSON.stringify({}))
        }
    }

    const clearAlert = (source: AlertType) => {
        applyDismissSideEffects(source)
        setAlerts((oldAlerts) => {
            const newAlerts = { ...oldAlerts }
            delete newAlerts[source]
            return newAlerts
        })
    }

    const raiseAlert = (source: AlertType, content: AlertContent) => {
        setAlerts((oldAlerts) => ({
            ...oldAlerts,
            [source]: { content, dismiss: () => clearAlert(source) },
        }))
    }

    const clearAlerts = () => setAlerts({})

    const getLastDismissalTime = (): Date => {
        const sessionValue = sessionStorage.getItem(dismissMissionFailTimeKey)
        if (sessionValue === null || sessionValue === '') {
            return addMinutes(Date.now(), -defaultTimeInterval)
        } else {
            // If last dismissal time was more than {MaxTimeInterval} minutes ago, use the limit value instead
            return max([addMinutes(Date.now(), -maxTimeInterval), JSON.parse(sessionValue)])
        }
    }

    // Set the initial failed missions when loading the page or changing installations
    useEffect(() => {
        if (!isAuthenticated) return
        const updateRecentFailedMissions = () => {
            const lastDismissTime: Date = getLastDismissalTime()
            backendApi
                .getMissionRuns({
                    installationCode: installation.installationCode,
                    statuses: [MissionStatus.Failed],
                    pageSize: pageSize,
                })
                .then((missions) => {
                    const newRecentFailedMissions = missions.content.filter(
                        (m) =>
                            convertUTCDateToLocalDate(new Date(m.endTime!)) > lastDismissTime &&
                            m.installationCode!.toLocaleLowerCase() ===
                                installation.installationCode.toLocaleLowerCase()
                    )
                    setRecentFailedMissions(newRecentFailedMissions)
                })
                .catch(() => {
                    raiseAlert(AlertType.RequestFail, {
                        kind: 'requestFail',
                        message: TranslateText('Failed to retrieve failed missions'),
                    })
                })
        }
        if (!recentFailedMissions || recentFailedMissions.length === 0) updateRecentFailedMissions()
    }, [installation])

    // Register a signalR event handler that listens for new failed missions
    useEffect(() => {
        if (connectionReady) {
            registerEvent(SignalREventLabels.missionRunFailed, (username: string, message: string) => {
                const newFailedMission: Mission = JSON.parse(message)
                const lastDismissTime: Date = getLastDismissalTime()

                setRecentFailedMissions((failedMissions) => {
                    if (
                        !newFailedMission.installationCode ||
                        newFailedMission.installationCode.toLocaleLowerCase() !==
                            installation.installationCode.toLocaleLowerCase()
                    )
                        return failedMissions // Ignore missions for other installations
                    // Ignore missions shortly after the user dismissed the last one
                    if (convertUTCDateToLocalDate(new Date(newFailedMission.endTime!)) <= lastDismissTime)
                        return failedMissions
                    const isDuplicate = failedMissions.filter((m) => m.id === newFailedMission.id).length > 0
                    if (isDuplicate) return failedMissions // Ignore duplicate failed missions
                    return [...failedMissions, newFailedMission]
                })
            })
        }
    }, [registerEvent, connectionReady, installation])

    useEffect(() => {
        if (connectionReady) {
            registerEvent(SignalREventLabels.alert, (username: string, message: string) => {
                const backendAlert: Alert = JSON.parse(message)
                if (
                    backendAlert.installationCode.toLocaleLowerCase() !==
                    installation.installationCode.toLocaleLowerCase()
                )
                    return

                const alertType = alertTypeEnumMap[backendAlert.alertCode]

                if (backendAlert.robotId !== null && !enabledRobots.filter((r) => r.id === backendAlert.robotId)) return

                if (alertType === AlertType.AutoScheduleFail) {
                    setAutoScheduleFailedMissionDict((oldDict) => {
                        const newDict = { ...oldDict, [backendAlert.alertTitle]: backendAlert.alertMessage }
                        window.localStorage.setItem(autoScheduleStorageKey, JSON.stringify(newDict))
                        return newDict
                    })
                    return
                }

                if (alertType === AlertType.InfoAlert) {
                    raiseAlert(alertType, {
                        kind: 'info',
                        title: backendAlert.alertTitle,
                        message: backendAlert.alertMessage,
                    })
                } else {
                    raiseAlert(alertType, {
                        kind: 'failure',
                        title: backendAlert.alertTitle,
                        message: backendAlert.alertMessage,
                    })
                }
            })
        }
    }, [registerEvent, connectionReady, installation, enabledRobots])

    const robotsWithFrozenQueue = enabledRobots.filter((robot) => robot.status === RobotStatus.Lockdown)

    const getActiveSendToDockAlertType = () => {
        if (robotsWithFrozenQueue.length === 0) return undefined
        else if (robotsWithFrozenQueue.find((robot) => robot.status !== RobotStatus.Home)) return AlertType.RequestDock
        else return AlertType.DockSuccess
    }

    const computedDockAlertType = getActiveSendToDockAlertType()
    const [activeSendToDockAlertType, setActiveSendToDockAlertType] = useState<AlertType | undefined>(
        computedDockAlertType
    )
    const [prevComputedDockAlertType, setPrevComputedDockAlertType] = useState<AlertType | undefined>(
        computedDockAlertType
    )
    const [dismissedDockAlertType, setDismissedDockAlertType] = useState<AlertType | undefined>(undefined)

    if (computedDockAlertType !== prevComputedDockAlertType) {
        setPrevComputedDockAlertType(computedDockAlertType)
        setDismissedDockAlertType(undefined)
        setActiveSendToDockAlertType((current) => {
            if (current === computedDockAlertType) return current
            if (current !== undefined && computedDockAlertType === undefined) return AlertType.DismissDock
            return computedDockAlertType
        })
    }

    const showDockAlert =
        activeSendToDockAlertType !== undefined && activeSendToDockAlertType !== dismissedDockAlertType

    const allAlerts: AlertMap = { ...alerts }

    if (recentFailedMissions.length > 0) {
        allAlerts[AlertType.MissionFail] = {
            content: { kind: 'failedMissions', missions: recentFailedMissions },
            dismiss: () => clearAlert(AlertType.MissionFail),
        }
    }

    if (Object.keys(autoScheduleFailedMissionDict).length > 0) {
        allAlerts[AlertType.AutoScheduleFail] = {
            content: { kind: 'autoScheduleFail', failedMissions: autoScheduleFailedMissionDict },
            dismiss: () => clearAlert(AlertType.AutoScheduleFail),
        }
    }

    if (showDockAlert) {
        allAlerts[AlertType.RequestDock] = {
            content: { kind: 'dock', dockType: activeSendToDockAlertType! },
            dismiss: () => setDismissedDockAlertType(activeSendToDockAlertType),
        }
    }

    return (
        <AlertContext.Provider value={{ alerts: allAlerts, raiseAlert, clearAlert, clearAlerts }}>
            {children}
        </AlertContext.Provider>
    )
}

export const useAlertContext = () => useContext(AlertContext)
