import { createContext, FC, ReactNode, useContext, useEffect, useState } from 'react'
import { addMinutes, max } from 'date-fns'
import { Mission, MissionStatus } from 'models/Mission'
import { FailedMissionAlertContent, FailedMissionAlertListContent } from 'components/Alerts/FailedMissionAlert'
import { SignalREventLabels, useSignalRContext } from './SignalRContext'
import { Alert } from 'models/Alert'
import { useAssetContext } from './AssetContext'
import { RobotStatus } from 'models/Robot'
import {
    FailedAlertContent,
    FailedAlertListContent,
    FailedAutoMissionAlertContent,
} from 'components/Alerts/FailedAlertContent'
import { convertUTCDateToLocalDate } from 'utils/StringFormatting'
import { AlertCategory } from 'components/Alerts/AlertsBanner'
import { DockAlertContent, DockAlertListContent } from 'components/Alerts/DockAlert'
import { useLanguageContext } from './LanguageContext'
import { FailedRequestAlertContent, FailedRequestAlertListContent } from 'components/Alerts/FailedRequestAlert'
import { InfoAlertContent, InfoAlertListContent } from 'components/Alerts/InfoAlertContent'
import { useBackendApi } from 'api/UseBackendApi'
import { AuthContext } from './AuthContext'
import { InstallationContext } from './InstallationContext'

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

export type AlertDictionaryType = {
    [key in AlertType]?: { content: ReactNode | undefined; dismissFunction: () => void; alertCategory: AlertCategory }
}

interface IAlertContext {
    alerts: AlertDictionaryType
    setAlert: (source: AlertType, alert: ReactNode, category: AlertCategory) => void
    clearAlerts: () => void
    clearAlert: (source: AlertType) => void
    listAlerts: AlertDictionaryType
    setListAlert: (source: AlertType, listAlert: ReactNode, category: AlertCategory) => void
    clearListAlerts: () => void
    clearListAlert: (source: AlertType) => void
}

interface Props {
    children: React.ReactNode
}

const defaultAlertInterface = {
    alerts: {},
    setAlert: () => {},
    clearAlerts: () => {},
    clearAlert: () => {},
    listAlerts: {},
    setListAlert: () => {},
    clearListAlerts: () => {},
    clearListAlert: () => {},
}

export interface AutoScheduleFailedMissionDict {
    [key: string]: string
}

const AlertContext = createContext<IAlertContext>(defaultAlertInterface)

