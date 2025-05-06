import { createContext, FC, ReactNode, useContext, useEffect, useState } from 'react'
import { addMinutes, max } from 'date-fns'
import { Mission, MissionStatus } from 'models/Mission'
import { FailedMissionAlertContent, FailedMissionAlertListContent } from 'components/Alerts/FailedMissionAlert'
import { BackendAPICaller } from 'api/ApiCaller'
import { SignalREventLabels, useSignalRContext } from './SignalRContext'
import { useInstallationContext } from './InstallationContext'
import { Alert } from 'models/Alert'
import { useRobotContext } from './RobotContext'
import { BlockedRobotAlertContent, BlockedRobotAlertListContent } from 'components/Alerts/BlockedRobotAlert'
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
    DockSuccess: AlertType.DockSuccess,
    AutoScheduleFail: AlertType.AutoScheduleFail,
    skipAutoMission: AlertType.InfoAlert,
}

type AlertDictionaryType = {
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
    const [blockedRobotNames, setBlockedRobotNames] = useState<string[]>([])
    const { registerEvent, connectionReady } = useSignalRContext()
    const { installationCode } = useInstallationContext()
    const { TranslateText } = useLanguageContext()
    const { enabledRobots } = useRobotContext()
    const [autoScheduleFailedMissionDict, setAutoScheduleFailedMissionDict] = useState<AutoScheduleFailedMissionDict>(
        JSON.parse(window.localStorage.getItem('autoScheduleFailedMissionDict') || '{}')
    )

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
        if (source === AlertType.MissionFail)
            sessionStorage.setItem(dismissMissionFailTimeKey, JSON.stringify(Date.now()))

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

    // This variable is needed since the state in the useEffect below uses an outdated alert object
    const [newFailedMissions, setNewFailedMissions] = useState<Mission[]>([])

    // Set the initial failed missions when loading the page or changing installations
    useEffect(() => {
        const updateRecentFailedMissions = () => {
            const lastDismissTime: Date = getLastDismissalTime()
            BackendAPICaller.getMissionRuns({ statuses: [MissionStatus.Failed], pageSize: pageSize })
                .then((missions) => {
                    const newRecentFailedMissions = missions.content.filter(
                        (m) =>
                            convertUTCDateToLocalDate(new Date(m.endTime!)) > lastDismissTime &&
                            (!installationCode ||
                                m.installationCode!.toLocaleLowerCase() !== installationCode.toLocaleLowerCase())
                    )
                    if (newRecentFailedMissions.length > 0) setNewFailedMissions(newRecentFailedMissions)
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
    }, [installationCode])

    // Register a signalR event handler that listens for new failed missions
    useEffect(() => {
        if (connectionReady) {
            registerEvent(SignalREventLabels.missionRunFailed, (username: string, message: string) => {
                const newFailedMission: Mission = JSON.parse(message)
                const lastDismissTime: Date = getLastDismissalTime()

                setRecentFailedMissions((failedMissions) => {
                    if (
                        installationCode &&
                        (!newFailedMission.installationCode ||
                            newFailedMission.installationCode.toLocaleLowerCase() !==
                                installationCode.toLocaleLowerCase())
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
    }, [registerEvent, connectionReady, installationCode])

    useEffect(() => {
        if (connectionReady) {
            registerEvent(SignalREventLabels.alert, (username: string, message: string) => {
                const backendAlert: Alert = JSON.parse(message)
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

                if (alertType === AlertType.DockSuccess) {
                    setAlert(
                        alertType,
                        <DockAlertContent alertType={alertType} alertCategory={AlertCategory.INFO} />,
                        AlertCategory.INFO
                    )
                    clearAlert(AlertType.RequestDock)
                    setListAlert(
                        alertType,
                        <DockAlertListContent alertType={alertType} alertCategory={AlertCategory.INFO} />,
                        AlertCategory.INFO
                    )
                    clearListAlert(AlertType.RequestDock)
                } else if (alertType === AlertType.InfoAlert) {
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
    }, [registerEvent, connectionReady, installationCode, enabledRobots, autoScheduleFailedMissionDict])

    useEffect(() => {
        if (newFailedMissions.length > 0) {
            setAlert(
                AlertType.MissionFail,
                <FailedMissionAlertContent missions={newFailedMissions} />,
                AlertCategory.ERROR
            )
            setListAlert(
                AlertType.MissionFail,
                <FailedMissionAlertListContent missions={newFailedMissions} />,
                AlertCategory.ERROR
            )
            setNewFailedMissions([])
        }
    }, [newFailedMissions])

    useEffect(() => {
        const newBlockedRobotNames = enabledRobots
            .filter((robot) => robot.status === RobotStatus.Blocked)
            .map((robot) => robot.name!)

        const isBlockedRobotNamesModifyed =
            newBlockedRobotNames.some((name) => !blockedRobotNames.includes(name)) ||
            newBlockedRobotNames.length !== blockedRobotNames.length

        if (isBlockedRobotNamesModifyed) {
            if (newBlockedRobotNames.length > 0) {
                setAlert(
                    AlertType.BlockedRobot,
                    <BlockedRobotAlertContent robotNames={newBlockedRobotNames} />,
                    AlertCategory.WARNING
                )
                setListAlert(
                    AlertType.BlockedRobot,
                    <BlockedRobotAlertListContent robotNames={newBlockedRobotNames} />,
                    AlertCategory.WARNING
                )
            } else {
                clearAlert(AlertType.BlockedRobot)
                clearListAlert(AlertType.BlockedRobot)
            }
        }
        setBlockedRobotNames(newBlockedRobotNames)
    }, [enabledRobots, installationCode])

    useEffect(() => {
        if (Object.keys(autoScheduleFailedMissionDict).length > 0) {
            setListAlert(
                AlertType.AutoScheduleFail,
                <FailedAutoMissionAlertContent autoScheduleFailedMissionDict={autoScheduleFailedMissionDict} />,
                AlertCategory.ERROR
            )
            setAlert(
                AlertType.AutoScheduleFail,
                <FailedAutoMissionAlertContent autoScheduleFailedMissionDict={autoScheduleFailedMissionDict} />,
                AlertCategory.ERROR
            )
        }
    }, [connectionReady, autoScheduleFailedMissionDict])

    return (
        <AlertContext.Provider
            value={{
                alerts,
                setAlert,
                clearAlerts,
                clearAlert,
                listAlerts,
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