export const AlertProvider: FC<Props> = ({ children }) => {
    const [alerts, setAlerts] = useState<AlertDictionaryType>(defaultAlertInterface.alerts)
    const [listAlerts, setListAlerts] = useState<AlertDictionaryType>(defaultAlertInterface.listAlerts)
    const [recentFailedMissions, setRecentFailedMissions] = useState<Mission[]>([])
    const { registerEvent, connectionReady } = useSignalRContext()
    const { TranslateText } = useLanguageContext()
    const { enabledRobots } = useAssetContext()
    const { installation } = useContext(InstallationContext)
    const [autoScheduleFailedMissionDict, setAutoScheduleFailedMissionDict] = useState<AutoScheduleFailedMissionDict>(
        JSON.parse(window.localStorage.getItem('autoScheduleFailedMissionDict') || '{}')
    )
    const backendApi = useBackendApi()
    const { isAuthenticated } = useContext(AuthContext)

    const pageSize: number = 100
    // The default amount of minutes in the past for failed missions to generate an alert
    const defaultTimeInterval: number = 10
    // The maximum amount of minutes in the past for failed missions to generate an alert
    const maxTimeInterval: number = 60
    const dismissMissionFailTimeKey: string = 'lastMissionFailDismissalTime'

    const setAlert = (source: AlertType, alert: ReactNode, category: AlertCategory) => {
        setAlerts((oldAlerts) => {
            return {
                ...oldAlerts,
                [source]: { content: alert, dismissFunction: () => clearAlert(source), alertCategory: category },
            }
        })
    }

    const clearAlerts = () => setAlerts({})

    const clearAlert = (source: AlertType) => {
        if (source === AlertType.MissionFail) {
            sessionStorage.setItem(dismissMissionFailTimeKey, JSON.stringify(Date.now()))
            setRecentFailedMissions([])
        }

        if (source === AlertType.AutoScheduleFail) {
            setAutoScheduleFailedMissionDict({})
            window.localStorage.setItem('autoScheduleFailedMissionDict', JSON.stringify({}))
        }

        setAlerts((oldAlerts) => {
            const newAlerts = { ...oldAlerts }
            delete newAlerts[source]
            return newAlerts
        })
    }

    const setListAlert = (source: AlertType, listAlert: ReactNode, category: AlertCategory) => {
        setListAlerts((oldListAlerts) => {
            return {
                ...oldListAlerts,
                [source]: {
                    content: listAlert,
                    dismissFunction: () => clearListAlert(source),
                    alertCategory: category,
                },
            }
        })
    }

    const clearListAlerts = () => setListAlerts({})

    const clearListAlert = (source: AlertType) => {
        if (source === AlertType.MissionFail)
            sessionStorage.setItem(dismissMissionFailTimeKey, JSON.stringify(Date.now()))

        if (source === AlertType.AutoScheduleFail) {
            setAutoScheduleFailedMissionDict({})
            window.localStorage.setItem('autoScheduleFailedMissionDict', JSON.stringify({}))
        }

        setListAlerts((oldListAlerts) => {
            const newListAlerts = { ...oldListAlerts }
            delete newListAlerts[source]
            return newListAlerts
        })
    }

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
                    setAlert(
                        AlertType.RequestFail,
                        <FailedRequestAlertContent
                            translatedMessage={TranslateText('Failed to retrieve failed missions')}
                        />,
                        AlertCategory.ERROR
                    )
                    setListAlert(
                        AlertType.RequestFail,
                        <FailedRequestAlertListContent
                            translatedMessage={TranslateText('Failed to retrieve failed missions')}
                        />,
                        AlertCategory.ERROR
                    )
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
                    const newAutoScheduleFailedMissionDict: AutoScheduleFailedMissionDict = {
                        ...autoScheduleFailedMissionDict,
                    }
                    newAutoScheduleFailedMissionDict[backendAlert.alertTitle] = backendAlert.alertMessage
                    setAutoScheduleFailedMissionDict(newAutoScheduleFailedMissionDict)
                    window.localStorage.setItem(
                        'autoScheduleFailedMissionDict',
                        JSON.stringify(newAutoScheduleFailedMissionDict)
                    )
                    return
                }

                if (alertType === AlertType.InfoAlert) {
                    setAlert(
                        alertType,
                        <InfoAlertContent title={backendAlert.alertTitle} message={backendAlert.alertMessage} />,
                        AlertCategory.INFO
                    )
                    setListAlert(
                        alertType,
                        <InfoAlertListContent title={backendAlert.alertTitle} message={backendAlert.alertMessage} />,
                        AlertCategory.INFO
                    )
                } else {
                    setAlert(
                        alertType,
                        <FailedAlertContent title={backendAlert.alertTitle} message={backendAlert.alertMessage} />,
                        AlertCategory.ERROR
                    )
                    setListAlert(
                        alertType,
                        <FailedAlertListContent title={backendAlert.alertTitle} message={backendAlert.alertMessage} />,
                        AlertCategory.ERROR
                    )
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

    const combinedAlerts: AlertDictionaryType = { ...alerts }
    const combinedListAlerts: AlertDictionaryType = { ...listAlerts }

    if (recentFailedMissions.length > 0) {
        combinedAlerts[AlertType.MissionFail] = {
            content: <FailedMissionAlertContent missions={recentFailedMissions} />,
            dismissFunction: () => clearAlert(AlertType.MissionFail),
            alertCategory: AlertCategory.ERROR,
        }
        combinedListAlerts[AlertType.MissionFail] = {
            content: <FailedMissionAlertListContent missions={recentFailedMissions} />,
            dismissFunction: () => clearListAlert(AlertType.MissionFail),
            alertCategory: AlertCategory.ERROR,
        }
    }

    if (Object.keys(autoScheduleFailedMissionDict).length > 0) {
        combinedAlerts[AlertType.AutoScheduleFail] = {
            content: <FailedAutoMissionAlertContent autoScheduleFailedMissionDict={autoScheduleFailedMissionDict} />,
            dismissFunction: () => clearAlert(AlertType.AutoScheduleFail),
            alertCategory: AlertCategory.ERROR,
        }
        combinedListAlerts[AlertType.AutoScheduleFail] = {
            content: <FailedAutoMissionAlertContent autoScheduleFailedMissionDict={autoScheduleFailedMissionDict} />,
            dismissFunction: () => clearListAlert(AlertType.AutoScheduleFail),
            alertCategory: AlertCategory.ERROR,
        }
    }

    if (showDockAlert) {
        const dockAlertCategory =
            activeSendToDockAlertType === AlertType.RequestDock ? AlertCategory.WARNING : AlertCategory.INFO
        combinedAlerts[AlertType.RequestDock] = {
            content: <DockAlertContent alertType={activeSendToDockAlertType!} />,
            dismissFunction: () => setDismissedDockAlertType(activeSendToDockAlertType),
            alertCategory: dockAlertCategory,
        }
        combinedListAlerts[AlertType.RequestDock] = {
            content: <DockAlertListContent alertType={activeSendToDockAlertType!} />,
            dismissFunction: () => setDismissedDockAlertType(activeSendToDockAlertType),
            alertCategory: dockAlertCategory,
        }
    }

    return (
        <AlertContext.Provider
            value={{
                alerts: combinedAlerts,
                setAlert,
                clearAlerts,
                clearAlert,
                listAlerts: combinedListAlerts,
                setListAlert,
                clearListAlerts,
                clearListAlert,
            }}
        >
            {children}
        </AlertContext.Provider>
    )
}

export const useAlertContext = () => useContext(AlertContext)
